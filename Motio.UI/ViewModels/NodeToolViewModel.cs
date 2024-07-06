using System.ComponentModel;
using Motio.UI.Utils;
using Motio.NodeCore;
using System.Windows.Input;
using System.Windows;
using Motio.UI.Views.Icons;
using Motio.NodeCommon.StandardInterfaces;
using Motio.ObjectStoring;
using System.Collections;

namespace Motio.UI.ViewModels
{
    public class NodeToolViewModel : Proxy<NodeTool>, INotifyPropertyChanged, IHasHost
    {
        public event PropertyChangedEventHandler PropertyChanged;

        NodeTool _original;
        public override NodeTool Original => _original;
        protected NodeViewModel _host;
        public object Host { get => _host; set => _host = (NodeViewModel)value; }

        private FrameworkElement _icon;
        public virtual FrameworkElement Icon => _icon ?? (_icon = new DefaultToolIcon());
        public bool Visible { get; set; } = false;
        public bool Selected { get; set; } = false;

        [CustomSaver]
        object CustomSaver()
        {
            return this;
        }

        public NodeToolViewModel(NodeTool tool) : base(tool)
        {
            this._original = tool;
            _host = ProxyStatic.GetProxyOf<NodeViewModel>(_original.nodeHost);
        }

        public override void Delete()
        {
            base.Delete();
            var propertyPanel = _host.FindPropertyPanel();
            if(propertyPanel.ActiveTool == this)
            {
                propertyPanel.DeactivateActiveTool();
            }
        }

        /// <summary>
        /// called when the frame number changes 
        /// </summary>
        public virtual void UpdateDisplay()
        {

        }

        /// <summary>
        /// this is called when the node appears in the property pannel
        /// you should then display your gizmos
        /// </summary>
        public virtual void OnShow()
        {
            Visible = true;
           // SubscribeToPropertiesEvent();
        }

        /// <summary>
        /// this is called wwhen the node is removed from the property pannel
        /// you should then hide your gizmos
        /// </summary>
        public virtual void OnHide()
        {
            Visible = false;
           // UnsubscribeFromPropertiesEvent();
        }

        /// <summary>
        /// this is called when the user clicks the button of the tool
        /// </summary>
        public virtual void OnSelect()
        {
            Selected = true;
        }

        /// <summary>
        /// this is called when the user deselect this tool
        /// </summary>
        public virtual void OnDeselect()
        {
            Selected = false;
        }

        /// <summary>
        /// <para>this is called when the user clicks on the viewport while this tool is activated</para>
        /// <para>note that passive tools do get this event but not the drag events</para>
        /// </summary>
        /// <param name="ev">le mouse event of the click</param>
        /// <param name="worldPos">world position of where the user clicked</param>
        /// <param name="canvasPos">screen position (on the gizmo canvas) of where the user clicked</param>
        public virtual void OnClickInViewport(MouseEventArgs ev, Point worldPos, Point canvasPos)
        {

        }

        /// <summary>
        /// <para>this is called when the user starts dragging on the viewport while this tool is activated</para>
        /// <para>note1: passive tools donc have access to this event</para>
        /// <para>note2: the positions in <paramref name="dragEvent"/> are in canvas space (alias screen space) 
        /// use AnimationTimelineViewModel.Canv2World to convert</para>
        /// </summary>
        /// <param name="dragEvent"></param>
        public virtual void OnDragEnterInViewport(ClickLogic.DragEventArgs dragEvent)
        {
            
        }

        /// <summary>
        /// <para>this is called when the user is dragging on the viewport while this tool is activated</para>
        /// <para>note1: passive tools donc have access to this event</para>
        /// <para>note2: the positions in <paramref name="dragEvent"/> are in canvas space (alias screen space) 
        /// use AnimationTimelineViewModel.Canv2World to convert</para>
        /// </summary>
        /// <param name="dragEvent"></param>
        public virtual void OnDragInViewport(ClickLogic.DragEventArgs dragEvent)
        {

        }

        /// <summary>
        /// <para>this is called when the user just stopped dragging on the viewport while this tool is activated</para>
        /// <para>note1: passive tools donc have access to this event</para>
        /// <para>note2: the positions in <paramref name="dragEvent"/> are in canvas space (alias screen space) 
        /// use AnimationTimelineViewModel.Canv2World to convert</para>
        /// </summary>
        /// <param name="dragEvent"></param>
        public virtual void OnDragEndInViewport(ClickLogic.DragEventArgs dragEvent)
        {

        }

        /*
         * 
        private void NodeTool_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateDisplay();
        }

        private void SubscribeToPropertiesEvent()
        {
            for (int i = 0; i < nodeHost.Properties.Count; i++)
            {
                nodeHost.Properties[i].PropertyChanged += NodeTool_PropertyChanged;
            }
        }

        private void UnsubscribeFromPropertiesEvent()
        {
            for (int i = 0; i < nodeHost.Properties.Count; i++)
            {
                nodeHost.Properties[i].PropertyChanged -= NodeTool_PropertyChanged;
            }
        }

        */
    }
}
