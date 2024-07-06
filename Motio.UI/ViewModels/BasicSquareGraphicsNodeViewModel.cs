using Motio.NodeCore;
using Motio.UI.Utils;
using System;
using System.ComponentModel;

namespace Motio.UI.ViewModels
{
    class BasicSquareGraphicsNodeViewModel : GraphicsAffectingNodeViewModel, IProxy<GraphicsAffectingNode>, INotifyPropertyChanged
    {
        GraphicsAffectingNode _original;
        GraphicsAffectingNode IProxy<GraphicsAffectingNode>.Original => _original;

        public BasicSquareGraphicsNodeViewModel(GraphicsAffectingNode node) : base(node)
        {
            this._original = node;
            //Get("fdf").Visible = false;
        }

        NodePropertyBaseViewModel Get(string name)
        {
            return ProxyStatic.GetProxyOf<NodePropertyBaseViewModel>(_original.Properties[name]);
        }
    }
}
