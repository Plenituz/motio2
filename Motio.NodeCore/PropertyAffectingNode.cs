using Motio.NodeCommon;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.NodeCore.Utils;
using Motio.ObjectStoring;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Motio.NodeCore
{
    /// <summary>
    /// <para>
    /// This is the base class for every node that goes on a property
    /// </para>
    /// <para>
    /// When impleting this don't forget to add a public static property named ClassNameStatic, it should have a getter 
    /// that returns the name to display in the node list to the user. It's also better if ClassName returns ClassNameStatic.
    /// <code>
    /// public override string ClassName => ClassNameStatic;
    /// public static string ClassNameStatic => "My Node Name";
    /// </code>
    /// </para>
    /// </summary>
    public abstract class PropertyAffectingNode : Node, IHasHost, ISetParent, ICacheMember
    {
        /// <summary>
        /// The property this node is on
        /// </summary>
        public NodePropertyBase propertyHost { get; protected set; }

        /// <summary>
        /// This is the name of this node type. It should be user readable and will be displayed at several places, 
        /// such as the node list of the property panel. It's recommended to use the same value as ClassNameStatic
        /// </summary>
        public virtual string ClassName => (string)GetType().GetField("ClassNameStatic")?.GetValue(null);
        /// <summary>
        /// Implementation of IHasHost, setting the host calls <see cref="AttachToProperty(NodePropertyBase)"/>
        /// </summary>
        object IHasHost.Host { get => propertyHost; set => AttachToProperty((NodePropertyBase)value); }
        ICacheHost ICacheMember.Host => propertyHost;

        private UniqueFrameRange frameRange = new UniqueFrameRange();
        /// <summary>
        /// range this particular node needs to be calculated
        /// </summary>
        public virtual IFrameRange IndividualCalculationRange => frameRange;
        /// <summary>
        /// range of the particular node and all property nodes
        /// </summary>
        public IFrameRange CalculationRange => GetFullRange();

        [CreateLoadInstance]
        static object CreateLoadInstance(NodePropertyBase parent, Type createThis)
        {
            //cree le truc, set nodeHost
            PropertyAffectingNode g =
                (PropertyAffectingNode)Activator
                .CreateInstance(createThis, new object[] { });
            g.AttachToProperty(parent);
            return g;
        }

        public PropertyAffectingNode()
        {
            UserGivenName = ClassName;
            PropertyChanged += PropertyAffectingNode_PropertyChanged;
            AfterConstructor();
        }

        private void PropertyAffectingNode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Enabled)))
            {
                propertyHost.InvalidateAllCachedFrames(this);
            }
        }

        /// <summary>
        /// Tells this node that it is linked to a property, this should not be used by itself.
        /// Use <see cref="NodePropertyBase.TryAttachNode(PropertyAffectingNode)"/> instead
        /// </summary>
        /// <param name="property">The property to attach the node to</param>
        public virtual void AttachToProperty(NodePropertyBase property)
        {
            this.propertyHost = property;
        }

        /// <summary>
        /// TODO make this private
        /// <para>
        /// to group node makers: DONT CALL THIS DIRECTLY, CALL <see cref="EvaluateFrameWrapper(int, DataFeed)"/>
        /// otherwise the <see cref="IndividualCalculationRange"/> will be fucked up
        /// </para>
        /// 
        /// <para>
        /// This is where you should do your calculation and put the result in a channel of the <paramref name="dataFeed"/>.
        /// The standard channel that is used by <see cref="propertyHost"/> is <see cref="Node.PROPERTY_OUT_CHANNEL"/> but 
        /// you can put any data in any other channel and other nodes will be able to retreive it.
        /// </para>
        /// <para>
        /// Some nodes use the interfaces in <see cref="Motio.NodeCommon"/> as markers to find data to process. 
        /// learn more from <a href="/examples/standardInterface">this example</a>
        /// </para>
        /// </summary>
        /// <param name="frame">The frame to calculate the value for</param>
        /// <param name="dataFeed">This is where you get and store data to pass to other nodes</param>
        public abstract void EvaluateFrame(int frame, DataFeed dataFeed);

        /// <summary>
        /// Invalidate the cache of all the properties of this node at <paramref name="frame"/>
        /// </summary>
        /// <param name="frame">Frame to invalidate the cache for</param>
        //public virtual void InvalidateAllProperties(int frame)
        //{
        //    for (int i = 0; i < Properties.Count; i++)
        //    {
        //        Properties[i].InvalidateFrame(frame);
        //    }
        //}

        private IFrameRange GetFullRange()
        {
            FrameRange range = new FrameRange();
            range.Add(IndividualCalculationRange);
            for (int i = 0; i < Properties.Count; i++)
            {
                range.Add(Properties[i].CalculationRange);
            }
            return range;
        }

        /// <summary>
        /// This is called once before every "batch process" (generally when the user presses play).
        /// This means a lot of frames are about to be calculated. You can use this function to calculate some
        /// values that don't change from frame to frame, avoiding to do it for each frame in <see cref="EvaluateFrame(int, DataFeed)"/>
        /// </summary>
        public virtual void Prepare()
        {
            //for (int i = 0; i < Properties.Count; i++)
            //{
            //    Properties[i].Prepare();
            //}
        }

        public override void Delete()
        {
            base.Delete();
            propertyHost.attachedNodes.Remove(this);
            propertyHost = null;
        }

        void ISetParent.SetParent(object parent)
        {
            propertyHost = (NodePropertyBase)parent;
        }

        void ICacheMember.Calculator(int frame, DataFeed dataFeed)
        {
            if (!Enabled)
                return;
            EvaluateFrame(frame, dataFeed);
            frameRange.FrameWasCalculated(frame);
        }

        void ICacheMember.CacheCleared(int frame)
        {
            frameRange.CacheWasCleared();
            //for (int i = 0; i < Properties.Count; i++)
            //{
            //    Properties[i].CacheCleared(frame);
            //}
        }
    }
}
