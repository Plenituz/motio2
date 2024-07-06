using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Motio.NodeCommon.Utils;
using Motio.NodeCore.Interfaces;
using Motio.ObjectStoring;

namespace Motio.NodeCore
{
    public abstract class ContextStarterGAffNode : GraphicsAffectingNode, IHasAttached<ContextAwareNode>
    {
        public ContextStarterGAffNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public ContextStarterGAffNode() { }

        [SaveMe]
        public ObservableCollection<ContextAwareNode> attachedNodes = new ObservableCollection<ContextAwareNode>();
        IEnumerable<ContextAwareNode> IHasAttached<ContextAwareNode>.AttachedMembers => attachedNodes;
        IEnumerable IHasAttached.AttachedMembers => attachedNodes;

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            for(int i = 0; i < attachedNodes.Count; i++)
            {
                attachedNodes[i].EvaluateInContext(frame, dataFeed);
            }
        }

        public override void Delete()
        {
            base.Delete();
            while (attachedNodes.Count != 0)
                attachedNodes[0].Delete();
            nodeHost.attachedNodes.Remove(this);
        }

        public override void Prepare()
        {
            base.Prepare();
            for(int i = 0; i < attachedNodes.Count; i++)
            {
                attachedNodes[i].Prepare();
            }
        }

        internal void AttachNode(ContextAwareNode contextAwareNode)
        {
            attachedNodes.Add(contextAwareNode);
        }

        public void ReplaceMemberAt(int index, object member)
        {
            attachedNodes[index] = (ContextAwareNode)member;
        }

        internal void InvalidateAllCachedFrames(ContextAwareNode contextAwareNode)
        {
            nodeHost.InvalidateAllCachedFrames(this);
        }
    }
}
