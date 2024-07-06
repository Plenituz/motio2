using Motio.NodeCore;
using Motio.UI.Utils;

namespace Motio.UI.ViewModels
{
    public class ContextAwareNodeViewModel : NodeViewModel, IProxy<ContextAwareNode>
    {
        ContextAwareNode _original;
        ContextAwareNode IProxy<ContextAwareNode>.Original => _original;

        public ContextStarterGAffNodeViewModel _host;
        public override object Host { get => _host; set => _host = (ContextStarterGAffNodeViewModel)value; }


        public ContextAwareNodeViewModel(ContextAwareNode node) : base(node)
        {
            this._original = node;
            _host = ProxyStatic.GetProxyOf<ContextStarterGAffNodeViewModel>(_original.nodeHost);
            TrySetupViewModel();
        }
    }
}
