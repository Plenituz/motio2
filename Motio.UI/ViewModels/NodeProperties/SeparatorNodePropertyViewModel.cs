using System.Windows.Controls;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;

namespace Motio.UI.ViewModels
{
    public class SeparatorNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<SeparatorNodeProperty>
    {
        SeparatorPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new SeparatorPropertyDisplay());

        SeparatorNodeProperty _original;
        SeparatorNodeProperty IProxy<SeparatorNodeProperty>.Original => throw new System.NotImplementedException();

        public SeparatorNodePropertyViewModel(SeparatorNodeProperty property) : base(property)
        {
            this._original = property;
        }
    }
}
