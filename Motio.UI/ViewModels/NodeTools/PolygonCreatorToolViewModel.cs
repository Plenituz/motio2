using System.Windows;
using System.Windows.Input;
using Motio.NodeCore;
using Motio.NodeCore.Utils;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.NodeImpl.NodeTools;
using Motio.UI.Utils;

namespace Motio.UI.ViewModels
{
    public class PolygonCreatorToolViewModel : NodeToolViewModel
    {
        PolygonCreatorTool tool;
        GroupNodeProperty PointGroup => tool.nodeHost.Properties.Get<GroupNodeProperty>("point_group");

        public PolygonCreatorToolViewModel(PolygonCreatorTool tool) : base(tool)
        {
            this.tool = tool;
        }

        public override void OnClickInViewport(MouseEventArgs ev, Point worldPos, Point canvasPos)
        {
            base.OnClickInViewport(ev, worldPos, canvasPos);
            GroupNodeProperty pointGroup = PointGroup;
            VectorNodeProperty newPoint = new VectorNodeProperty(tool.nodeHost,
                pointGroup.Properties.Count + "th point of the polygon", "Point " + pointGroup.Properties.Count);

            DeletablePropertyWrapper wrapper = new DeletablePropertyWrapper(tool.nodeHost);



            string name;
            //make sure the name is unique
            do
            {
                name = NodeUUIDGroup.RandomString();
            }
            while (pointGroup.Properties.TryGetProperty(name, out var tmp));
            pointGroup.Properties.AddManually(name, wrapper);
            wrapper.Properties.Add(name + "_point", newPoint, new Geometry.Vector2((float)worldPos.X, (float)worldPos.Y));

            //bool found = ControlExtensions.FindInProperties(_host.Properties, "point_" + (pointGroup.Properties.Count-1), out NodePropertyBaseViewModel prop);


            MoveTool moveTool = new MoveTool(tool.nodeHost, name + "_point");
            tool.nodeHost.PassiveTools.Add(moveTool);
            ProxyStatic.GetProxyOf<NodeToolViewModel>(moveTool).OnShow();
        }
    }
}
