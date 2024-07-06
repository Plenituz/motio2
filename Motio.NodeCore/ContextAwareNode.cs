using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.ObjectStoring;
using System;
using System.ComponentModel;

namespace Motio.NodeCore
{
    public abstract class ContextAwareNode : Node, IHasHost, ISetParent
    {
        public ContextStarterGAffNode nodeHost;

        public virtual string ClassName => (string)GetType().GetField("ClassNameStatic")?.GetValue(null);
        public object Host { get => nodeHost; set => nodeHost = (ContextStarterGAffNode)value; }

        public void SetParent(object parent) => Host = parent;

        [CreateLoadInstance]
        static object CreateLoadInstance(ContextStarterGAffNode parent, Type createThis)
        {
            //cree le truc, set nodeHost
            ContextAwareNode g =
                (ContextAwareNode)Activator
                .CreateInstance(createThis, new object[] { });
            g.nodeHost = parent;
            return g;
        }

        /// <summary>
        /// creates the node and attach it to it's ContextStarterGAffNode parent
        /// </summary>
        /// <param name="nodeHost"></param>
        public ContextAwareNode(ContextStarterGAffNode nodeHost) : this()
        {
            UserGivenName = ClassName;
            this.nodeHost = nodeHost;
            nodeHost.AttachNode(this);
        }

        /// <summary>
        /// THIS CONSTRUCTOR SHOULD ALWAYS BE IMPLEMENTED IN THE CHILD CLASSES
        /// for the create load instance to work
        /// </summary>
        protected ContextAwareNode()
        {
            //for the create load instance to work
            PropertyChanged += ContextAwareNode_PropertyChanged;
        }

        private void ContextAwareNode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Enabled)))
            {
                nodeHost.InvalidateAllCachedFrames(this);
            }
        }

        public virtual void Prepare()
        {
        }

        public override void Delete()
        {
            base.Delete();
            nodeHost.attachedNodes.Remove(this);
        }

        public abstract void EvaluateInContext(int frame, DataFeed dataFeed);
    }
}
