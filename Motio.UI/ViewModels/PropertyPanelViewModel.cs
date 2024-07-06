using Motio.Configuration;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Motio.UI.Utils;
using Motio.UI.Views;
using System.Collections.Specialized;
using Motio.NodeCore.Utils;

namespace Motio.UI.ViewModels
{
    public class PropertyPanelViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public MainControlViewModel mainViewModel;
        /// <summary>
        /// nodes that are currently displayed in the property panel
        /// </summary>
        private int MaxCountPropertyPanel => Configs.GetValue<int>(Configs.NbNodeInPropertyPanel);
        private NodeToolViewModel activeTool;
        public NodeToolViewModel ActiveTool => activeTool;

        //TODO make sure no node is displayed twice in the panel (locked and other)
        public ObservableCollection<NodeViewModel> DisplayedLockedNodes { get; private set; } = new ObservableCollection<NodeViewModel>();
        public ObservableCollection<GraphicsNodeViewModel> DisplayedGraphicsNodes { get; private set; } = new ObservableCollection<GraphicsNodeViewModel>();
        public GraphicsAffectingNodeViewModel DisplayedGraphicsAffectingNode { get; private set; }
        private GraphicsAffectingNodeViewModel previousDisplayedGraphicsAffectingNode = null;
        public GraphicsNodeDisplay TargetedAddButton;
        /// <summary>
        /// refresh the display of the graphics nodes
        /// this has to be done manually when new property affecting nodes get added to 
        /// properties that don't already have a node
        /// </summary>
        public event Action RefreshGraphics;

        public PropertyPanelViewModel(AnimationTimelineViewModel timeline, MainControlViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            PropertyChanged += PropertyPanelViewModel_PropertyChanged;

            foreach(NodeViewModel graphics in timeline.GraphicsNodes)
            {
                AddToPropertyPanel(graphics);
            }

            mainViewModel.AnimationTimeline.PropertyChanged += AnimationTimeline_PropertyChanged;
            mainViewModel.RenderView.ViewportClicked += RenderView_ViewportClicked;
            mainViewModel.RenderView.ViewportDragStart += RenderView_ViewportDragStart;
            mainViewModel.RenderView.ViewportDrag += RenderView_ViewportDrag;
            mainViewModel.RenderView.ViewportDragEnd += RenderView_ViewportDragEnd;
            DisplayedLockedNodes.CollectionChanged += DisplayedLockedNodes_CollectionChanged;
        }

        private void DisplayedLockedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EventHall.Trigger(this, "PropertyPanel.DisplayedLockedNodes", "CollectionChanged", e);
            void ElementAdded(NodeViewModel node)
            {
                if (DisplayedGraphicsAffectingNode == node)
                {
                    DisplayedGraphicsAffectingNode = null;
                    DisplayedGraphicsAffectingNode = (GraphicsAffectingNodeViewModel)node;
                }

                if (node is PropertyAffectingNodeViewModel pAff
                    && DisplayedGraphicsAffectingNode != null
                    && DisplayedGraphicsAffectingNode.DisplayedSecondaryNodes.Contains(pAff))
                    DisplayedGraphicsAffectingNode.DisplayedSecondaryNodes.Remove(pAff);
                ActivateTools(node);
            }

