using Motio.GLWrapper;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.ObjectStoring;
using System;

namespace Motio.NodeCore
{
    public abstract class GraphicsAffectingNode : Node, IHasHost, ISetParent, ICacheMember
    {
        public GraphicsNode nodeHost;

        /// <summary>
        /// default name for the node, should say what it does 
        /// </summary>
        public virtual string ClassName => (string)GetType().GetField("ClassNameStatic")?.GetValue(null);
        public object Host { get => nodeHost; set => nodeHost = (GraphicsNode)value; }
        ICacheHost ICacheMember.Host => nodeHost;

        private UniqueFrameRange frameRange = new UniqueFrameRange();
        /// <summary>
        /// range this particular node needs to be calculated, by default this is 
        /// setup so the node is only calculated once (ie time independant)
        /// </summary>
        public virtual IFrameRange IndividualCalculationRange => frameRange;
        public virtual IFrameRange DownBranchCalculationRange => GetDownBranchRange();
        /// <summary>
        /// range of the particular node and all property nodes
        /// </summary>
        public IFrameRange CalculationRange => GetFullRange();

        [CreateLoadInstance]
        static object CreateLoadInstance(GraphicsNode parent, Type createThis)
        {
            //cree le truc, set nodeHost
            GraphicsAffectingNode g =
                (GraphicsAffectingNode)Activator
                .CreateInstance(createThis, new object[] { });
            g.nodeHost = parent;
            return g;
        }


        /// <summary>
        /// creates the node and attach it to it's graphics node
        /// </summary>
        /// <param name="nodeHost"></param>
        public GraphicsAffectingNode(GraphicsNode nodeHost) : this()
        {
            this.nodeHost = nodeHost;
            nodeHost.AttachNode(this);
            //this has to be after the this.nodeHost = ..
            UserGivenName = ClassName;
        }

        /// <summary>
        /// THIS CONSTRUCTOR SHOULD ALWAYS BE IMPLEMENTED IN THE CHILD CLASSES
        /// for the create load instance to work
        /// </summary>
        protected GraphicsAffectingNode()
        {
            //for the create load instance to work
            PropertyChanged += GraphicsAffectingNode_PropertyChanged;
            AfterConstructor();
        }

        private void GraphicsAffectingNode_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Enabled)))
            {
                nodeHost.InvalidateAllCachedFrames(this);
            }
        }

        /// <summary>
        /// range of the node, all it's children and all the previous nodes in the prante graphics node
        /// </summary>
        /// <returns></returns>
        public IFrameRange GetFullRange()
        {
            FrameRange range = new FrameRange();
            range.Add(DownBranchCalculationRange);
            int index = nodeHost.attachedNodes.IndexOf(this);
            if (index == 0)
                return range;

            for (int i = index - 1; i >= 0; i--)
            {
                range.Add(nodeHost.attachedNodes[i].DownBranchCalculationRange);
            }
            return range;
        }

        /// <summary>
        /// calculation range of this node and all it's children
        /// </summary>
        /// <returns></returns>
        public IFrameRange GetDownBranchRange()
        {
            //PERF TODO if this turns out to be too slow, we may want to cache the range and only calculate it 
            //when a "range invalidate" event/function is called
            //the thought is, this is always going to be better sans fully calculating all the frames
            FrameRange range = new FrameRange();
            range.Add(IndividualCalculationRange);
            for(int i = 0; i < Properties.Count; i++)
            {
                range.Add(Properties[i].CalculationRange);
            }
            return range;
        }

        /// <summary>
        /// called before every "batch process" meaning when a lot of frames are going
        /// to get calculated this is called. that way in EvaluateFrame you only put actual
        /// processing and not redondant checks
        /// it's called in the background thread and (most fo the time) parallel with the SetupProperties() method.
        /// This means you can't really use properties in here unless you make sure to wait for the properties to be added
        /// </summary>
        public virtual void Prepare()
        {
            //for(int i = 0; i < Properties.Count; i++)
            //{
            //    Properties[i].Prepare();
            //}
        }

        public override void Delete()
        {
            base.Delete();
            nodeHost.attachedNodes.Remove(this);
        }

        public abstract void EvaluateFrame(int frame, DataFeed dataFeed);

        public void SetParent(object parent) => Host = parent;

        void ICacheMember.Calculator(int frame, DataFeed dataFeed)
        {
            if (!Enabled)
                return;
            EvaluateFrame(frame, dataFeed);
            frameRange.FrameWasCalculated(frame);
        }

        /// <summary>
        /// called when the cache for this property and its parent has been cleared
        /// 
        /// </summary>
        /// <param name="frame"></param>
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
