using System.Windows.Controls;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Views.PropertyDisplays;

namespace Motio.UI.ViewModels
{
    public class DeletablePropertyWrapperViewModel : GroupNodePropertyViewModel
    {
        DeletablePropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new DeletablePropertyDisplay());

        public DeletablePropertyWrapperViewModel(DeletablePropertyWrapper property) : base(property)
        {
            
        }
    }
}
