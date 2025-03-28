﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Motio.NodeCommon.Utils
{
    /// <summary>
    /// Represents an ordered set of values.
    /// </summary> 
    /// <remarks>
    /// Values are kept in order in which they are added.
    /// Order can be modified by <see cref="MoveFirst(T)"/>, <see cref="MoveLast(T)"/>, <see cref="MoveBefore(T, T)"/>, <see cref="MoveAfter(T, T)"/>.
    /// </remarks>
    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "By design")]
    public class OrderedHashSet<T> : ICollection<T>, ISerializable, IDeserializationCallback
    {
        private static bool IsValueType = typeof(T).IsValueType;
        private static bool IsNullable = typeof(T).IsValueType && typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>);

        // store lower 31 bits of hash code 
        private const int Lower31BitMask = 0x7FFFFFFF;

        // factor used to increase hashset capacity
        private const int GrowthFactor = 2;

        // when constructing a hashset from an existing collection, it may contain duplicates, 
        // so this is used as the max acceptable excess ratio of capacity to count. Note that 
        // this is only used on the ctor and not to automatically shrink if the hashset has, e.g,
        // a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess. 
        // This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
        private const int ShrinkThreshold = 3;

        // constants for serialization 
        private const String CapacityName = "Capacity";
        private const String ElementsName = "Elements";
        private const String ComparerName = "Comparer";
        private const String VersionName = "Version";

        private int[] m_buckets;
        private Slot[] m_slots;
        private int m_count;
        private int m_lastIndex;
        private int m_freeList;
        private IEqualityComparer<T> m_comparer;
        private int m_version;
        private int m_firstOrderIndex;  // Index of first entry by order
        private int m_lastOrderIndex;   // Index of last entry by order

        // temporary variable needed during deserialization 
        private SerializationInfo m_siInfo;

        /// <summary> 
        /// Number of elements in this hashset
        /// </summary>
        public int Count
        {
            get { return m_count; }
        }

        /// <summary>
        /// Gets collection reader.
        /// </summary>
        public Reader Items
        {
            get { return new Reader(this); }
        }

        /// <summary>
        /// Gets reversed collection reader.
        /// </summary>
        public ReverseReader Reversed
        {
            get { return new ReverseReader(this); }
        }

        #region Constructors

        public OrderedHashSet()
                : this(EqualityComparer<T>.Default)
        {
        }

        public OrderedHashSet(int capacity)
                : this(capacity, EqualityComparer<T>.Default)
        {
        }

        public OrderedHashSet(IEqualityComparer<T> comparer)
            : this(0, comparer)
        {
        }

        public OrderedHashSet(int capacity, IEqualityComparer<T> comparer)
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<T>.Default;
            }

            m_comparer = comparer;
            m_lastIndex = 0;
            m_count = 0;
            m_freeList = -1;
            m_version = 0;
            m_firstOrderIndex = -1;
            m_lastOrderIndex = -1;

            if (capacity > 0)
            {
                Initialize(capacity);
            }
        }

        public OrderedHashSet(IEnumerable<T> collection)
                : this(collection, EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Implementation Notes: 
        /// Since resizes are relatively expensive (require rehashing), this attempts to minimize 
        /// the need to resize by setting the initial capacity based on size of collection.
        /// </summary> 
        /// <param name="collection"></param>
        /// <param name="comparer"></param>
        public OrderedHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
                : this(comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            // to avoid excess resizes, first set size based on collection's count. Collection 
            // may contain duplicates, so call TrimExcess if resulting hashset is larger than
            // threshold
            int suggestedCapacity = 0;
            ICollection<T> coll = collection as ICollection<T>;
            if (coll != null)
            {
                suggestedCapacity = coll.Count;
            }
            Initialize(suggestedCapacity);

            UnionWith(collection);
            if ((m_count == 0 && m_slots.Length > HashHelpers.GetPrime(0)) ||
                (m_count > 0 && m_slots.Length / m_count > ShrinkThreshold))
            {
                TrimExcess();
            }
        }

        protected OrderedHashSet(SerializationInfo info, StreamingContext context)
        {
            // We can't do anything with the keys and values until the entire graph has been 
            // deserialized and we have a reasonable estimate that GetHashCode is not going to
            // fail.  For the time being, we'll just cache this.  The graph is not valid until
            // OnDeserialization has been called.
            m_siInfo = info;
        }

        #endregion

        #region ISerializable methods

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            // need to serialize version to avoid problems with serializing while enumerating 
            info.AddValue(VersionName, m_version);
            info.AddValue(ComparerName, m_comparer, typeof(IEqualityComparer<T>));
            info.AddValue(CapacityName, m_buckets == null ? 0 : m_buckets.Length);
            if (m_buckets != null)
            {
                T[] array = new T[m_count];
                CopyTo(array); // Copies ordered data
                info.AddValue(ElementsName, array, typeof(T[]));
            }
        }

        #endregion

        #region IDeserializationCallback methods

        public virtual void OnDeserialization(Object sender)
        {

            if (m_siInfo == null)
            {
                // It might be necessary to call OnDeserialization from a container if the 
                // container object also implements OnDeserialization. However, remoting will
                // call OnDeserialization again. We can return immediately if this function is 
                // called twice. Note we set m_siInfo to null at the end of this method. 
                return;
            }

            int capacity = m_siInfo.GetInt32(CapacityName);
            m_comparer = (IEqualityComparer<T>)m_siInfo.GetValue(ComparerName, typeof(IEqualityComparer<T>));
            m_freeList = -1;
            m_firstOrderIndex = -1;
            m_lastOrderIndex = -1;

            if (capacity != 0)
            {
                m_buckets = new int[capacity];
                m_slots = new Slot[capacity];

                T[] array = (T[])m_siInfo.GetValue(ElementsName, typeof(T[]));

                if (array == null)
                {
                    throw new SerializationException("Serialization_MissingKeys");
                }

                // there are no resizes here because we already set capacity above 
                for (int i = 0; i < array.Length; i++)
                {
                    Add(array[i]);
                }
            }
            else
            {
                m_buckets = null;
            }

            m_version = m_siInfo.GetInt32(VersionName);
            m_siInfo = null;
        }

        #endregion

        #region ICollection<T> methods 

        /// <summary> 
        /// Whether this is readonly
        /// </summary> 
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Add item to this hashset. This is the explicit implementation of the ICollection<T>
        /// interface. The other Add method returns bool indicating whether item was added. 
        /// </summary>
        /// <param name="item">item to add</param> 
        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        /// <summary>
        /// Remove all items from this set. This clears the elements but not the underlying
        /// buckets and slots array. Follow this call by TrimExcess to release these. 
        /// </summary>
        public void Clear()
        {
            if (m_lastIndex > 0)
            {
                Debug.Assert(m_buckets != null, "m_buckets was null but m_lastIndex > 0");

                // clear the elements so that the gc can reclaim the references.
                // clear only up to m_lastIndex for m_slots
                Array.Clear(m_slots, 0, m_lastIndex);
                Array.Clear(m_buckets, 0, m_buckets.Length);
                m_lastIndex = 0;
                m_count = 0;
                m_freeList = -1;
                m_firstOrderIndex = -1;
                m_lastOrderIndex = -1;
            }
            m_version++;
        }

        /// <summary>
        /// Checks if this hashset contains the item 
        /// </summary>
        /// <param name="item">item to check for containment</param> 
        /// <returns>true if item contained; false if not</returns> 
        public bool Contains(T item)
        {
            if (m_buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                // see note at "HashSet" level describing why "- 1" appears in for loop
                for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next)
                {
                    if (m_slots[i].hashCode == hashCode && m_comparer.Equals(m_slots[i].value, item))
                    {
                        return true;
                    }
                }
            }
            // either m_buckets is null or wasn't found 
            return false;
        }

        /// <summary> 
        /// Copy items in this hashset to array, starting at arrayIndex
        /// </summary> 
        /// <param name="array">array to add items to</param> 
        /// <param name="arrayIndex">index to start at</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, m_count);
        }

        /// <summary> 
        /// Take the union of this HashSet with other. Modifies this set.
        /// 
        /// Implementation note: GetSuggestedCapacity (to increase capacity in advance avoiding
        /// multiple resizes ended up not being useful in practice; quickly gets to the
        /// point where it's a wasteful check.
        /// </summary> 
        /// <param name="other">enumerable with items to add</param>
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            foreach (T item in other)
            {
                Add(item);
            }
        }

        private int InternalIndexOf(T item)
        {
            int num = this.InternalGetHashCode(item);
            for (int i = this.m_buckets[num % this.m_buckets.Length] - 1; i >= 0; i = this.m_slots[i].next)
            {
                if (this.m_slots[i].hashCode == num && this.m_comparer.Equals(this.m_slots[i].value, item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary> 
        /// Remove item from this hashset
        /// </summary> 
        /// <param name="item">item to remove</param> 
        /// <returns>true if removed; false if not (i.e. if the item wasn't in the HashSet)</returns>
        public bool Remove(T item)
        {
            if (m_buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                int bucket = hashCode % m_buckets.Length;
                int last = -1;
                for (int i = m_buckets[bucket] - 1; i >= 0; last = i, i = m_slots[i].next)
                {
                    if (m_slots[i].hashCode == hashCode && m_comparer.Equals(m_slots[i].value, item))
                    {
                        if (last < 0)
                        {
                            // first iteration; update buckets
                            m_buckets[bucket] = m_slots[i].next + 1;
                        }
                        else
                        {
                            // subsequent iterations; update 'next' pointers
                            m_slots[last].next = m_slots[i].next;
                        }

                        // Connect linked list
                        if (m_firstOrderIndex == i) // Is first
                        {
                            m_firstOrderIndex = m_slots[i].nextOrder;
                        }
                        if (m_lastOrderIndex == i) // Is last
                        {
                            m_lastOrderIndex = m_slots[i].previousOrder;
                        }

                        var next = m_slots[i].nextOrder;
                        var prev = m_slots[i].previousOrder;
                        if (next != -1)
                        {
                            m_slots[next].previousOrder = prev;
                        }
                        if (prev != -1)
                        {
                            m_slots[prev].nextOrder = next;
                        }

                        m_slots[i].hashCode = -1;
                        m_slots[i].value = default(T);
                        m_slots[i].next = m_freeList;
                        m_slots[i].previousOrder = -1;
                        m_slots[i].nextOrder = -1;

                        m_count--;
                        m_version++;
                        if (m_count == 0)
                        {
                            m_lastIndex = 0;
                            m_freeList = -1;
                        }
                        else
                        {
                            m_freeList = i;
                        }
                        return true;
                    }
                }
            }
            // either m_buckets is null or wasn't found
            return false;
        }

        #endregion

        #region IEnumerable methods 

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region HashSet methods 

        /// <summary>
        /// Copies the elements to an array.
        /// </summary>
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0, m_count);
        }

        /// <summary>
        /// Copies the specified number of elements to an array, starting at the specified array index.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            // check array index valid index into array 
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "ArgumentOutOfRange_NeedNonNegNum");
            }

            // also throw if count less than 0
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "ArgumentOutOfRange_NeedNonNegNum");
            }

            // will array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
            {
                throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
            }

            int numCopied = 0;
            for (int i = m_firstOrderIndex; i != -1 && numCopied < count; i = m_slots[i].nextOrder)
            {
                array[arrayIndex + numCopied] = m_slots[i].value;
                numCopied++;
            }
        }

        public bool MoveFirst(T item)
        {
            int index = InternalIndexOf(item);
            if (index != -1)
            {
                var prev = m_slots[index].previousOrder;
                if (prev != -1) // Not first
                {
                    // Disconnect
                    var next = m_slots[index].nextOrder;
                    if (next == -1) // Last
                    {
                        m_lastOrderIndex = prev;
                    }
                    else
                    {
                        m_slots[next].previousOrder = prev;
                    }
                    m_slots[prev].nextOrder = next;

                    // Reconnect
                    m_slots[index].previousOrder = -1;
                    m_slots[index].nextOrder = m_firstOrderIndex;
                    m_slots[m_firstOrderIndex].previousOrder = index;
                    m_firstOrderIndex = index;
                }
                return true;
            }
            return false;
        }

        public bool MoveLast(T item)
        {
            int index = InternalIndexOf(item);
            if (index != -1)
            {
                var next = m_slots[index].nextOrder;
                if (next != -1) // Not last
                {
                    // Disconnect
                    var prev = m_slots[index].previousOrder;
                    if (prev == -1) // First
                    {
                        m_firstOrderIndex = next;
                    }
                    else
                    {
                        m_slots[prev].nextOrder = next;
                    }
                    m_slots[next].previousOrder = prev;

                    // Reconnect
                    m_slots[index].nextOrder = -1;
                    m_slots[index].previousOrder = m_lastOrderIndex;
                    m_slots[m_lastOrderIndex].nextOrder = index;
                    m_lastOrderIndex = index;
                }
                return true;
            }
            return false;
        }

        public bool MoveBefore(T itemToMove, T mark)
        {
            int index = InternalIndexOf(itemToMove);
            int markIndex = InternalIndexOf(mark);
            if (index != -1 && markIndex != -1 && index != markIndex)
            {
                // Disconnect
                var next = m_slots[index].nextOrder;
                var prev = m_slots[index].previousOrder;
                if (prev == -1) // First
                {
                    m_firstOrderIndex = next;
                }
                else
                {
                    m_slots[prev].nextOrder = next;
                }
                if (next == -1) // Last
                {
                    m_lastOrderIndex = prev;
                }
                else
                {
                    m_slots[next].previousOrder = prev;
                }

                // Reconnect
                var preMark = m_slots[markIndex].previousOrder;
                m_slots[index].nextOrder = markIndex;
                m_slots[index].previousOrder = preMark;
                m_slots[markIndex].previousOrder = index;
                if (preMark == -1)
                {
                    m_firstOrderIndex = index;
                }
                else
                {
                    m_slots[preMark].nextOrder = index;
                }
                return true;
            }
            return false;
        }

        public bool MoveAfter(T itemToMove, T mark)
        {
            int index = InternalIndexOf(itemToMove);
            int markIndex = InternalIndexOf(mark);
            if (index != -1 && markIndex != -1 && index != markIndex)
            {
                // Disconnect
                var next = m_slots[index].nextOrder;
                var prev = m_slots[index].previousOrder;
                if (prev == -1) // First
                {
                    m_firstOrderIndex = next;
                }
                else
                {
                    m_slots[prev].nextOrder = next;
                }
                if (next == -1) // Last
                {
                    m_lastOrderIndex = prev;
                }
                else
                {
                    m_slots[next].previousOrder = prev;
                }

                // Reconnect
                var postMark = m_slots[markIndex].nextOrder;
                m_slots[index].previousOrder = markIndex;
                m_slots[index].nextOrder = postMark;
                m_slots[markIndex].nextOrder = index;
                if (postMark == -1)
                {
                    m_lastOrderIndex = index;
                }
                else
                {
                    m_slots[postMark].previousOrder = index;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns enumeration which goes from <paramref name="item"/> to last element in the set (including both).
        /// When <paramref name="item"/> is not found, returns empty enumeration.
        /// </summary>
        public Range StartWith(T item)
        {
            return new Range(this, item);
        }

        /// <summary>
        /// Returns enumeration which goes from <paramref name="item"/> to first element in the set (including both).
        /// When <paramref name="item"/> is not found, returns empty enumeration.
        /// </summary>
        public ReverseRange StartWithReversed(T item)
        {
            return new ReverseRange(this, item);
        }

        /// <summary>
        /// Remove elements that match specified predicate. Returns the number of elements removed 
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            int numRemoved = 0;
            for (int i = 0; i < m_lastIndex; i++)
            {
                if (m_slots[i].hashCode >= 0)
                {
                    // cache value in case delegate removes it
                    T value = m_slots[i].value;
                    if (match(value))
                    {
                        // check again that remove actually removed it 
                        if (Remove(value))
                        {
                            numRemoved++;
                        }
                    }
                }
            }
            return numRemoved;
        }

        /// <summary> 
        /// Gets the IEqualityComparer that is used to determine equality of keys for
        /// the HashSet. 
        /// </summary>
        public IEqualityComparer<T> Comparer
        {
            get
            {
                return m_comparer;
            }
        }

        /// <summary>
        /// Sets the capacity of this list to the size of the list (rounded up to nearest prime), 
        /// unless count is 0, in which case we release references.
        ///
        /// This method can be used to minimize a list's memory overhead once it is known that no
        /// new elements will be added to the list. To completely clear a list and release all 
        /// memory referenced by the list, execute the following statements:
        /// 
        /// Clear(); 
        /// TrimExcess();
        /// </summary> 
        public void TrimExcess()
        {
            Debug.Assert(m_count >= 0, "m_count is negative");

            if (m_count == 0)
            {
                // if count is zero, clear references
                m_buckets = null;
                m_slots = null;
                m_version++;
            }
            else
            {
                Debug.Assert(m_buckets != null, "m_buckets was null but m_count > 0");

                // similar to IncreaseCapacity but moves down elements in case add/remove/etc 
                // caused fragmentation
                int newSize = HashHelpers.GetPrime(m_count);
                Slot[] newSlots = new Slot[newSize];
                int[] newBuckets = new int[newSize];

                // move down slots and rehash at the same time. newIndex keeps track of current
                // position in newSlots array
                int newIndex = 0;
                for (int i = 0; i < m_lastIndex; i++)
                {
                    if (m_slots[i].hashCode >= 0)
                    {
                        newSlots[newIndex] = m_slots[i];

                        // rehash
                        int bucket = newSlots[newIndex].hashCode % newSize;
                        newSlots[newIndex].next = newBuckets[bucket] - 1;
                        newBuckets[bucket] = newIndex + 1;

                        // temporarily store new index in m_slots[i].next
                        m_slots[i].next = newIndex;

                        newIndex++;
                    }
                }

                newIndex = 0;
                for (int i = 0; i < m_lastIndex; i++)
                {
                    if (m_slots[i].hashCode >= 0)
                    {
                        var next = m_slots[i].nextOrder;
                        var prev = m_slots[i].previousOrder;

                        // Use temporarily stored index
                        if (next != -1)
                        {
                            newSlots[newIndex].nextOrder = m_slots[next].next;
                        }
                        else
                        {
                            m_lastOrderIndex = newIndex;
                        }
                        if (prev != -1)
                        {
                            newSlots[newIndex].previousOrder = m_slots[prev].next;
                        }
                        else
                        {
                            m_firstOrderIndex = newIndex;
                        }
                        newIndex++;
                    }
                }

                Debug.Assert(newSlots.Length <= m_slots.Length, "capacity increased after TrimExcess");

                m_lastIndex = newIndex;
                m_slots = newSlots;
                m_buckets = newBuckets;
                m_freeList = -1;
            }
        }

        #endregion

        #region Helper methods 

        /// <summary>
        /// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime
        /// greater than or equal to capacity. 
        /// </summary>
        /// <param name="capacity"></param> 
        private void Initialize(int capacity)
        {
            Debug.Assert(m_buckets == null, "Initialize was called but m_buckets was non-null");

            int size = HashHelpers.GetPrime(capacity);

            m_buckets = new int[size];
            m_slots = new Slot[size];
        }

        /// <summary> 
        /// Expand to new capacity. New capacity is next prime greater than or equal to suggested
        /// size. This is called when the underlying array is filled. This performs no 
        /// defragmentation, allowing faster execution; note that this is reasonable since
        /// AddIfNotPresent attempts to insert new elements in re-opened spots.
        /// </summary>
        private void IncreaseCapacity()
        {
            Debug.Assert(m_buckets != null, "IncreaseCapacity called on a set with no elements");

            // Handle overflow conditions. Try to expand capacity by GrowthFactor. If that causes
            // overflow, use size suggestion of m_count and see if HashHelpers returns a value 
            // greater than that. If not, capacity can't be increased so throw capacity overflow
            // exception.
            int sizeSuggestion = unchecked(m_count * GrowthFactor);
            if (sizeSuggestion < 0)
            {
                sizeSuggestion = m_count;
            }
            int newSize = HashHelpers.GetPrime(sizeSuggestion);
            if (newSize <= m_count)
            {
                throw new ArgumentException("Arg_HSCapacityOverflow");
            }

            // Able to increase capacity; copy elements to larger array and rehash
            Slot[] newSlots = new Slot[newSize];
            if (m_slots != null)
            {
                Array.Copy(m_slots, 0, newSlots, 0, m_lastIndex);
            }

            int[] newBuckets = new int[newSize];
            for (int i = 0; i < m_lastIndex; i++)
            {
                int bucket = newSlots[i].hashCode % newSize;
                newSlots[i].next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }
            m_slots = newSlots;
            m_buckets = newBuckets;

        }

        /// <summary> 
        /// Add item to this HashSet. Returns bool indicating whether item was added (won't be 
        /// added if already present)
        /// </summary> 
        /// <param name="item"></param>
        /// <returns>true if added, false if already present</returns>
        public bool Add(T value)
        {
            if (m_buckets == null)
            {
                Initialize(0);
            }

            int hashCode = InternalGetHashCode(value);
            int bucket = hashCode % m_buckets.Length;
            for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next)
            {
                if (m_slots[i].hashCode == hashCode && m_comparer.Equals(m_slots[i].value, value))
                {
                    return false;
                }
            }
            int index;
            if (m_freeList >= 0)
            {
                index = m_freeList;
                m_freeList = m_slots[index].next;
            }
            else
            {
                if (m_lastIndex == m_slots.Length)
                {
                    IncreaseCapacity();
                    // this will change during resize
                    bucket = hashCode % m_buckets.Length;
                }
                index = m_lastIndex;
                m_lastIndex++;
            }
            m_slots[index].hashCode = hashCode;
            m_slots[index].value = value;
            m_slots[index].next = m_buckets[bucket] - 1;

            // Append to linked list
            if (m_lastOrderIndex != -1)
            {
                m_slots[m_lastOrderIndex].nextOrder = index;
            }
            if (m_firstOrderIndex == -1)
            {
                m_firstOrderIndex = index;
            }
            m_slots[index].nextOrder = -1;
            m_slots[index].previousOrder = m_lastOrderIndex;
            m_lastOrderIndex = index;

            m_buckets[bucket] = index + 1;
            m_count++;
            m_version++;
            return true;
        }

        /// <summary> 
        /// Workaround Comparers that throw ArgumentNullException for GetHashCode(null).
        /// </summary> 
        /// <param name="item"></param>
        /// <returns>hash code</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int InternalGetHashCode(T item)
        {
            // This check prevents boxing of value types to compare to null
            if (IsValueType ? IsNullable && item.Equals(null) : item == null)
            {
                return 0;
            }
            return m_comparer.GetHashCode(item) & Lower31BitMask;
        }

        #endregion

        internal struct Slot
        {
            internal int hashCode;      // Lower 31 bits of hash code, -1 if unused
            internal T value;
            internal int next;          // Index of next entry, -1 if last
            internal int nextOrder;     // Index of next entry by order, -1 if last
            internal int previousOrder; // Index of previous entry by order, -1 if first
        }

        public struct Reader : IReadOnlyCollection<T>
        {
            private OrderedHashSet<T> m_set;

            public int Count { get { return m_set.Count; } }

            public Reader(OrderedHashSet<T> set)
            {
                this.m_set = set;
            }

            public bool Contains(T item)
            {
                return m_set.Contains(item);
            }

            public Range StartWith(T item)
            {
                return new Range(m_set, item);
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_set);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct ReverseReader : IReadOnlyCollection<T>
        {
            private OrderedHashSet<T> m_set;

            public int Count { get { return m_set.Count; } }

            public ReverseReader(OrderedHashSet<T> set)
            {
                this.m_set = set;
            }

            public bool Contains(T item)
            {
                return m_set.Contains(item);
            }

            public ReverseRange StartWith(T item)
            {
                return new ReverseRange(m_set, item);
            }

            public ReverseEnumerator GetEnumerator()
            {
                return new ReverseEnumerator(m_set);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Part of <see cref="OrderedHashSet{T}"/> starting with specified element.
        /// Enumeration goes from specified element to last element in collection.
        /// Returns empty enumeration when specified item is not in collection.
        /// </summary>
        public struct Range : IEnumerable<T>
        {
            OrderedHashSet<T> m_set;
            T m_startingItem;

            public Range(OrderedHashSet<T> set, T startingItem)
            {
                m_set = set;
                m_startingItem = startingItem;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_set, m_set.InternalIndexOf(m_startingItem));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Part of <see cref="OrderedHashSet{T}"/> in reversed order starting with specified element.
        /// Enumeration goes from specified element to first element in collection.
        /// Returns empty enumeration when specified item is not in collection.
        /// </summary>
        public struct ReverseRange : IEnumerable<T>
        {
            OrderedHashSet<T> m_set;
            T m_startingItem;

            public ReverseRange(OrderedHashSet<T> set, T startingItem)
            {
                m_set = set;
                m_startingItem = startingItem;
            }

            public ReverseEnumerator GetEnumerator()
            {
                return new ReverseEnumerator(m_set, m_set.InternalIndexOf(m_startingItem));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Serializable]
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private OrderedHashSet<T> m_set;
            private int m_index;
            private int m_version;
            private T m_current;

            public T Current
            {
                get { return m_current; }
            }

            internal Enumerator(OrderedHashSet<T> set)
                : this(set, set.m_firstOrderIndex)
            {
            }

            internal Enumerator(OrderedHashSet<T> set, int startIndex)
            {
                m_set = set;
                m_index = startIndex;
                m_version = set.m_version;
                m_current = default(T);
            }

            public bool MoveNext()
            {
                if (m_version != m_set.m_version)
                {
                    throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
                }

                while (m_index != -1)
                {
                    m_current = m_set.m_slots[m_index].value;
                    m_index = m_set.m_slots[m_index].nextOrder;
                    return true;
                }
                m_current = default(T);
                return false;
            }

            void IDisposable.Dispose()
            {
            }

            Object IEnumerator.Current
            {
                get
                {
                    if (m_index == m_set.m_firstOrderIndex || m_index == -1)
                    {
                        throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }
        }

        [Serializable]
        public struct ReverseEnumerator : IEnumerator<T>, IEnumerator
        {
            private OrderedHashSet<T> m_set;
            private int m_index;
            private int m_version;
            private T m_current;

            public T Current
            {
                get { return m_current; }
            }

            internal ReverseEnumerator(OrderedHashSet<T> set)
                : this(set, set.m_lastOrderIndex)
            {
            }

            internal ReverseEnumerator(OrderedHashSet<T> set, int startIndex)
            {
                m_set = set;
                m_index = startIndex;
                m_version = set.m_version;
                m_current = default(T);
            }

            public bool MoveNext()
            {
                if (m_version != m_set.m_version)
                {
                    throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
                }

                while (m_index != -1)
                {
                    m_current = m_set.m_slots[m_index].value;
                    m_index = m_set.m_slots[m_index].previousOrder;
                    return true;
                }
                m_current = default(T);
                return false;
            }

            Object IEnumerator.Current
            {
                get
                {
                    if (m_index == m_set.m_lastOrderIndex || m_index == -1)
                    {
                        throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
                    }
                    return Current;
                }
            }

            void IDisposable.Dispose()
            {
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
