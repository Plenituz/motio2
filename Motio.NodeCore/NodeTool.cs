using Motio.NodeCommon.StandardInterfaces;
using Motio.ObjectStoring;

namespace Motio.NodeCore
{
    /// <summary>
    /// <para>
    /// A NodeTool is attached to a Node and can be selected and used to interact with render view.
    /// </para>
    /// <para>
    /// You can create NodeTools for your nodes but most of the functionnality occurs in the ViewModel. 
    /// There is currently no way for you to provide a custom ViewModel for your tools, but it's an important feature that
    /// will be added before the alpha.
    /// </para>
    /// </summary>
    public abstract class NodeTool : IHasHost, ISetParent
    {
        object IHasHost.Host { get => nodeHost; set => nodeHost = (Node)value; }
        public Node nodeHost;

        public NodeTool(Node nodeHost)
        {
            this.nodeHost = nodeHost;
        }

        /// <summary>
        /// This is called when this node is about to be destroyed. To prevent memory leaks you should
        /// unsubscribe from all events here.
        /// </summary>
        public void Delete()
        {
            nodeHost.Tools.Remove(this);
            nodeHost.PassiveTools.Remove(this);
        }

        public void SetParent(object parent)
        {
            nodeHost = (Node)parent;
        }
    }
}
