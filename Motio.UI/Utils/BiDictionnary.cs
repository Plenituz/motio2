using System;
using System.Collections.Generic;

namespace Motio.UI.Utils
{
    /// <summary>
    /// a dictionnary that works by key and value 
    /// </summary>
    /// <typeparam name="TFirst"></typeparam>
    /// <typeparam name="TSecond"></typeparam>
    public class BiDictionary<TFirst, TSecond>
    {
        Dictionary<TFirst, TSecond> firstToSecond = new Dictionary<TFirst, TSecond>();
        Dictionary<TSecond, TFirst> secondToFirst = new Dictionary<TSecond, TFirst>();

        public void Add(TFirst first, TSecond second)
        {
            bool f = firstToSecond.ContainsKey(first);
            bool s = secondToFirst.ContainsKey(second);
            if(f)
                throw new ArgumentException("Duplicate first :" + first);
            if(s)
                throw new ArgumentException("Duplicate second :" + second);
            firstToSecond.Add(first, second);
            secondToFirst.Add(second, first);
        }

        public void RemoveByFirst(TFirst first)
        {
            TSecond second = firstToSecond[first];
            firstToSecond.Remove(first);
            secondToFirst.Remove(second);
        }

        public void RemoveBySecond(TSecond second)
        {
            TFirst first = secondToFirst[second];
            secondToFirst.Remove(second);
            firstToSecond.Remove(first);
        }

        public bool ContainsFirstKey(TFirst key)
        {
            return firstToSecond.ContainsKey(key);
        }

        internal bool ContainsSecondKey(TSecond ellipse)
        {
            return secondToFirst.ContainsKey(ellipse);
        }

        public IEnumerable<TFirst> KeysFirst()
        {
            return firstToSecond.Keys;
        }

        public IEnumerable<TSecond> KeysSecond()
        {
            return secondToFirst.Keys;
        }

        public IEnumerable<TResult> Select<TResult>(Func<KeyValuePair<TFirst, TSecond>, TResult> func)
        {
            foreach(KeyValuePair<TFirst, TSecond> pair in firstToSecond)
            {
                yield return func(pair);
            }
        }

        public TSecond GetByFirst(TFirst first)
        {
            return firstToSecond[first];
        }

        public bool TryGetByFirst(TFirst first, out TSecond second)
        {
            return firstToSecond.TryGetValue(first, out second);
        }

        public TFirst GetBySecond(TSecond second)
        {
            return secondToFirst[second];
        }

        public bool TryGetBySecond(TSecond second, out TFirst first)
        {
            return secondToFirst.TryGetValue(second, out first);
        }

        public void Clear()
        {
            secondToFirst.Clear();
            firstToSecond.Clear();
        }
    }
}
