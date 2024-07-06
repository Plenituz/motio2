using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class ButtonNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<ButtonNodeProperty>, INotifyPropertyChanged
    {
        ButtonNodeProperty _original;
        ButtonNodeProperty IProxy<ButtonNodeProperty>.Original => _original;

        ButtonPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new ButtonPropertyDisplay());

        public Action ClickFunc => _original.ClickFunc;

        public ButtonNodePropertyViewModel(ButtonNodeProperty original) : base(original)
        {
            _original = original;
        }
    }
}
