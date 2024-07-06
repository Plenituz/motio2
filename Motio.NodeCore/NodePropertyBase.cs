using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections;
using Motio.ObjectStoring;
using Motio.Debuging;
using Motio.NodeCore.Utils;
using Motio.NodeCore.Interfaces;
using System.Linq;
using System.Collections.Specialized;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.PythonRunning;
using IronPython.Runtime.Types;

namespace Motio.NodeCore
{
    /// <summary>
    /// Base class for any Node Property
    /// </summary>
    public abstract class NodePropertyBase : INotifyPropertyChanged, IHasHost, ISetParent, IErrorProne, IHasAttached<PropertyAffectingNode>, ICacheHost
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action Deleted;

        /// <summary>
        /// Node this property is on
        /// </summary>
        public Node nodeHost;
        /// <summary>
        /// The name displayed in the property panel
        /// </summary>
        [SaveMe]
        public string Name { get; set; }
        /// <summary>
        /// If this is true the interface will allow AnimationPropertyNodes to be added on this property
        /// </summary>
        public abstract bool IsKeyframable { get; }

        //IErrorProne
        /// <summary>
        /// Relay object that implements IErrorProne properly, probably an instance of <see cref="ErrorProneImpl"/>
        /// </summary>
        protected IErrorProne ErrorProneHandler { get; set; } = new ErrorProneImpl();
        /// <summary>
        /// Ordered list of the errors on this property
        /// </summary>
        public IEnumerable<string> Errors => ErrorProneHandler.Errors;
        /// <summary>
        /// Ordered list of the warnings on this property
        /// </summary>
        public IEnumerable<string> Warnings => ErrorProneHandler.Warnings;
        /// <summary>
        /// True if this property has at least one error
        /// </summary>
        public bool HasErrors => ErrorProneHandler.HasErrors;
        /// <summary>
        /// True if this property has at least one warning
        /// </summary>
        public bool HasWarnings => ErrorProneHandler.HasWarnings;
        //IHasHost
        object IHasHost.Host { get => nodeHost; set => nodeHost = (Node)value; }


        /// <summary>
        /// <para>
        /// This is the unique name of this property in the host's <see cref="PropertyGroup"/> property.
        /// Careful, this iterates over the dictionnary. 
        /// </para>
        /// <para>
        /// Shorthand for <c>nodeHost.Properties.GetUniqueName(this)</c>
        /// </para>
        /// </summary>
        public string HostUniqueName => nodeHost.Properties.GetUniqueName(this);

        public IFrameRange CalculationRange => GetFullRange();

        /// <summary>
        /// The nodes evaluated before any other nodes and not shown in the UI.
        /// Example : the animation node (holding the keyframes). 
        /// The in value is the result of the evaluation of the hiddenNodes
        /// </summary>
        [SaveMe]
        public ObservableCollection<PropertyAffectingNode> hiddenNodes = new ObservableCollection<PropertyAffectingNode>();

        /// <summary>
        /// The user added nodes, displayed in the UI and evaluated after the hidden nodes, 
        /// with the same <see cref="DataFeed"/>. The out value is the result of the evaluation of the attachesNodes
        /// </summary>
        [SaveMe]
        public ObservableCollection<PropertyAffectingNode> attachedNodes = new ObservableCollection<PropertyAffectingNode>();

        /// <summary>
        /// The value before any node, even the hidden ones.
        /// It is passed in the DataFeed to the first node in the <see cref="Node.PROPERTY_OUT_CHANNEL"/> channel.
        /// </summary>
        [SaveMe]
        public abstract object StaticValue { get; set; }

        /// <summary>
        /// Description showed to the user on hover over the property name
        /// </summary>
        [SaveMe]
        public string Description { get; set; }

        IEnumerable<PropertyAffectingNode> IHasAttached<PropertyAffectingNode>.AttachedMembers => attachedNodes;
        IEnumerable IHasAttached.AttachedMembers => attachedNodes;
        IEnumerable<ICacheMember> ICacheHost.Members => attachedNodes;

        /// <summary>
        /// The cache for the value just after the hidden nodes, diplayed in the UI as the base value 
        /// </summary>
        //protected GenericCache<object> inValues;

        /// <summary>
        /// The cache for the value at the end of the attachedNodes chain
        /// </summary>
        //protected GenericCache<object> outValues;

        //protected SyncNodeChainCache chainCache;

