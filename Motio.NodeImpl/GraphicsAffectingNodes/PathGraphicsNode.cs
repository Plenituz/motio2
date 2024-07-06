using Motio.NodeCore;
using Motio.ObjectStoring;
using Motio.NodeImpl.NodeTools;
using Motio.NodeCommon.Utils;
using Motio.Pathing;
using Motio.Geometry;
using System.Collections.Generic;
using Motio.Meshing;
using Motio.NodeCore.Utils;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class PathGraphicsNode : GraphicsAffectingNode
    {
        public PathGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public PathGraphicsNode() { }

        //public override string ClassName => ClassNameStatic;
        public static string ClassNameStatic = "Path";

        [SaveMe]
        public Path path = new Path();

        protected override void SetupNode()
        {
            Tools.Add(new PathCreatorTool(this));
            path.AnyPointPropertyChanged += (_, __) => InvalidateGraphics();
            path.Points.CollectionChanged += (_, __) => InvalidateGraphics();
        }

        private void InvalidateGraphics()
        {
            this.FindGraphicsNode(out var gAff).InvalidateAllCachedFrames(gAff);
        }

        public override void SetupProperties()
        {
            
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            if(!dataFeed.TryGetChannelData(PATH_CHANNEL, out PathGroup group))
            {
                group = new PathGroup();
                dataFeed.SetChannelData(PATH_CHANNEL, group);
            }
            group.Add(path);
        }
    }
}
