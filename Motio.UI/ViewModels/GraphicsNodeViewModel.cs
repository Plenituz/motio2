using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using Motio.UI.Utils;
using Motio.NodeCore;
using Motio.Meshing;
using Motio.NodeCore.Interfaces;
using Motio.NodeCommon.ObjectStoringImpl;
using Motio.Debuging;

namespace Motio.UI.ViewModels
{
    public class GraphicsNodeViewModel : NodeViewModel, IProxy<GraphicsNode>, 
        INotifyPropertyChanged, IHasAttached<GraphicsAffectingNodeViewModel>
    {
        GraphicsNode _original;
        GraphicsNode IProxy<GraphicsNode>.Original => _original;

        private AnimationTimelineViewModel _timelineHost;
        public AnimationTimelineViewModel TimelineHost => _timelineHost;
        public override object Host         { get => _timelineHost; set => _timelineHost = (AnimationTimelineViewModel)value; }
        public int StartTime                { get => _original.startTime; set => _original.startTime = value; }
        public int Duration                 { get => _original.duration; set => _original.duration = value; }
        public ListProxy<GraphicsAffectingNode, GraphicsAffectingNodeViewModel> AttachedNodes { get; private set; }
        public bool Visible                 { get => _original.Visible; set => _original.Visible = value; }

        IEnumerable<GraphicsAffectingNodeViewModel> IHasAttached<GraphicsAffectingNodeViewModel>.AttachedMembers
        {
            get
            {
                foreach(GraphicsAffectingNodeViewModel vm in AttachedNodes)
                    yield return vm;
            }
        }

        IEnumerable IHasAttached.AttachedMembers => AttachedNodes;

        public GraphicsNodeViewModel(GraphicsNode node) : base(node)
        {
            this._original = node;
            //we retreive the proxy of the timeline and cache it
            _timelineHost = ProxyStatic.CreateProxy<AnimationTimelineViewModel>(node.timelineHost);
            AttachedNodes = new ListProxy<GraphicsAffectingNode, GraphicsAffectingNodeViewModel>(
                _original.attachedNodes, _original.attachedNodes);
            TrySetupViewModel();
        }

        public bool UpdateModel()
        {
            return _original.UpdateModel();
        }

        public GraphicsAffectingNodeViewModel CreateNode(ICreatableNode creatableNode)
        {
            //the node attach itself to the graphicsnode in the constructor
            GraphicsAffectingNode nNode = creatableNode.CreateInstance(_original) as GraphicsAffectingNode;
            if (nNode == null)
            {
                Logger.WriteLine("couldn't create GraphicsAffectingNode node from " + creatableNode.ClassNameStatic);
                return null;
            }

            return ProxyStatic.GetProxyOf<GraphicsAffectingNodeViewModel>(nNode);
        }

        public void ReplaceMemberAt(int index, object member)
        {
            throw new System.NotImplementedException();
        }
    }
}
