using Motio.NodeCommon;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCore;
using Motio.NodeCore.Utils;
using Motio.ObjectStoring;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyPanelDisplays;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public abstract class NodeViewModel : Proxy<Node>, INotifyPropertyChanged, IHasHost
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool setup = false;

        public virtual string UserGivenName { get => Original.UserGivenName; set => Original.UserGivenName = value; }
        public virtual bool Enabled { get => Original.Enabled; set => Original.Enabled = value; }
        public NodeUUID UUID { get => _original.UUID; set => _original.UUID = value; }

        public virtual ListProxy<NodePropertyBase, NodePropertyBaseViewModel> Properties { get; private set; }
        public virtual ListProxy<NodeTool, NodeToolViewModel> Tools { get; private set; }
        public virtual ListProxy<NodeTool, NodeToolViewModel> PassiveTools { get; private set; }
        public virtual ObservableCollection<NodePropertyBaseViewModel> PropertiesInTimeline { get; private set; } 
            = new ObservableCollection<NodePropertyBaseViewModel>();
        public PropertyPanelViewModel PropertyPanel => this.FindPropertyPanel();
        public bool IsLockedInPropertyPanel => PropertyPanel.DisplayedLockedNodes.Contains(this);

        Node _original;
        public override Node Original => _original;
        public abstract object Host { get; set; }
        private DefaultPropertyPanelDisplay _propertyPanelDisplay;
        public virtual ContentControl PropertyPanelDisplay => _propertyPanelDisplay ?? (_propertyPanelDisplay = new DefaultPropertyPanelDisplay());

        [CustomSaver]
        object CustomSaver()
        {
            object data = TimelineSaver.SaveObjectToJson(this, true);
            if (data == this)//if the object has no [SaveMe] the same object is returned
            {
                return this;
            }
            else
            {
                IDictionary dict = (IDictionary)data;
                dict.Add("uuid", UUID.ToString());
                return dict;
            }
        }

        public NodeViewModel(Node node) : base(node)
        {
            this._original = node;
            Tools = new ListProxy<NodeTool, NodeToolViewModel>(_original.Tools, _original.Tools);
            PassiveTools = new ListProxy<NodeTool, NodeToolViewModel>(_original.PassiveTools, _original.PassiveTools);
            Properties = new ListProxy<NodePropertyBase, NodePropertyBaseViewModel>(
                _original.Properties, _original.Properties);

            PropertiesInTimeline.CollectionChanged += PropertiesInTimeline_CollectionChanged;
        }

        /// <summary>
        /// setup the viewmodel only if this viewmodel can find it's mainviewmodel parent.
        /// 
        /// This HAS to be called in the constructor of derived classes after setting the host
        /// </summary>
        public void TrySetupViewModel()
        {
            var vm = this.FindMainViewModel();
            if (vm != null && !setup)
            {
                setup = true;
                SetupViewModel();
            }
        }

        protected virtual void SetupViewModel()
        {

        }

        private void PropertiesInTimeline_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            KeyframePanelViewModel keyframePanel = this.FindKeyframePanel();

            if (PropertiesInTimeline.Count == 0 && keyframePanel.NodeInTimeline.Contains(this))
            {
                //this node should no longer be in the timeline since no properties are displayed anymore
                keyframePanel.RemoveFromTimeline(this);
            }

        }

        public override void Delete()
        {
            base.Delete();
            PropertiesInTimeline.CollectionChanged -= PropertiesInTimeline_CollectionChanged;
            PropertiesInTimeline.Clear();
            MainControlViewModel mainViewModel = this.FindMainViewModel();
            mainViewModel.KeyframePanel.RemoveFromTimeline(this);
            mainViewModel.PropertyPanel.RemoveFromPropertyPanel(this);

            //foreach(NodePropertyBaseViewModel prop in Properties)
            //{
            //    prop.Original.Delete();
            //}
            //foreach (NodeToolViewModel tool in Tools)
            //{
            //    tool.Original.Delete();
            //}
            //foreach(NodeToolViewModel passiveTool in PassiveTools)
            //{
            //    passiveTool.Original.Delete();
            //}
            //_original.Delete();
        }
    }
}
