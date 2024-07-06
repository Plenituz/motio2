using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls;
using Motio.NodeCore;
using Motio.NodeCore.Interfaces;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyPanelDisplays;

namespace Motio.UI.ViewModels
{
    public class ContextStarterGAffNodeViewModel : GraphicsAffectingNodeViewModel, IProxy<ContextStarterGAffNode>, IHasAttached<ContextAwareNodeViewModel>
    {
        ContextStarterGAffNode _original;
        ContextStarterGAffNode IProxy<ContextStarterGAffNode>.Original => _original;

        private ContextStarterPanelDisplay _propertyPanelDisplay;
        public override ContentControl PropertyPanelDisplay => _propertyPanelDisplay ?? (_propertyPanelDisplay = new ContextStarterPanelDisplay());

        IEnumerable IHasAttached.AttachedMembers => AttachedNodes;
        public IEnumerable<ContextAwareNodeViewModel> AttachedMembers
        {
            get
            {
                foreach (ContextAwareNodeViewModel node in AttachedNodes)
                    yield return node;
            }
        }
        public ListProxy<ContextAwareNode, ContextAwareNodeViewModel> AttachedNodes { get; private set; }


        public ContextStarterGAffNodeViewModel(ContextStarterGAffNode node) : base(node)
        {
            this._original = node;
            AttachedNodes = new ListProxy<ContextAwareNode, ContextAwareNodeViewModel>(
                _original.attachedNodes, _original.attachedNodes);
        }

        public void ReplaceMemberAt(int index, object member)
        {
            throw new System.NotImplementedException();
        }
    }
}
