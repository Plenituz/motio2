using Motio.NodeCore;
using Motio.UI.Utils;

namespace Motio.UI.ViewModels
{
    public class TestNodeToolViewModel : NodeToolViewModel
    {

        public TestNodeToolViewModel(NodeTool tool) : base(tool)
        {
        }

        public override void OnSelect()
        {
            base.OnSelect();
            //_host.Original.Properties["size"].StaticValue = 2;
            _host.FindPropertyPanel().DeactivateActiveTool();
        }
    }
}
