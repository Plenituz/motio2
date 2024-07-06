using Motio.Debuging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;

namespace Motio.UI.Utils
{
    public class ListProxy<OriginalType, ProxyType> : ICollection<IProxy<OriginalType>>, INotifyCollectionChanged, INotifyPropertyChanged
        where ProxyType : IProxy<OriginalType>
        where OriginalType : class
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// reference to the original list 
        /// </summary>
        public IList<OriginalType> originalList;
        /// <summary>
        /// list of proxy objects
        /// </summary>
        private IList<IProxy<OriginalType>> proxyList = new List<IProxy<OriginalType>>();
        public INotifyCollectionChanged originalEvent;
        public bool isEventLinkedToList = true;

        public int Count => originalList.Count;
        public bool IsReadOnly => originalList.IsReadOnly;

        public ListProxy(IList<OriginalType> originalList, INotifyCollectionChanged originalEvent)
        {
            this.originalList = originalList;
            this.originalEvent = originalEvent;

            //populate the list of proxy
#if false
            try
            {
#endif
                foreach (OriginalType original in originalList)
                {
                    ProxyType proxy = ProxyStatic.CreateProxy<ProxyType>(original);
                    proxyList.Add(proxy);
                }
#if false
            }
            catch (Exception e)
            {
                MessageBox.Show("error creating proxy:\n" + e);
            }
#endif
            //add after the foreach loop otherwise there is duplicates added 
            this.originalEvent.CollectionChanged += OriginalList_CollectionChanged;
        }

        private void OriginalList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs ev = null;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        List<ProxyType> newItems = new List<ProxyType>();
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            OriginalType original = (OriginalType)e.NewItems[i];
                            ProxyType proxy = ProxyStatic.CreateProxy<ProxyType>(original);
                            proxyList.Add(proxy);
                            newItems.Add(proxy);
                        }
                        ev = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        if(e.OldItems.Count > 1)
                            throw new NotImplementedException();
                        IProxy<OriginalType> moved = proxyList[e.OldStartingIndex];
                        proxyList.RemoveAt(e.OldStartingIndex);
                        proxyList.Insert(e.NewStartingIndex, moved);
                        ev = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, moved, e.NewStartingIndex, e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<ProxyType> removedItems = new List<ProxyType>();
                        int startingIndex = -1;
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            for(int k = 0; k < proxyList.Count; k++)
                            {
                                ProxyType proxy = (ProxyType)proxyList[k];
                                if (proxy.Original == (OriginalType)e.OldItems[i])
                                {
                                    startingIndex = k;
                                    removedItems.Add(proxy);
                                    proxyList.Remove(proxy);
                                    ((dynamic)proxy).Delete();//computer intensive but no other way ?
                                    break;
                                }
                            }
                        }
                        ev = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, startingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        if(e.NewItems.Count > 1 || e.OldItems.Count > 1)
                            throw new NotImplementedException();

                        dynamic oldItem = proxyList[e.OldStartingIndex];
                        ProxyType newItem = ProxyStatic.CreateProxy<ProxyType>((OriginalType)e.NewItems[0]);

                        proxyList[e.OldStartingIndex] = newItem;
                        oldItem.Delete();

                        ev = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        ev = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                        foreach(IProxy<OriginalType> proxy in proxyList)
                        {
                            ((dynamic)proxy).Delete();//computer intensive but no other way ?
                        }
                        proxyList.Clear();
                    }
                    break;
            }
            //we use a custom made event with the proxy instead of the real object 
            //because otherwise the datacontext of objects linked to this list 
            //will be set to the original not the proxy (only the objects added after the list was read for the first time)
            Application.Current.Dispatcher.Invoke(() =>
            {
                //the proylist might be changed from another thread, make sure we send the event in the ui thread
                CollectionChanged?.Invoke(sender, ev);
            });
        }

        public void Add(IProxy<OriginalType> item)
        {
            originalList.Add(item.Original);
            if(!isEventLinkedToList)
                proxyList.Add(item);
        }

        public void Clear()
        {
            originalList.Clear();
            if (!isEventLinkedToList)
                proxyList.Clear();
        }

        public bool Contains(IProxy<OriginalType> item)
        {
            return originalList.Contains(item.Original);
        }

        public void CopyTo(IProxy<OriginalType>[] array, int arrayIndex)
        {
            proxyList.CopyTo(array, arrayIndex);
        }

        public bool Remove(IProxy<OriginalType> item)
        {
            if (!isEventLinkedToList)
                proxyList.Remove(item);
            return originalList.Remove(item.Original);
        }

        public IEnumerator<IProxy<OriginalType>> GetEnumerator()
        {
            return proxyList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return proxyList.GetEnumerator();
        }
    }
}
