using Motio.Debuging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Motio.UI.Utils
{
    public class DictionaryProxy<OriginalKeyType, OriginalValueType, ProxyValueType>
        : IDictionary<OriginalKeyType, ProxyValueType>, INotifyCollectionChanged, INotifyPropertyChanged
        where ProxyValueType : IProxy<OriginalValueType>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ProxyValueType this[OriginalKeyType key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ICollection<OriginalKeyType> Keys => throw new NotImplementedException();
        public ICollection<ProxyValueType> Values => throw new NotImplementedException();
        public int Count => throw new NotImplementedException();
        public bool IsReadOnly => throw new NotImplementedException();

        Dictionary<OriginalKeyType, ProxyValueType> proxyDict = new Dictionary<OriginalKeyType, ProxyValueType>();
        /// <summary>
        /// reference to the original dict
        /// </summary>
        IDictionary<OriginalKeyType, OriginalValueType> originalDict;
        INotifyCollectionChanged collectionEvent;
        public bool isEventLinkedToDict = true;

        public DictionaryProxy(IDictionary<OriginalKeyType, OriginalValueType> originalDict, INotifyCollectionChanged collectionEvent)
        {
            this.originalDict = originalDict;
            this.collectionEvent = collectionEvent;

            this.collectionEvent.CollectionChanged += CollectionEvent_CollectionChanged;

            foreach(KeyValuePair<OriginalKeyType, OriginalValueType> pair in originalDict)
            {
                ProxyValueType proxy = ProxyStatic.CreateProxy<ProxyValueType>(pair.Value);
                proxyDict.Add(pair.Key, proxy);
            }
        }

        //ProxyValueType CreateProxy(OriginalValueType original)
        //{
        //    if (ProxyStatic.original2proxy.ContainsFirstKey(original))
        //    {
        //        return ProxyStatic.GetProxyOf<ProxyValueType>(original);
        //    }
        //    string typeString = "Motio.ViewModels." + original.GetType().Name + "ViewModel";
        //    Type proxyType = Type.GetType(typeString);
        //    if(proxyType == null)
        //    {
        //        proxyType = typeof(ProxyValueType);
        //        Logger.WriteLine("couldn't find proxy type for " + original.GetType());
        //    }

        //    return (ProxyValueType)Activator.CreateInstance(proxyType, original);
        //}

        private void CollectionEvent_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs ev = null;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        List<KeyValuePair<OriginalKeyType, ProxyValueType>> newItems
                            = new List<KeyValuePair<OriginalKeyType, ProxyValueType>>();
                        for(int i = 0; i < e.NewItems.Count; i++)
                        {
                            var pair = (KeyValuePair<OriginalKeyType, OriginalValueType>)e.NewItems[i];
                            ProxyValueType proxy = ProxyStatic.CreateProxy<ProxyValueType>(pair.Value);
                            proxyDict.Add(pair.Key, proxy);
                            newItems.Add(new KeyValuePair<OriginalKeyType, ProxyValueType>(pair.Key, proxy));
                        }
                        ev = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        throw new NotImplementedException();
                    }
#pragma warning disable CS0162 // Unreachable code detected
                    break;
#pragma warning restore CS0162 // Unreachable code detected
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<KeyValuePair<OriginalKeyType, ProxyValueType>> removedItems 
                            = new List<KeyValuePair<OriginalKeyType, ProxyValueType>>();
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            var pair = (KeyValuePair<OriginalKeyType, OriginalValueType>)e.OldItems[i];
                            dynamic proxy = proxyDict[pair.Key];
                            proxy.Delete();
                            proxyDict.Remove(pair.Key);
                            removedItems.Add(new KeyValuePair<OriginalKeyType, ProxyValueType>(pair.Key, (ProxyValueType)proxy));
                        }
                        ev = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        throw new NotImplementedException();
                    }
#pragma warning disable CS0162 // Unreachable code detected
                    break;
#pragma warning restore CS0162 // Unreachable code detected
                case NotifyCollectionChangedAction.Reset:
                    {
                        ev = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                        foreach (KeyValuePair<OriginalKeyType, ProxyValueType> proxy in proxyDict)
                        {
                            ((dynamic)proxy.Value).Delete();//computer intensive but no other way ?
                        }
                        proxyDict.Clear();
                    }
                    break;
            }
            //we use a custom made event with the proxy instead of the real object 
            //because otherwise the datacontext of objects linked to this list 
            //will be set to the original not the proxy (only the objects added after the list was read for the first time)
            CollectionChanged?.Invoke(sender, ev);
        }

        public void Add(OriginalKeyType key, ProxyValueType value)
        {
            originalDict.Add(key, value.Original);
            if (!isEventLinkedToDict)
                proxyDict.Add(key, value);
        }

        public void Add(KeyValuePair<OriginalKeyType, ProxyValueType> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            originalDict.Clear();
            if (!isEventLinkedToDict)
                proxyDict.Clear();
        }

        public bool Contains(KeyValuePair<OriginalKeyType, ProxyValueType> item)
        {
            return ContainsKey(item.Key);
        }

        public bool ContainsKey(OriginalKeyType key)
        {
            return originalDict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<OriginalKeyType, ProxyValueType>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<OriginalKeyType, ProxyValueType>> GetEnumerator()
        {
            return proxyDict.GetEnumerator();
        }

        public bool Remove(OriginalKeyType key)
        {
            if (!isEventLinkedToDict)
                proxyDict.Remove(key);
            return originalDict.Remove(key);
        }

        public bool Remove(KeyValuePair<OriginalKeyType, ProxyValueType> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(OriginalKeyType key, out ProxyValueType value)
        {
            if (originalDict.ContainsKey(key))
            {
                value = proxyDict[key];
                return true;
            }
            else
            {
                value = default(ProxyValueType);
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return proxyDict.GetEnumerator();
        }
    }
}
