using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class OrderNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<OrderNodeProperty>, INotifyPropertyChanged
    {
        OrderPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new OrderPropertyDisplay());

        OrderNodeProperty _original;
        OrderNodeProperty IProxy<OrderNodeProperty>.Original => _original;

        public ObservableCollection<string> Items => _original.items;

        public OrderNodePropertyViewModel(OrderNodeProperty property) : base(property)
        {
            this._original = property;
        }
    }
}
