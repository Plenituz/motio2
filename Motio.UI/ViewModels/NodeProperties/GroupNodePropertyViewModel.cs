using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class GroupNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<GroupNodeProperty>, INotifyPropertyChanged
    {
        GroupPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new GroupPropertyDisplay());

        private GroupNodeProperty _original;
        GroupNodeProperty IProxy<GroupNodeProperty>.Original => _original;

        public virtual ListProxy<NodePropertyBase, NodePropertyBaseViewModel> Properties { get; private set; }

        public GroupNodePropertyViewModel(GroupNodeProperty property) : base(property)
        {
            this._original = property;
            Properties = new ListProxy<NodePropertyBase, NodePropertyBaseViewModel>(
                _original.Properties, _original.Properties);
        }
        
    }
}
