using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class IntNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<IntNodeProperty>, INotifyPropertyChanged
    {
        IntPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new IntPropertyDisplay());

        IntNodeProperty _original;
        IntNodeProperty IProxy<IntNodeProperty>.Original => _original;

        public int IntValue => (int)_original.StaticValue;
        public float Sensitivity { get; set; } = 0.1f;
        public int? RangeFrom { get => _original.RangeFrom; set => _original.RangeFrom = value; }
        public int? RangeTo { get => _original.RangeTo; set => _original.RangeTo = value; }
        public bool HardRangeFrom { get => _original.HardRangeFrom; set => _original.HardRangeFrom = value; }
        public bool HardRangeTo { get => _original.HardRangeTo; set => _original.HardRangeTo = value; }

        public IntNodePropertyViewModel(IntNodeProperty property) : base(property)
        {
            this._original = property;
        }

        public Exception SetPropertyValueFromUserInput(string value)
        {
            return _original.SetPropertyValueFromUserInput(value);
        }
    }
}
