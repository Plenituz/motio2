using Motio.NodeCore;
using Motio.ObjectStoring;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class FileNodeProperty : StringNodeProperty
    {
        public enum ActionType
        {
            Open,
            Save
        }
        [SaveMe]
        public ActionType action = ActionType.Open;

        [SaveMe]
        public string title = "title";
        [SaveMe]
        public string filter = "All files|*";

        public FileNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
        }

        public FileNodeProperty(Node nodeHost) : base(nodeHost)
        {
        }
    }
}
