using Motio.NodeCommon.StandardInterfaces;
using System;
using System.Collections.Generic;

namespace Motio.NodeCore.Utils
{
    public static class NodeExtensions
    {
        public static AnimationTimeline GetTimeline(this Node node)
        {
            if (node is IHasHost hasHost)
            {
                object topHost = hasHost.FindRoot();
                if (topHost is AnimationTimeline tl)
                    return tl;
                throw new Exception("top host is not an AnimationTimeline");
            }
            throw new Exception("in GetTimeline extension method: given node is not IHasHost");
        }

        /// <summary>
        /// find the graphics node and GraphicsAffectingNode above the given node
        /// the GraphicsAffectingNode might be null
        /// </summary>
        /// <param name="nodeHost"></param>
        /// <param name="gAffParent"></param>
        /// <returns></returns>
        public static GraphicsNode FindGraphicsNode(this Node nodeHost, out GraphicsAffectingNode gAffParent)
        {
            if (nodeHost is GraphicsNode gNode)
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
                    if (endOfChain is GraphicsNode grNode)
                    {
                        if (!(secondToLast is GraphicsAffectingNode))
                            throw new Exception("second to last is not a graphics affecting node");
                        gAffParent = (GraphicsAffectingNode)secondToLast;
                        return grNode;
                    }
                    secondToLast = endOfChain;
                    endOfChain = ((IHasHost)endOfChain).Host;
                }
            }
            throw new KeyNotFoundException("end of chain is not GraphicsNode " + nodeHost);
        }

        public static bool SearchUpInHierachy(this Node node, Node nodeToSearch)
        {
            if (node is IHasHost hasHost)
            {
                return SearchUp(hasHost, nodeToSearch);
            }
            return false;
        }

        public static bool SearchUp(IHasHost on, object toSearch)
        {
            object host = on.Host;
            if (host == toSearch)
                return true;
            if (host is IHasHost hostHasHost)
                return SearchUp(hostHasHost, toSearch);

            return false;
        }
    }
}
