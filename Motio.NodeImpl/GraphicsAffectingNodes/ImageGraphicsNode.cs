using Motio.Geometry;
using Motio.Graphics;
using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.NodeImpl.NodeTools;
using System;
using System.Drawing.Imaging;
using System.Linq;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class ImageGraphicsNode : GraphicsAffectingNode
    {
        public static string ClassNameStatic = "Image";
        public ImageGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public ImageGraphicsNode() { }

        protected override void SetupNode()
        {
            base.SetupNode();
            PassiveTools.Add(new MoveTool(this, "position"));
        }

        public override void SetupProperties()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            string filter = string.Format("All image files ({0})|{0}|All files|*",
                string.Join(";", codecs.Select(codec => codec.FilenameExtension).ToArray()));
            Properties.Add("path", 
                new FileNodeProperty(this, "Path to the image file", "Path")
                {
                    filter = filter,
                    title = "Open Image",
                    action = FileNodeProperty.ActionType.Open
                }, "");
            Properties.Add("position", 
                new VectorNodeProperty(this, "Position of the center of the image", "Position"), Vector2.Zero);
            Properties.Add("scale",
                new VectorNodeProperty(this, "Scale of the image", "Scale") { uniform = true }, new Vector2(50, 50));
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            string path = Properties.GetValue<string>("path", frame);
            Vector2 position = Properties.GetValue<Vector2>("position", frame);
            Vector2 scale = Properties.GetValue<Vector2>("scale", frame);
            //https://github.com/mono/opentk/blob/master/Source/Examples/OpenGL/1.x/Textures.cs
            //
            ImageStorage.ImageInfo info;
            try
            {
                info = ImageStorage.GetImageInfo(path);
            }
            catch (Exception ex)
            {
                Properties["path"].SetError(1, "Couldn't get image info for '" + path + "':\n" + ex.Message);
                return;
            }
            Properties["path"].ClearError(1);

            float widthOver2 = 0.5f;
            float heightOver2 = (info.pixelHeight / (float)info.pixelWidth) / 2f;

            Vertex[] vertices = new Vertex[]
            {
                new Vertex(new Vector2(-widthOver2, -heightOver2)*scale + position)
                {
                    uv = new Vector2(0, info.pixelHeight)
                },
                new Vertex(new Vector2(widthOver2, -heightOver2)*scale + position)
                {
                    uv = new Vector2(info.pixelWidth, info.pixelHeight)
                },
                new Vertex(new Vector2(widthOver2, heightOver2)*scale + position)
                {
                    uv = new Vector2(info.pixelWidth, 0)
                },
                new Vertex(new Vector2(-widthOver2, heightOver2)*scale + position)
                {
                    uv = new Vector2(0, 0)
                }
            };
            int[] triangles = new int[]
            {
                0, 1, 2,
                2, 3, 0
            };

            Mesh mesh;
            if (dataFeed.ChannelExists(MESH_CHANNEL))
            {
                mesh = dataFeed.GetChannelData<MeshGroup>(MESH_CHANNEL).New;
            }
            else
            {
                MeshGroup group = new MeshGroup();
                mesh = group.New;
                dataFeed.SetChannelData(MESH_CHANNEL, group);
            }

            mesh.triangles = triangles;
            mesh.vertices = vertices;
            //TODO shader texture here
        }
    }
}
