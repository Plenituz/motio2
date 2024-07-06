using Motio.Geometry;
using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodeTools;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class EditGraphicsNode : GraphicsAffectingNode
    {
        private static char[] splitChar = new char[] { ':' };
        public static string ClassNameStatic = "Edit";
        public EditGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public EditGraphicsNode() { }

        public ConcurrentDictionary<string, Vector2> edits = new ConcurrentDictionary<string, Vector2>();

        protected override void SetupNode()
        {
            base.SetupNode();
            PassiveTools.Add(new EditTool(this));
        }

        public override void SetupProperties()
        {

        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            if (!(dataFeed.TryGetChannelData(MESH_CHANNEL, out MeshGroup group)))
                return;
            //TODO the transform fucks up
            foreach(var pair in edits)
            {
                ApplyEdit(pair.Key, pair.Value, group);
            }

            for (int i = 0; i < group.Count; i++)
            {
                group[i].shader = "regular_color";
            }
        }

        private void FromStr(string id, MeshGroup group, out Mesh mesh, out int pIndex)
        {
            string[] split = id.Split(splitChar, 2);
            int groupIndex = int.Parse(split[0]);
            pIndex = int.Parse(split[1]);
            if (groupIndex >= 0 && group.Count > groupIndex)
            {
                mesh = group[groupIndex];
            }
            else
            {
                mesh = null;
            }
        }

        private void ApplyEdit(string id, Vector2 pos, MeshGroup group)
        {
            FromStr(id, group, out Mesh mesh, out int pIndex);
            if (mesh == null || pIndex > mesh.vertices.Count || pIndex < 0)
                return;
            Vertex vertex = mesh.vertices[pIndex];
            vertex.SetPos(pos);
            mesh.vertices[pIndex] = vertex;
        }
    }
}
