using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System;
using Motio.NodeCommon;
using Motio.ObjectStoring;
using System.Collections;
using Motio.Debuging;
using Motio.NodeCommon.StandardInterfaces;
using System.Threading;
using Motio.NodeCore.Utils;
using Motio.NodeCommon.Utils;

namespace Motio.NodeCore
{
    /// <summary>
    /// <para>
    /// Holds the properties of a node and allow access via index or name
    /// </para> 
    /// <para>
    /// When adding properties to a <c>PropertyGroup</c> you need to supply 
    /// a unique name to store the property in a dictionary. This name has to be unique 
    /// whithin the <c>PropertyGroup</c>. It also should be explanatory of what property it 
    /// refers to because the user can have access to it
    /// </para>
    /// <para>
    /// This implements <see cref="INotifyCollectionChanged"/> so you can listen to get any addition or removal 
    /// </para>
    /// </summary>
    public class PropertyGroup : INotifyCollectionChanged, INotifyPropertyChanged, IHasHost, ISetParent, IList<NodePropertyBase>
    {
        //private OrderedDictionary<string, NodePropertyBase> _properties = new OrderedDictionary<string, NodePropertyBase>();
        private OrderedConcurentDictionary<string, NodePropertyBase> _properties = new OrderedConcurentDictionary<string, NodePropertyBase>();

        /// <summary>
        /// NEVER USE THIS DIRECTLY, USE THE METHODS
        /// </summary>
        private OrderedConcurentDictionary<string, NodePropertyBase> properties
        {
            get
            {
                CheckSetup();
                return _properties;
            }
        }

