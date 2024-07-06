using System.Windows.Controls;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Views.PropertyDisplays;

namespace Motio.UI.ViewModels
{
    public class FileNodePropertyViewModel : StringNodePropertyViewModel
    {
        private FileNodeProperty _original;

        FilePropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new FilePropertyDisplay());

        public FileNodeProperty.ActionType Action   { get => _original.action;  set => _original.action = value; }
        public string Title                         { get => _original.title;   set => _original.title = value; }
        public string Filter                        { get => _original.filter;  set => _original.filter = value; }

        public FileNodePropertyViewModel(FileNodeProperty property) : base(property)
        {
            this._original = property;
        }
    }
}
