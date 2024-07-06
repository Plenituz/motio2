using Motio.Graphics;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System.ComponentModel;
using System.Windows.Controls;
using Motio.UICommon;

namespace Motio.UI.ViewModels
{
    public class ColorNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<ColorNodeProperty>, INotifyPropertyChanged
    {
        ColorNodeProperty _original;
        ColorNodeProperty IProxy<ColorNodeProperty>.Original => _original;

        public System.Windows.Media.Color Color
        {
            get => _original.ColorValue.ToMediaColor();
            set => _original.StaticValue = value.ToMotioColor();
        }
        ColorPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new ColorPropertyDisplay());

        public ColorNodePropertyViewModel(ColorNodeProperty property) : base(property)
        {
            this._original = property;
        }
    }
}