        /// <summary>
        /// List of KeyValuePair representing the properties, in order
        /// </summary>
        public IEnumerable<KeyValuePair<string, NodePropertyBase>> PropertiesReadOnly
        {
            get
            {
                foreach (var pair in _properties)
                    yield return pair;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        /// <summary>
        /// Node on which this group is
        /// </summary>
        public Node NodeHost { get; private set; }
        /// <summary>
        /// If the property group is not setup when trying to get a property 
        /// SetupProperties() will be called on the nodehost 
        /// </summary>
        public bool isSetup = false;
        /// <summary>
        /// Number of property in this group
        /// </summary>
        public int Count => properties.Count;
        /// <summary>
        /// Implementation of the IHasHost interface, returns NodeHost
        /// Do not set the Host if you don't know what you're doing
        /// </summary>
        object IHasHost.Host { get => NodeHost; set => NodeHost = (Node)value; }

        /// <summary>
        /// Indexer allowing access by index
        /// </summary>
        /// <param name="key">Index of the wanted property</param>
        /// <returns></returns>
        public NodePropertyBase this[int key]
        {
            get => Get<NodePropertyBase>(key);
            set => properties[key] = value;
        }

        /// <summary>
        /// Indexer allowing access by name
        /// </summary>
        /// <param name="key">The unique name of the wanted property</param>
        /// <returns></returns>
        public NodePropertyBase this[string key]
        {
            get => Get<NodePropertyBase>(key);
            //set => properties[key] = value;
        }

        [CustomSaver]
        object OnSave()
        {
            return properties
                .ToDictionary(
                    pair => pair.Key, 
                    pair => TimelineSaver.SaveObjectToJson(pair.Value));
        }

        [CustomLoader]
        void OnLoad(JObject jobj)
        {
            //the jobject contains the list of properties
            foreach(JProperty jprop in jobj.Properties())
            {
                object instance = TimelineLoader.LoadObjectFromJson(jprop, null, this);
                if (!properties.TryAdd(jprop.Name, (NodePropertyBase)instance))
                    throw new Exception("couldn't add property on load");
                //JObject collectionItem = (JObject)jprop.Value;
                //CreatableNode type = TimelineLoader.GetTypeEntry(collectionItem);
                //NodePropertyBase nodeProp = (NodePropertyBase)TimelineLoader.CreateInstance(type, this);
                //properties.Add(jprop.Name, nodeProp);
                //TimelineLoader.LoadObject(nodeProp, collectionItem);
            }
        }

        [CreateLoadInstance]
        static object CreateLoadInstance(Node parent, Type createThis)
        {
            PropertyGroup group = new PropertyGroup(parent)
            {
                isSetup = true
            };
            return group;
        }

        private bool Find(OrderedConcurentDictionary<string, NodePropertyBase> properties, string name, out NodePropertyBase result)
        {
            if (properties.TryGetValue(name, out result))
                return true;

            foreach (var pair in properties)
            {
                if (pair.Value is GroupNodeProperty group)
                {
                    if (Find(group.Properties.properties, name, out result))
                        return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Shorthand for <code>(T)propertyGroup["name"]</code>
        /// </summary>
        /// <typeparam name="T">Type to cast the resultant property to</typeparam>
        /// <param name="name">The unique name of the wanted property</param>
        /// <returns></returns>
        public T Get<T>(string name) where T : NodePropertyBase
        {
            if (!Find(properties, name, out NodePropertyBase result))
                throw new Exception("Coudln't find a property with name " + name + " in " + NodeHost.UserGivenName);

            return (T)result;
            //return (T)properties[name];
        }

        /// <summary>
        /// Shorthand for <code>(T)propertyGroup[index]</code>
        /// </summary>
        /// <typeparam name="T">Type to cast the resultant property to</typeparam>
        /// <param name="name">The unique name of the wanted property</param>
        /// <returns></returns>
        public T Get<T>(int index) where T : NodePropertyBase
        {
            return (T)properties[index];
        }

        /// <summary>
        /// </summary>
        /// <param name="nodeHost">Node on which this group will be</param>
        public PropertyGroup(Node nodeHost)
        {
            this.NodeHost = nodeHost;
            CollectionChanged += PropertyGroup_CollectionChanged;
        }

        private void PropertyGroup_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NodeHost.FindGraphicsNode(out var gAff).InvalidateAllCachedFrames(gAff);
        }

        /// <summary>
        /// Runs SetupProperties on the host if necessary
        /// </summary>
        protected virtual void CheckSetup()
        {
            if (!isSetup)
            {
                //order is important otherwise stack overflow
                isSetup = true;
                NodeHost.SetupProperties();
            }
        }

        /// <summary>
        /// wait for the given property to be created and have a static value not null
        /// </summary>
        /// <param name="name"></param>
        public void WaitForProperty(string name)
        {
            NodePropertyBase prop;
            while (!Find(properties, name, out prop))
                Thread.Sleep(1);
            while (prop.StaticValue == null)
                Thread.Sleep(1);
        }

        /// <summary>
        /// Invalidate the cache of every property in this group for the given frame. 
        /// Invalidation of the cache will cause it to be recalculated when necessary
        /// </summary>
        /// <param name="frame">Frame to invalidate the cache for</param>
        //public virtual void InvalidateFrame(int frame)
        //{
        //    //foreach(var pair in properties)
        //    //{
        //    //    pair.Value.InvalidateFrame(frame);
        //    //}
        //    //for(int i = 0; i < properties.Count; i++)
        //    //{
        //    //    properties[i].InvalidateFrame(frame);
        //    //}
        //}

        /// <summary>
        /// <para>Add the given property with a default value</para>
        /// <para>The name is supposed to be unique and will be used to retreive the property</para>
        /// </summary>
        /// <param name="name">Unique name to store this property in the dictionary</param>
        /// <param name="property">The property to add to this group</param>
        /// <param name="staticValue">Default value of thi property. The default value is necessary for cache invalidation</param>
        public void Add(string name, NodePropertyBase property, object staticValue)
        {
            AddManually(name, property);
            property.StaticValue = staticValue;
        }


        /// <summary>
        /// <para>
        /// This is the same as <see cref="Add(string, NodePropertyBase, object)"/> but without the default value.
        /// </para>
        /// 
        /// <para>
        /// Only use this to add the property without invalidating the cache, or to avoid side effects of a default value.
        /// For exemple the <c>GroupNodeProperty</c> uses this to add child properties without setting a keyframe due to the 
        /// StaticValue being set.
        /// </para>
        /// 
        /// </summary>
        /// <param name="name">Unique name to store this property in the dictionary</param>
        /// <param name="property">The property to add to this group</param>
        public void AddManually(string name, NodePropertyBase property)
        {
            //this is used to add without creating a keyframe in the GroupNodeProperty
            //we have to manually call the CollectionChanged event
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, property);

            //following line moved to Add
            //property.staticValue = staticValue;

            if (!properties.TryAdd(name, property))
                throw new Exception("couldn't add node to properties");
            CollectionChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Remove the property with <paramref name="name"/> as it's unique name
        /// careful this will not call delete on the property, 
        /// to remove properly use delete on the property it will call this.
        /// 
        /// You can't remove a property in a subgroup with this method
        /// </summary>
        /// <param name="name">Unique name of the property to remove</param>
        public void Remove(string name)
        {
            if (name == null)
                return;
            if (!properties.TryGetValue(name, out NodePropertyBase removed))
                return;
            NotifyCollectionChangedEventArgs args = 
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, removed);

            properties.TryRemove(name, out _);
            CollectionChanged?.Invoke(this, args);
        }

        internal void RemoveNoCollectionChanged(string name)
        {
            if (name == null)
                return;
            if (!properties.TryGetValue(name, out NodePropertyBase removed))
                return;

            properties.TryRemove(name, out _);
        }

        /// <summary>
        /// Remove the property at <paramref name="index"/>
        /// careful this will not call delete on the property, 
        /// to remove properly use delete on the property it will call this.
        /// 
        /// You can't remove a property in a subgroup with this method
        /// </summary>
        /// <param name="index">Index of the property to remove</param>
        public void Remove(int index)
        {
            if (!properties.TryGetValue(index, out NodePropertyBase removed))
                return;
            NotifyCollectionChangedEventArgs args =
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, removed);

            properties.TryRemove(index, out _);
            CollectionChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Remove all the properties from this group
        /// </summary>
        public void Clear()
        {
            foreach(var pair in properties)
            {
                //they delete themself from properties (by calling remove)
                pair.Value.Delete();
            }
            //for (int i = 0; i < properties.Count; i++)
            //{
            //    properties[i].Delete();
            //}
            NotifyCollectionChangedEventArgs args = 
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

            CollectionChanged?.Invoke(this, args);
        }

        public void Delete()
        {
            while(properties.Count != 0)
            {
                properties[0].Delete();
            }
            CollectionChanged = null;
            PropertyChanged = null;
        }

        /// <summary>
        /// Get the unique name for <paramref name="property"/>
        /// </summary>
        /// <param name="property">The property to get the unique name for</param>
        /// <returns>The unique name of <paramref name="property"/></returns>
        public string GetUniqueName(NodePropertyBase property)
        {
            return HostUniqueNameInGroups(property);
            //foreach (KeyValuePair<string, NodePropertyBase> pair in properties)
            //{
            //    if (property == pair.Value)
            //        return pair.Key;
            //}
            //return null;
        }

        private static string HostUniqueNameInGroups(NodePropertyBase self, PropertyGroup propertyGroup = null)
        {
            if (propertyGroup == null)
                propertyGroup = self.nodeHost.Properties;

            foreach (KeyValuePair<string, NodePropertyBase> pair in propertyGroup.PropertiesReadOnly)
            {
                if (self == pair.Value)
                    return pair.Key;
            }
            foreach (KeyValuePair<string, NodePropertyBase> pair in propertyGroup.PropertiesReadOnly)
            {
                if (pair.Value is GroupNodeProperty group)
                {
                    string inGroup = HostUniqueNameInGroups(self, group.Properties);
                    if (inGroup != null)
                        return inGroup;
                }
            }

            return null;
        }

        /// <summary>
        /// Calculates or get the cached out value at <paramref name="frame"/> for the property under <paramref name="name"/>. 
        /// Also casts the result to <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Type to cast the result to</typeparam>
        /// <param name="name">Unique name of the property to evaluate</param>
        /// <param name="frame">Frame to get the value for</param>
        /// <returns>The out value of the property under <paramref name="name"/></returns>
        public T GetValue<T>(string name, int frame)
        {
            NodePropertyBase prop = Get<NodePropertyBase>(name);
            return  (T)(prop.GetCacheOrCalculateEndOfChain(frame)?.GetChannelData(Node.PROPERTY_OUT_CHANNEL) ?? prop.StaticValue);
        }

        /// <summary>
        /// Same as <see cref="GetValue{T}(string, int)"/> but made easier to use via python
        /// </summary>
        /// <param name="name"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public object GetValue(string name, int frame)
        {
            try
            {
                NodePropertyBase prop = Get<NodePropertyBase>(name);
                return prop.GetCacheOrCalculateEndOfChain(frame)?.GetChannelData(Node.PROPERTY_OUT_CHANNEL) ?? prop.StaticValue;
            }
            catch (Exception e)
            {
                Logger.WriteLine("error getting property value:\n" + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Try to get a property named <paramref name="name"/>
        /// </summary>
        /// <param name="name">Unique name of the property to find</param>
        /// <param name="prop">If found, the property is stored here</param>
        /// <returns>True if the property was found, false otherwise</returns>
        public bool TryGetProperty(string name, out NodePropertyBase prop)
        {
            return Find(properties, name, out prop);
            //return properties.TryGetValue(name, out prop);
        }

        //interfaces implementation
        int ICollection<NodePropertyBase>.Count => Count;
        bool ICollection<NodePropertyBase>.IsReadOnly => true;
        NodePropertyBase IList<NodePropertyBase>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }
        int IList<NodePropertyBase>.IndexOf(NodePropertyBase item) => throw new NotImplementedException ();
        void IList<NodePropertyBase>.Insert(int index, NodePropertyBase item) => throw new NotImplementedException("this IList is read only");
        void IList<NodePropertyBase>.RemoveAt(int index) => Remove(index);
        void ICollection<NodePropertyBase>.Add(NodePropertyBase item) => throw new NotImplementedException("this IList is read only");
        void ICollection<NodePropertyBase>.Clear() => Clear();
        bool ICollection<NodePropertyBase>.Contains(NodePropertyBase item) => throw new NotImplementedException();
        void ICollection<NodePropertyBase>.CopyTo(NodePropertyBase[] array, int arrayIndex)
        {
            for(int i = arrayIndex; i < arrayIndex + properties.Count; i++)
            {
                array[i] = properties[i];
            }
        }

        bool ICollection<NodePropertyBase>.Remove(NodePropertyBase item)
        {
            return properties.TryRemove(item);
            //bool contained = properties.ContainsValue(item);
            //Remove(GetUniqueName(item));
            //return contained;
        }

        IEnumerator<NodePropertyBase> IEnumerable<NodePropertyBase>.GetEnumerator()
        {
            for(int i = 0; i < properties.Count; i++)
            {
                yield return properties[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < properties.Count; i++)
            {
                yield return properties[i];
            }
        }

        public void SetParent(object parent)
        {
            NodeHost = (Node)parent;
        }
    }
}
