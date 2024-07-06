using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class BoolNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<BoolNodeProperty>, INotifyPropertyChanged
    {
        BoolPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new BoolPropertyDisplay());

        BoolNodeProperty _original;
        BoolNodeProperty IProxy<BoolNodeProperty>.Original => _original;

        public bool Value { get => (bool)_original.StaticValue; set => _original.StaticValue = value; }

        public BoolNodePropertyViewModel(BoolNodeProperty property) : base(property)
        {
            this._original = property;
        }
    }
}
