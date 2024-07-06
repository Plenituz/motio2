using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class StringNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<StringNodeProperty>, INotifyPropertyChanged
    {
        StringNodeProperty _original;
        StringNodeProperty IProxy<StringNodeProperty>.Original => _original;

        public string StringValue { get => _original.StringValue; set => _original.StaticValue = value; }
        public virtual string SyntaxHighlighting { get; } = "Python";

        StringPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new StringPropertyDisplay());


        public StringNodePropertyViewModel(StringNodeProperty property) : base(property)
        {
            this._original = property;
        }

        public void ValidateValue(object value)
        {
            _original.ValidateValue(value);
        }
    }
}
