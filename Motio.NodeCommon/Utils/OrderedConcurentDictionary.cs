using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Motio.NodeCommon.Utils
{
    public class OrderedConcurentDictionary<TKey, TValue> : IDictionary, IDictionary<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
        ConcurrentDictionary<TKey, TValue> dict = new ConcurrentDictionary<TKey, TValue>();
        ConcurrentDictionary<int, TKey> keys = new ConcurrentDictionary<int, TKey>();

        public TValue this[TKey key]
        {
            get { TryGetValue(key, out TValue v); return v; }
            set { SetValue(key, value); }
        }
        
        public TValue this[int index]
        {
            get { TryGetValue(index, out TValue v); return v; }
            set { SetValue(index, value); }
        }

        public int Count => keys.Count;

        object IDictionary.this[object key]
        {
            get { TryGetValue((TKey)key, out TValue v); return v; }
            set { SetValue((TKey)key, (TValue)value); }
        }
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => this[key];
            set => this[key] = value;
        }

        ICollection IDictionary.Keys { get { List<TKey> keys = dict.Keys.ToList(); return keys; } }
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => dict.Keys;
        ICollection IDictionary.Values { get { List<TValue> keys = dict.Values.ToList(); return keys; } }
        ICollection<TValue> IDictionary<TKey, TValue>.Values => dict.Values;
        bool IDictionary.IsReadOnly => false;
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
        bool IDictionary.IsFixedSize => false;
        int ICollection.Count => Count;
        int ICollection<KeyValuePair<TKey, TValue>>.Count => Count;
        object _syncRoot;
        object ICollection.SyncRoot => _syncRoot ?? (_syncRoot = new object());
        bool ICollection.IsSynchronized => true;

        /// <summary>
        /// most efficient way of adding to this dictionnary O(1)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAdd(TKey key, TValue value)
        {
            bool dictAdd = dict.TryAdd(key, value);
            if (!dictAdd)
                return dictAdd;
            bool keyAdd = keys.TryAdd(keys.Count, key);
            return keyAdd;
        }

        /// <summary>
        /// add at index, O(Count - index) roughly O(N/2)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAddAt(int index, TKey key, TValue value)
        {
            if (!dict.TryAdd(key, value))
                return false;

            for (int i = keys.Count; i >= index; i--)
            {
                keys[i] = keys[i - 1];
            }
            keys[index] = key;
            return true;
        }

        /// <summary>
        /// O(1)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dict.TryGetValue(key, out value);
        }
        
        /// <summary>
        /// O(1)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(int index, out TValue value)
        {
            if (!keys.TryGetValue(index, out TKey key))
            {
                value = default(TValue);
                return false;
            }

            return dict.TryGetValue(key, out value);
        }

        /// <summary>
        /// O(1) bu a bit longer than <see cref="TryGetValue(TKey, out TValue)"/>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGetValue(int index, out KeyValuePair<TKey, TValue> item)
        {
            if (!keys.TryGetValue(index, out TKey key))
            {
                item = new KeyValuePair<TKey, TValue>();
                return false;
            }

            if (!dict.TryGetValue(key, out TValue value))
            {
                item = new KeyValuePair<TKey, TValue>();
                return false;
            }

            item = new KeyValuePair<TKey, TValue>(key, value);
            return true;
        }

        public void SetValue(TKey key, TValue value)
        {
            dict[key] = value;
        }

        public void SetValue(int index, TValue value)
        {
            dict[keys[index]] = value;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for(int i = 0; i < Count; i++)
            {
                TryGetValue(i, out KeyValuePair<TKey, TValue> pair);
                yield return pair;
            }
        }

        /// <summary>
        /// try to remove with the key. removing is more efficient using <see cref="TryRemove(int, out TValue)"/>.
        /// O(2N) if <paramref name="removedIndex"/> is null, O(N) otherwise
        /// </summary>
        /// <param name="key"></param>
        /// <param name="removed"></param>
        /// <param name="removedIndex">index where the key is stored, provide it if you can to avoid an enumeration</param>
        /// <returns></returns>
        public bool TryRemove(TKey key, out TValue removed, int? removedIndex = null)
        {
            if (!dict.TryRemove(key, out removed))
                return false;

            if(!removedIndex.HasValue)
            {
                foreach (var pair in keys)
                {
                    if (pair.Value.Equals(key))
                    {
                        removedIndex = pair.Key;
                        break;
                    }
                }
            }

            int max = keys.Count - 1;
            for (int i = removedIndex.Value; i < max; i++)
            {
                keys[i] = keys[i + 1];
            }
            if (!keys.TryRemove(keys.Count - 1, out _))
                throw new System.Exception("couldn't remove last key from key indexer, this instance is corrupt");

            return true;
        }

        /// <summary>
        /// most efficient way of removing an item from the list. O(N)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="removed"></param>
        /// <returns></returns>
        public bool TryRemove(int index, out TValue removed)
        {
            return TryRemove(keys[index], out removed, index);
        }

        public bool TryRemove(TValue value)
        {
            for(int i = 0; i < Count; i++)
            {
                TryGetValue(i, out TValue v);
                if (value.Equals(v))
                    return TryRemove(i, out _);
            }
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public void Clear()
        {
            dict.Clear();
            keys.Clear(); 
        }

        void IDictionary.Add(object key, object value) => TryAdd((TKey)key, (TValue)value);
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => TryAdd(key, value);
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => TryAdd(item.Key, item.Value);
        void IDictionary.Clear() => Clear();
        void ICollection<KeyValuePair<TKey, TValue>>.Clear() => Clear();
        bool IDictionary.Contains(object key) => ContainsKey((TKey)key);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            bool got = dict.TryGetValue(item.Key, out TValue v);
            return got && item.Value.Equals(v);
        }
        bool IDictionary<TKey, TValue>.ContainsKey(TKey key) => ContainsKey(key);
        void ICollection.CopyTo(Array array, int index) => throw new NotImplementedException();
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();
        IDictionaryEnumerator IDictionary.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        void IDictionary.Remove(object key) => TryRemove((TKey)key, out _);
        bool IDictionary<TKey, TValue>.Remove(TKey key) => TryRemove(key, out _);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => TryRemove(item.Key, out _);
        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => TryGetValue(key, out value);
    }
}