        private void AttachedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if nodes move or change we need to invalidate parent
            EventHall.Trigger(this, GetPath() + ".AttachedNodes", "CollectionChanged", e);
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        PropertyAffectingNode added = (PropertyAffectingNode)e.NewItems[0];
                        nodeHost.GetTimeline().CacheManager.AddToChain(this, added, e.NewStartingIndex + hiddenNodes.Count);
                        //chainCache.AddAt(added, e.NewStartingIndex + hiddenNodes.Count);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        nodeHost.GetTimeline().CacheManager.MoveInChain(this, e.OldStartingIndex + hiddenNodes.Count, e.NewStartingIndex + hiddenNodes.Count);
                        //chainCache.Move(e.OldStartingIndex + hiddenNodes.Count, e.NewStartingIndex + hiddenNodes.Count);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        if (e.OldItems.Count > 1)
                            throw new ArgumentException();
                        //PropertyAffectingNode removed = (PropertyAffectingNode)e.OldItems[0];
                        nodeHost.GetTimeline().CacheManager.RemoveFromChain(this, e.OldStartingIndex + hiddenNodes.Count);
                        //chainCache.Remove(e.OldStartingIndex + hiddenNodes.Count);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        PropertyAffectingNode added = (PropertyAffectingNode)e.NewItems[0];
                        //PropertyAffectingNode removed = (PropertyAffectingNode)e.OldItems[0];
                        nodeHost.GetTimeline().CacheManager.RemoveFromChain(this, e.OldStartingIndex + hiddenNodes.Count);
                        nodeHost.GetTimeline().CacheManager.AddToChain(this, added, e.NewStartingIndex + hiddenNodes.Count);
                        //chainCache.Remove(e.OldStartingIndex + hiddenNodes.Count);
                        //chainCache.AddAt(added, e.NewStartingIndex + hiddenNodes.Count);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        nodeHost.GetTimeline().CacheManager.RemoveLastsFromChain(this, hiddenNodes.Count);
                        //while (chainCache.Count != hiddenNodes.Count)
                        //{
                        //    chainCache.Remove(chainCache.Count - 1);
                        //}
                    }
                    break;
            }
        }

        private void HiddenNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EventHall.Trigger(this, GetPath() + ".AttachedNodes", "CollectionChanged", e);
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        PropertyAffectingNode added = (PropertyAffectingNode)e.NewItems[0];
                        nodeHost.GetTimeline().CacheManager.AddToChain(this, added, e.NewStartingIndex);
                        //chainCache.AddAt(added, e.NewStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        nodeHost.GetTimeline().CacheManager.MoveInChain(this, e.OldStartingIndex, e.NewStartingIndex);
                        //chainCache.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        //PropertyAffectingNode removed = (PropertyAffectingNode)e.OldItems[0];
                        nodeHost.GetTimeline().CacheManager.RemoveFromChain(this, e.OldStartingIndex);
                        //chainCache.Remove(e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        PropertyAffectingNode added = (PropertyAffectingNode)e.NewItems[0];
                        //PropertyAffectingNode removed = (PropertyAffectingNode)e.OldItems[0];
                        nodeHost.GetTimeline().CacheManager.RemoveFromChain(this, e.OldStartingIndex);
                        nodeHost.GetTimeline().CacheManager.AddToChain(this, added, e.NewStartingIndex);
                        //chainCache.Remove(e.OldStartingIndex);
                        //chainCache.AddAt(added, e.NewStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        nodeHost.GetTimeline().CacheManager.RemoveFirstsFromChain(this, attachedNodes.Count);
                        //while (chainCache.Count != attachedNodes.Count)
                        //{
                        //    chainCache.Remove(0);
                        //}
                    }
                    break;
            }
        }

        /// <summary>
        /// In this function you should create and add to <see cref="hiddenNodes"/> the node responsible
        /// for the keyframe animations
        /// </summary>
        public abstract void CreateAnimationNode();

        private IFrameRange GetFullRange()
        {
            FrameRange range = new FrameRange();
            for(int i = 0; i < hiddenNodes.Count; i++)
            {
                range.Add(hiddenNodes[i].CalculationRange);
            }

            for (int i = 0; i < attachedNodes.Count; i++)
            {
                range.Add(attachedNodes[i].CalculationRange);
            }
            return range;
        }

        /// <summary>
        /// This is called when this property is about to be destroyed. To prevent memory leaks you should
        /// unsubscribe from all events here.
        /// If you can, call Delete() on the <see cref="PropertyGroup"/> instead
        /// </summary>
        public virtual void Delete()
        {
            Deleted?.Invoke();
            while (hiddenNodes.Count != 0)
                hiddenNodes[0].Delete();
            while (attachedNodes.Count != 0)
                attachedNodes[0].Delete();
            nodeHost.Properties.RemoveNoCollectionChanged(HostUniqueName);
        }

        /// <summary>
        /// Returns the path of this node in the shape of "uuid.propertyName"
        /// This path is garanteed to be unique in the entire application
        /// </summary>
        /// <returns>the path of this node in the shape of "uuid.propertyName"</returns>
        public string GetPath()
        {
            return nodeHost.UUID + "." + HostUniqueName;
        }

        [CreateLoadInstance]
        static object CreateLoadInstance(PropertyGroup parent, Type createThis)
        {
            return Activator.CreateInstance(createThis, new object[] { parent.NodeHost });
        }

        /// <summary>
        /// Constructor for the loading from json. When creating a property you should use <see cref="NodePropertyBase(Node, string, string)"/>
        /// </summary>
        /// <param name="nodeHost"></param>
        public NodePropertyBase(Node nodeHost)
        {
            this.nodeHost = nodeHost;

            SyncNodeChainCache chainCache = new SyncNodeChainCache(NewConfiguredFeed);
            chainCache.Prepare += ChainCache_Prepare;
            chainCache.AnyCacheCleared += ChainCache_AnyCacheCleared;
            nodeHost.GetTimeline().CacheManager.AddCache(this, chainCache);

            PropertyChanged += NodePropertyBase_PropertyChanged;
            attachedNodes.CollectionChanged += AttachedNodes_CollectionChanged;
            hiddenNodes.CollectionChanged += HiddenNodes_CollectionChanged;
        }

        private void ChainCache_AnyCacheCleared(int frame, int frameNodeIndex)
        {
            if (frame == -1)
                InvalidateAllUpChain(nodeHost);
            else
                InvalidateFrameUpChain(nodeHost, frame);
        }

        private void InvalidateAllUpChain(Node node)
        {
            if(node is GraphicsAffectingNode gAff)
            {
                gAff.nodeHost.InvalidateAllCachedFrames(gAff);
                return;
            }
            if(node is PropertyAffectingNode pAff)
            {
                pAff.propertyHost.InvalidateAllCachedFrames(pAff);
                InvalidateAllUpChain(pAff.propertyHost.nodeHost);
                return;
            }
            throw new Exception("Invalidating node chain encountered a " + node 
                + " only GraphicsAffectingNode and PropertyAffectingNode are expected");
        }

        private void InvalidateFrameUpChain(Node node, int frame)
        {
            if (node is GraphicsAffectingNode gAff)
            {
                gAff.nodeHost.InvalidateCache(frame, gAff);
                return;
            }
            if (node is PropertyAffectingNode pAff)
            {
                pAff.propertyHost.InvalidateCache(frame, pAff);
                InvalidateFrameUpChain(pAff.propertyHost.nodeHost, frame);
                return;
            }
            throw new Exception("Invalidating node chain encountered a " + node
                + " only GraphicsAffectingNode and PropertyAffectingNode are expected");
        }

        /// <summary>
        /// get the index in the nodeChain cache of the given node 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private int FindCacheIndex(PropertyAffectingNode node)
        {
            int indexNode = hiddenNodes.IndexOf(node);
            if (indexNode == -1)
            {
                indexNode = attachedNodes.IndexOf(node);
                if (indexNode != -1)
                    indexNode += hiddenNodes.Count;
            }
            return indexNode;
        }

        /// <summary>
        /// this will cause all the nodes up the chain to be invalidated as well
        /// </summary>
        /// <param name="fromNode"></param>
        public virtual void InvalidateAllCachedFrames(PropertyAffectingNode fromNode)
        {
            int indexNode = FindCacheIndex(fromNode);
            if (indexNode == -1)
                throw new Exception("tried to invalidate a propertynode (" + fromNode + ") not on this node (" + this + ")");

            nodeHost.GetTimeline().CacheManager.ClearAllFramesAfter(this, indexNode);
        }

        /// <summary>
        /// this will cause all the nodes up the chain to be invalidated as well
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="fromNode"></param>
        public virtual void InvalidateCache(int frame, PropertyAffectingNode fromNode)
        {
            int indexNode = FindCacheIndex(fromNode);
            if (indexNode == -1)
                throw new Exception("tried to invalidate a propertynode (" + fromNode + ") not on this node (" + this + ")");
            nodeHost.GetTimeline().CacheManager.ClearCacheAfter(this, indexNode, frame);
        }

        private void ChainCache_Prepare()
        {
            for (int i = 0; i < hiddenNodes.Count; i++)
            {
                if (hiddenNodes[i].Enabled)
                    hiddenNodes[i].Prepare();
            }
            for (int i = 0; i < attachedNodes.Count; i++)
            {
                if (attachedNodes[i].Enabled)
                    attachedNodes[i].Prepare();
            }
        }

        /// <summary>
        /// Constructor to initialise the description and name. This is the prefered constructor to use 
        /// in a node
        /// </summary>
        /// <param name="nodeHost"></param>
        /// <param name="description"></param>
        /// <param name="name"></param>
        public NodePropertyBase(Node nodeHost, string description, string name) 
            : this(nodeHost)
        {
            Description = description;
            this.Name = name;
        }

        /// <summary>
        /// This is called when any property of this object changes.
        /// By default it invalidates the parent node when <see cref="StaticValue"/> changes
        /// </summary>
        /// <param name="sender">most likely this</param>
        /// <param name="e">contains the name of the property that changed</param>
        protected virtual void NodePropertyBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EventHall.Trigger(this, GetPath(), e.PropertyName);
            if(e.PropertyName.Equals(nameof(StaticValue)))
            {
                nodeHost.GetTimeline().CacheManager.ClearAllFramesAfter(this, 0);
            }
        }

        /// <summary>
        /// Ask the nodeHost if the given value is valid for this property
        /// and set errors accordingly
        /// </summary>
        /// <param name="value">value to validate</param>
        public virtual void ValidateValue(object value)
        {
            Exception ex = nodeHost.ValidatePropertyValue(this, value);
            //manually set/clear error because id 0 reserved for this; see SetError function lower in this file
            if(ex != null)
            {
                ErrorProneHandler.SetError(0, ex.Message);
                OnPropertyChanged(nameof(HasErrors));
            }
            else
            {
                ErrorProneHandler.ClearError(0);
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        public virtual DataFeed GetCache(int frame, int nodeNb)
        {
            if (hiddenNodes.Count + attachedNodes.Count == 0)
                return NewConfiguredFeed();
            
            if (nodeHost.GetTimeline().CacheManager.TryGetCache(this, nodeNb, frame, out DataFeed cache))
            {
                return cache;
            }
            return null;
        }

        /// <summary>
        /// in value is the value after the hidden nodes
        /// </summary>
        /// <returns></returns>
        public virtual DataFeed GetCacheOrCalculateInValue(int frame)
        {
            if (hiddenNodes.Count == 0)
                return NewConfiguredFeed();
            nodeHost.GetTimeline().CacheManager.StartSingleFrame(this, frame, 0, hiddenNodes.Count);
            return GetCache(frame, hiddenNodes.Count - 1);
        }

        public virtual DataFeed GetCacheOrCalculateEndOfChain(int frame)
        {
            if (hiddenNodes.Count + attachedNodes.Count == 0)
                return NewConfiguredFeed();
            nodeHost.GetTimeline().CacheManager.StartSingleFrame(this, frame, 0);
            return GetCache(frame, hiddenNodes.Count + attachedNodes.Count - 1);
        }

        public virtual DataFeed GetCachedEndOfChain(int frame)
        {
            return GetCache(frame, hiddenNodes.Count + attachedNodes.Count - 1);
        }

        private DataFeed NewConfiguredFeed()
        {
            DataFeed dataFeed = new DataFeed();
            dataFeed.EnforceChannelType(Node.PROPERTY_OUT_CHANNEL, StaticValue.GetType());
            dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, StaticValue);
            return dataFeed;
        }

        /// <summary>
        /// Tries to attach the node. If the node is node allowed (not the right type)
        /// this will not attach the node and return false
        /// </summary>
        /// <param name="node">The node to attach</param>
        /// <returns>True if the node was successfully attached</returns>
        public virtual bool TryAttachNode(PropertyAffectingNode node)
        {
            Type propType = StaticValue.GetType();
            IList<Type> allowedTypes = GetAcceptedPropertyTypes(node);
            bool allowed = ContainsTypeOrParentType(allowedTypes, propType);
            if (allowed)
            {
                //order is important
                node.AttachToProperty(this);
                attachedNodes.Add(node);
                return true;
            }
            else
            {
                string acceptedStr = allowedTypes.Select(t => t.ToString()).Aggregate((p, n) => p + " " + n);
                Logger.WriteLine("the node " + node.GetType() + " can't go on " + GetType()
                    + " only [" + acceptedStr + "] are accepted by this node");
                return false;
            }
        }

        public IList<Type> GetAcceptedPropertyTypes(PropertyAffectingNode node)
        {
            IList<Type> allowedTypes = null;
            if (node.GetType().ToString().Contains("IronPython"))
            {
                allowedTypes = GetAcceptedPropertyTypes(Python.Engine.Operations.GetMember(node, "__class__"));
            }
            else
            {
                allowedTypes = GetAcceptedPropertyTypes(node.GetType());
            }
            if (allowedTypes == null)
            {
                Logger.WriteLine("did not find AcceptedPropertyTypes on " + node
                    + ", considering it accepts everything");
                allowedTypes = new Type[] { typeof(object) };
            }
            
            return allowedTypes;
        }

        public static IList<Type> GetAcceptedPropertyTypes(Type type)
        {
            return (IList<Type>)type.GetField("AcceptedPropertyTypes")?.GetValue(null);
        }

        public static IList<Type> GetAcceptedPropertyTypes(PythonType pythonType)
        {
            dynamic d = Python.Engine.Operations.GetMember(pythonType, "acceptedPropertyTypes");
            return ((IList)d).Cast<Type>().ToList();
        }

        /// <summary>
        /// Attach the given <paramref name="node"/> has an hidden node without checking 
        /// compatibility
        /// </summary>
        /// <param name="node">The node to add to <see cref="hiddenNodes"/></param>
        public virtual void ForceAttachHiddenNode(PropertyAffectingNode node)
        {
            node.AttachToProperty(this);
            hiddenNodes.Add(node);
        }

        public static bool ContainsTypeOrParentType(IList<Type> list, Type accepted)
        {
            for(int i = 0; i < list.Count; i++)
            {
                if(list[i].IsAssignableFrom(accepted))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Call this to trigger <see cref="PropertyChanged"/>
        /// </summary>
        /// <param name="propertyName"></param>
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Set an error at <paramref name="id"/>.
        /// <strong>CAREFULL ID 0 is <i>RESERVED</i> for the property validation</strong>
        /// </summary>
        /// <param name="id">id to store the error under</param>
        /// <param name="msg">description of the error for the user to read</param>
        public void SetError(int id, string msg)
        {
            if (id == 0)
                throw new Exception("You cant use the error id 0, it's reserved for the property validation");
            ErrorProneHandler.SetError(id, msg);
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Set a warning at <paramref name="id"/>.
        /// </summary>
        /// <param name="id">id to store the warning under</param>
        /// <param name="msg">description of the warning for the user to read</param>
        public void SetWarning(int id, string msg)
        {
            ErrorProneHandler.SetWarning(id, msg);
            OnPropertyChanged(nameof(HasWarnings));
        }

        /// <summary>
        /// Check if and error with <paramref name="id"/> exists
        /// </summary>
        /// <param name="id">id of the error</param>
        /// <returns>True if and error has this id</returns>
        public bool HasError(int id)
        {
            return ErrorProneHandler.HasError(id);
        }

        /// <summary>
        /// Check if and warning with <paramref name="id"/> exists
        /// </summary>
        /// <param name="id">id of the warning</param>
        /// <returns>True if and warning has this id</returns>
        public bool HasWarning(int id)
        {
            return ErrorProneHandler.HasWarning(id);
        }

        /// <summary>
        /// Remove the error with <paramref name="id"/>
        /// </summary>
        /// <param name="id">id of the error to clear</param>
        public void ClearError(int id)
        {
            if (id == 0)
                throw new Exception("You cant use the error id 0, it's reserved for the property validation");
            ErrorProneHandler.ClearError(id);
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Remove the warning with <paramref name="id"/>
        /// </summary>
        /// <param name="id">id of the warning to clear</param>
        public void ClearWarning(int id)
        {
            ErrorProneHandler.ClearWarning(id);
            OnPropertyChanged(nameof(HasWarnings));
        }

        public void SetParent(object parent)
        {
            nodeHost = (Node)parent;
        }

        public void ReplaceMemberAt(int index, object member)
        {
            attachedNodes[index] = (PropertyAffectingNode)member;
        }

        int ICacheHost.IndexOf(ICacheMember member)
        {
            if (!(member is PropertyAffectingNode pAff))
                throw new Exception("cant get the index of a not PropertyAffectingNode");
            return attachedNodes.IndexOf(pAff);
        }
    }
}
