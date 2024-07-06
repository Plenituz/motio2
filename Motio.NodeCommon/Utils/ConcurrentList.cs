using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Motio.NodeCommon.Utils
{
    /// <summary>
    /// a fake list that can be accessed/modify concurently. fake because it's based on the ConcurrentDictionary
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentList<T> 
    {
        private ConcurrentDictionary<int, T> fakeList = new ConcurrentDictionary<int, T>();

        public int Count => fakeList.Count;

        public T this[int index]
        {
            get => fakeList[index];
            set => fakeList[index] = value;
        }

        public IEnumerable<T> Enumerate => fakeList.Values;

        public void Add(T item)
        {
            bool added = fakeList.TryAdd(fakeList.Count, item);
            if (!added)
                throw new Exception("couldn't add node to concurent list");
        }

        public bool TryGetValue(int index, out T value)
        {
            return fakeList.TryGetValue(index, out value);
        }

        public void AddAt(T item, int index)
        {
            for (int i = fakeList.Count - 1; i >= index; i--)
            {
                fakeList[i + 1] = fakeList[i];
            }
            fakeList[index] = item;
        }

        public void Remove(int index)
        {
            for (int i = index; i < fakeList.Count - 1; i++)
            {
                fakeList[i] = fakeList[i + 1];
            }
            fakeList.TryRemove(fakeList.Count - 1, out _);
        }

        public void RemoveAll()
        {
            fakeList.Clear();
        }

        public void Move(int indexToMove, int moveTo)
        {
            T data = fakeList[indexToMove];
            Remove(indexToMove);
            AddAt(data, moveTo);
        }
    }
}
