using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class DropdownNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<DropdownNodeProperty>, INotifyPropertyChanged
    {
        DropdownNodeProperty _original;
        DropdownNodeProperty IProxy<DropdownNodeProperty>.Original => _original;

        public ObservableCollection<string> Choices => _original.choices;
        public string Selected { get => (string)_original.StaticValue; set => _original.StaticValue = value; }

        DropdownPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new DropdownPropertyDisplay());

        public DropdownNodePropertyViewModel(DropdownNodeProperty property) : base(property)
        {
            _original = property;
        }
    }
}