            void ElementRemoved(NodeViewModel node)
            {
                DeactivateTools(node);
                if (DisplayedGraphicsAffectingNode == node)
                {
                    DisplayedGraphicsAffectingNode = null;
                    DisplayedGraphicsAffectingNode = (GraphicsAffectingNodeViewModel)node;
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach(NodeViewModel node in e.NewItems)
                    {
                        ElementAdded(node);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (NodeViewModel node in e.OldItems)
                    {
                        ElementRemoved(node);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (NodeViewModel node in e.NewItems)
                    {
                        ElementAdded(node);
                    }
                    foreach (NodeViewModel node in e.OldItems)
                    {
                        ElementRemoved(node);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                   throw new NotImplementedException("this function makes no functionnal sense");
            }
        }

        private void AnimationTimeline_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName.Equals(nameof(AnimationTimelineViewModel.CurrentFrame)))
            {
                activeTool?.UpdateDisplay();
                CallOnPassiveTools(t => t.UpdateDisplay());
            }
        }

        public bool IsVisible(GraphicsAffectingNodeViewModel node)
        {
            return node == DisplayedGraphicsAffectingNode || DisplayedLockedNodes.Contains(node);
        }

        public void AddToTargetedNode()
        {
            TargetedAddButton?.PlusBtn_PreviewMouseUp(null, null);
        }

        /// <summary>
        /// the active tool is the one that receive the viewport click events
        /// </summary>
        /// <param name="tool"></param>
        public void SetActiveTool(NodeToolViewModel tool)
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool));
            if(activeTool != null && activeTool.Selected)
            {
                activeTool.OnDeselect();
            }
            activeTool = tool;
            activeTool.OnSelect();
            mainViewModel.RenderView.SwitchToToolClick();
        }

        public void DeactivateActiveTool()
        {
            if (activeTool == null)
                return;
            if (activeTool.Selected) 
                activeTool.OnDeselect();
            activeTool = null;
            mainViewModel.RenderView.SwitchToSelectionClick();
        }

        private void RenderView_ViewportDragStart(ClickLogic.DragEventArgs e)
        {
            activeTool?.OnDragEnterInViewport(e);
        }

        private void RenderView_ViewportDrag(ClickLogic.DragEventArgs e)
        {
            activeTool?.OnDragInViewport(e);
        }

        private void RenderView_ViewportDragEnd(ClickLogic.DragEventArgs e)
        {
            activeTool?.OnDragEndInViewport(e);
        }

        private void RenderView_ViewportClicked(System.Windows.Input.MouseEventArgs e)
        {
            Point screenPos = e.GetPosition(mainViewModel.RenderView.viewport);
            Point worldPos = mainViewModel.AnimationTimeline.Canv2World(screenPos);
            
            if(activeTool != null)
            {
                activeTool.OnClickInViewport(e, worldPos, screenPos);
                e.Handled = true;
            }
            CallOnPassiveTools(t => t.OnClickInViewport(e, worldPos, screenPos));
        }

        /// <summary>
        /// <para>
        /// sugar function that redirects automatically to the right function depending on the node type
        /// </para>
        /// 
        /// <see cref="GraphicsNodeViewModel"/> -> <see cref="AddGraphicsNode(GraphicsNodeViewModel)"/><br/>
        /// <see cref="GraphicsAffectingNodeViewModel"/> -> <see cref="SetDisplayedNode(GraphicsAffectingNodeViewModel)"/><br/>
        /// <see cref="PropertyAffectingNodeViewModel"/> -> <see cref="GraphicsAffectingNodeViewModel.AddToDisplayedNodes(PropertyAffectingNodeViewModel)"/>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public void AddToPropertyPanel(NodeViewModel node)
        {
            if(node is GraphicsNodeViewModel g)
            {
                AddGraphicsNode(g);
            }
            else if (node is GraphicsAffectingNodeViewModel gAff)
            {
                SetDisplayedNode(gAff);
            }
            else if (node is PropertyAffectingNodeViewModel pAff)
            {
                pAff.FindGraphicsNode(out var m_gAff);
                m_gAff.AddToDisplayedNodes(pAff);
                SetDisplayedNode(m_gAff);
                RefreshGraphics?.Invoke();
            }
        }

        public void AddGraphicsNode(GraphicsNodeViewModel node)
        {
            if (node == null)
            {
                MessageBox.Show("tried to bring a null node to the property panel");
                return;
            }
            int index = DisplayedGraphicsNodes.IndexOf(node);
            if (index != -1)
            {
                DisplayedGraphicsNodes.Move(index, 0);
            }
            else
            {
                DisplayedGraphicsNodes.Insert(0, node);
            }

            while (DisplayedGraphicsNodes.Count > MaxCountPropertyPanel)
            {
                DisplayedGraphicsNodes.RemoveAt(DisplayedGraphicsNodes.Count - 1);
            }
        }

        public void SetDisplayedNode(GraphicsAffectingNodeViewModel node)
        {
            DisplayedGraphicsAffectingNode = node;
            if (node == null)
                return;
            int indexLock = DisplayedLockedNodes.IndexOf(node);
            if(indexLock != -1)
            {
                DisplayedLockedNodes.RemoveAt(indexLock);
                DisplayedLockedNodes.Insert(indexLock, node);
            }
        }

        public void RemoveFromPropertyPanel(NodeViewModel node)
        {
            if (node == null)
            {
                MessageBox.Show("tried to remove null node from property panel");
                return;
            }
            if (node is GraphicsNodeViewModel g)
            {
                DisplayedGraphicsNodes.Remove(g);
            }
            else if (node is GraphicsAffectingNodeViewModel gAff)
            {
                int indexLock = DisplayedLockedNodes.IndexOf(node);
                if (indexLock != -1)
                    DisplayedLockedNodes.RemoveAt(indexLock);
                if (DisplayedGraphicsAffectingNode == gAff)
                    SetDisplayedNode(null);
            }
            else if (node is PropertyAffectingNodeViewModel pAff)
            {
                pAff.FindGraphicsNode(out var m_gAff);
                m_gAff.DisplayedSecondaryNodes.Remove(pAff);
            }
        }

        /// <summary>
        /// call OnShow on all tools of the node and on select on passive tools too
        /// </summary>
        /// <param name="node"></param>
        internal void ActivateTools(NodeViewModel node)
        {
            foreach (NodeToolViewModel nodeTool in node.Tools)
            {
                if(!nodeTool.Visible)
                    nodeTool.OnShow();
            }
            foreach (NodeToolViewModel nodeTool in node.PassiveTools)
            {
                if(!nodeTool.Visible)
                    nodeTool.OnShow();
                if(!nodeTool.Selected)
                    nodeTool.OnSelect();
            }
        }

        /// <summary>
        /// call onHide on all tools of the node and on deselect on passive tools
        /// </summary>
        /// <param name="node"></param>
        internal void DeactivateTools(NodeViewModel node)
        {
            foreach (NodeToolViewModel tool in node.Tools)
            {
                if(tool.Visible)
                    tool.OnHide();
            }
            foreach (NodeToolViewModel tool in node.PassiveTools)
            {
                if(tool.Visible)
                    tool.OnHide();
                if(tool.Selected)
                    tool.OnDeselect();
            }
        }

        /// <summary>
        /// call the given action on each passive tool
        /// </summary>
        /// <param name="action"></param>
        internal void CallOnPassiveTools(Action<NodeToolViewModel> action)
        {
            if (DisplayedGraphicsAffectingNode == null)
                return;
            foreach (NodeToolViewModel tool in DisplayedGraphicsAffectingNode.PassiveTools)
            {
                action(tool);
            }
            foreach(var pAff in DisplayedGraphicsAffectingNode.DisplayedSecondaryNodes)
            {
                foreach(NodeToolViewModel tool in pAff.PassiveTools)
                {
                    action(tool);
                }
            }
            foreach(NodeViewModel node in DisplayedLockedNodes)
            {
                foreach(NodeToolViewModel tool in node.PassiveTools)
                {
                    action(tool);
                }
            }
        }

        private void PropertyPanelViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EventHall.Trigger(this, "PropertyPanel", e.PropertyName);
            if (e.PropertyName.Equals(nameof(DisplayedGraphicsAffectingNode)))
            {
                bool previousLocked = DisplayedLockedNodes.Contains(previousDisplayedGraphicsAffectingNode);
                if(!previousLocked)
                    DeactivateActiveTool();
                if(previousDisplayedGraphicsAffectingNode != null && !previousLocked)
                    DeactivateTools(previousDisplayedGraphicsAffectingNode);
                if(DisplayedGraphicsAffectingNode != null)
                    ActivateTools(DisplayedGraphicsAffectingNode);
                previousDisplayedGraphicsAffectingNode = DisplayedGraphicsAffectingNode;
            }
        }
    }
}
