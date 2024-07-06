using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class FloatNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<FloatNodeProperty>, INotifyPropertyChanged
    {
        FloatNodeProperty _original;
        FloatNodeProperty IProxy<FloatNodeProperty>.Original => _original;

        public float Sensitivity { get; set; } = 0.1f;
        public float FloatValue => _original.FloatValue;
        public float? RangeFrom { get => _original.RangeFrom; set => _original.RangeFrom = value; }
        public float? RangeTo { get => _original.RangeTo; set => _original.RangeTo = value; }
        public bool HardRangeFrom { get => _original.HardRangeFrom; set => _original.HardRangeFrom = value; }
        public bool HardRangeTo { get => _original.HardRangeTo; set => _original.HardRangeTo = value; }

        FloatPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new FloatPropertyDisplay());

        public FloatNodePropertyViewModel(FloatNodeProperty property) : base(property)
        {
            this._original = property;
        }

        public Exception SetPropertyValueFromUserInput(string value)
        {
            return _original.SetPropertyValueFromUserInput(value);
        }
    }
}
