using Motio.NodeImpl.NodePropertyTypes;
using Motio.Pathing;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class CurveNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<CurveNodeProperty>, INotifyPropertyChanged
    {
        CurveNodeProperty _original;
        CurveNodeProperty IProxy<CurveNodeProperty>.Original => _original;
        CurvePropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new CurvePropertyDisplay());

        public Path Path => _original.Path;

        public CurveNodePropertyViewModel(CurveNodeProperty property) : base(property)
        {
            _original = property;
        }
    }
}
