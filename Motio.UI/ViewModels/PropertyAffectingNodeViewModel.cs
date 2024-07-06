using Motio.NodeCommon;
using Motio.NodeCore;
using Motio.UI.Utils;
using System.ComponentModel;

namespace Motio.UI.ViewModels
{
    public class PropertyAffectingNodeViewModel : NodeViewModel, IProxy<PropertyAffectingNode>, INotifyPropertyChanged
    {
        public string ClassName => _original.ClassName;

        PropertyAffectingNode _original;
        PropertyAffectingNode IProxy<PropertyAffectingNode>.Original => _original;
        private NodePropertyBaseViewModel _host;
        public override object Host { get => _host; set => _host = (NodePropertyBaseViewModel)value; }

        public PropertyAffectingNodeViewModel(PropertyAffectingNode node) : base(node)
        {
            this._original = node;
            _host = ProxyStatic.GetProxyOf<NodePropertyBaseViewModel>(_original.propertyHost);
        }
    }
}
