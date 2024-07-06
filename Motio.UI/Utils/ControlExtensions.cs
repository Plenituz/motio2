using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCore;
using Motio.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Motio.UI.Utils
{
    public static class ControlExtensions
    {

        /// <summary>
        /// get the DataContext of the current window as a MainControlViewModel
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static MainControlViewModel FindMainViewModel(this FrameworkElement self)
        {
            var win = Window.GetWindow(self);

            return Window.GetWindow(self).DataContext as MainControlViewModel;
        }

        public static KeyframePanelViewModel FindKeyframePanel(this NodeViewModel self)
        {
            return FindMainViewModel(self)?.KeyframePanel;
        }

        public static PropertyPanelViewModel FindPropertyPanel(this NodeViewModel self)
        {
            return FindMainViewModel(self)?.PropertyPanel;
        }

        public static MainControlViewModel FindMainViewModel(this NodeViewModel self)
        {
            return ((AnimationTimelineViewModel)((IHasHost)self).FindRoot()).root;
        }

        public static KeyframePanelViewModel FindKeyframePanel(this NodePropertyBaseViewModel self)
        {
            return FindMainViewModel(self).KeyframePanel;
        }

        public static PropertyPanelViewModel FindPropertyPanel(this NodePropertyBaseViewModel self)
        {
            return FindMainViewModel(self).PropertyPanel;
        }

        public static MainControlViewModel FindMainViewModel(this NodePropertyBaseViewModel self)
        {
            return ((AnimationTimelineViewModel)((IHasHost)self).FindRoot()).root;
        }

        public static GraphicsNodeViewModel FindGraphicsNode(this NodeViewModel nodeHost, out GraphicsAffectingNodeViewModel gAffParent)
        {
            if (nodeHost is GraphicsNodeViewModel gNode)
            {
                gAffParent = null;
                return gNode;
            }

            object endOfChain = null;
            object secondToLast = nodeHost;
            if (nodeHost is IHasHost)
            {
                endOfChain = nodeHost;
                while (endOfChain is IHasHost)
                {
                    if (endOfChain is GraphicsNodeViewModel grNode)
                    {
                        if (!(secondToLast is GraphicsAffectingNodeViewModel))
                            throw new Exception("second to last is not a graphics affecting node");
                        gAffParent = (GraphicsAffectingNodeViewModel)secondToLast;
                        return grNode;
                    }
                    secondToLast = endOfChain;
                    endOfChain = ((IHasHost)endOfChain).Host;
                }
            }
            throw new KeyNotFoundException("end of chain is not GraphicsNode " + nodeHost);
        }

        /// <summary>
        /// search in all properties and property groups for the given property and return it
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        //public static bool FindInProperties(ListProxy<NodePropertyBase, NodePropertyBaseViewModel> properties, string name, out NodePropertyBaseViewModel result)
        //{
        //    var copy = properties.ToArray();
        //    foreach (NodePropertyBaseViewModel prop in copy)
        //    {
        //        string hostUniqueName = ((PropertyGroup)properties.originalList).GetUniqueName(prop.Original);
        //        if (hostUniqueName == name)
        //        {
        //            result = prop;
        //            return true;
        //        }
        //        if (prop is GroupNodePropertyViewModel group)
        //        {
        //            if (FindInProperties(group.Properties, name, out result))
        //                return true;
        //        }
        //    }
        //    result = null;
        //    return false;
        //}

        /// <summary>
        /// search in all properties and property groups for the given property and delete it
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="toDelete"></param>
        /// <returns></returns>
        public static bool DeleteInProperties(ListProxy<NodePropertyBase, NodePropertyBaseViewModel> properties, NodePropertyBaseViewModel toDelete)
        {
            var copy = properties.ToArray();
            foreach (NodePropertyBaseViewModel prop in copy)
            {
                if (prop == toDelete)
                {
                    properties.Remove(prop);
                    prop.Original.Delete();
                    return true;
                }
                if (prop is GroupNodePropertyViewModel group)
                {
                    if (DeleteInProperties(group.Properties, toDelete))
                        return true;
                }
            }
            return false;
        }
    }
}
