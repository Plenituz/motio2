using System.ComponentModel;
using Motio.UI.Utils;
using Motio.NodeCore;
using System.Collections.ObjectModel;
using System.Windows;
using Motio.Configuration;
using System.Collections.Specialized;

namespace Motio.UI.ViewModels
{
    public class GraphicsAffectingNodeViewModel : NodeViewModel, IProxy<GraphicsAffectingNode>, INotifyPropertyChanged
    {
        GraphicsAffectingNode _original;
        GraphicsAffectingNode IProxy<GraphicsAffectingNode>.Original => _original;

        public override string UserGivenName => _original.UserGivenName;
        public string ClassName => _original.ClassName;

        public GraphicsNodeViewModel _host;
        public override object Host { get => _host; set => _host = (GraphicsNodeViewModel)value; }

        private int MaxCountPropertyPanel => Configs.GetValue<int>(Configs.NbNodeInPropertyPanel);
        public ObservableCollection<PropertyAffectingNodeViewModel> DisplayedSecondaryNodes { get; private set; } = new ObservableCollection<PropertyAffectingNodeViewModel>();

        public GraphicsAffectingNodeViewModel(GraphicsAffectingNode node) : base(node)
        {
            this._original = node;
            _host = ProxyStatic.CreateProxy<GraphicsNodeViewModel>(_original.nodeHost);
            DisplayedSecondaryNodes.CollectionChanged += DisplayedSecondaryNodes_CollectionChanged;
            TrySetupViewModel();
        }

        public void AddToDisplayedNodes(PropertyAffectingNodeViewModel node)
        {
            if (node == null)
            {
                MessageBox.Show("tried to bring a null node to the property panel");
                return;
            }
            int index = DisplayedSecondaryNodes.IndexOf(node);
            if (index != -1)
                DisplayedSecondaryNodes.Move(index, 0);
            else
                DisplayedSecondaryNodes.Insert(0, node);

            while (DisplayedSecondaryNodes.Count > MaxCountPropertyPanel)
            {
                DisplayedSecondaryNodes.RemoveAt(DisplayedSecondaryNodes.Count - 1);
            }
        }

        private void DisplayedSecondaryNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var propertyPanel = _host.TimelineHost.root.PropertyPanel;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            NodeViewModel node = (NodeViewModel)e.NewItems[i];
                            propertyPanel.ActivateTools(node);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            NodeViewModel node = (NodeViewModel)e.OldItems[i];
                            if(!propertyPanel.DisplayedLockedNodes.Contains(node))
                                propertyPanel.DeactivateTools(node);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            NodeViewModel node = (NodeViewModel)e.OldItems[i];
                            if(!propertyPanel.DisplayedLockedNodes.Contains(node))
                                propertyPanel.DeactivateTools(node);
                        }
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            NodeViewModel node = (NodeViewModel)e.NewItems[i];
                            propertyPanel.ActivateTools(node);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        propertyPanel.CallOnPassiveTools(t => t.OnHide());
                        propertyPanel.CallOnPassiveTools(t => t.OnDeselect());
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        //throw new NotImplementedException();
                    }
                    break;
            }
        }
    }
}
