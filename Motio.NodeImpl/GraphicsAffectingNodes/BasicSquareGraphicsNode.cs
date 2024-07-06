using Motio.Geometry;
using Motio.Graphics;
using Motio.Meshing;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.NodeImpl.NodeTools;
using System;
using System.Collections.Generic;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class BasicSquareGraphicsNode : GraphicsAffectingNode
    {
        public BasicSquareGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public BasicSquareGraphicsNode() { }

        public static string ClassNameStatic = "TestNode";

        protected override void SetupNode()
        {
            base.SetupNode();
            PassiveTools.Add(new MoveTool(this, "pos"));
            PassiveTools.Add(new MoveTool(this, "pos2"));
        }

        public override void SetupProperties()
        {
            Properties.Add("jhj", new FloatNodeProperty(this, "", "df"), 0);
            Properties.Add("pos", new VectorNodeProperty(this, "", "pos"), new Vector2(15, 15));
            Properties.Add("pos2", new VectorNodeProperty(this, "", "pos2"), new Vector2(1, 15));
        }
        
        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            Properties.WaitForProperty("pos2");
            Vector2 pos = Properties.GetValue<Vector2>("pos", frame);
            MotioShape motioPoly = new MotioShape
            {
                vertices = new Vertex[]
                {
                    new Vertex(new Vector2(0, 0) + pos, new Vector4(1, 1, 0, 0f)),
                    new Vertex(new Vector2(10, 0) + pos, new Vector4(1, 0, 0, 0.5f)),
                    new Vertex(new Vector2(10, 10) + pos, new Vector4(1, 0, 0, 0.5f)),
                    new Vertex(new Vector2(0, 10) + pos, new Vector4(1, 0, 0, 0.5f))
                },
                holes = new MotioShape[]{
                    new MotioShape()
                    {
                        vertices = new Vertex[]
                        {
                            new Vertex(new Vector2(0 + 2, 0 + 2) + pos),
                            new Vertex(new Vector2(10 - 2, 0 + 2) + pos),
                            new Vertex(new Vector2(10 - 2, 10 - 2) + pos),
                            new Vertex(new Vector2(0 + 2, 10 - 2) + pos)
                        },
                        holes = new MotioShape[]
                        {
                            new MotioShape()
                            {
                                vertices = new Vertex[]
                                {
                                    new Vertex(new Vector2(0 + 4, 0 + 4) + pos),
                                    new Vertex(new Vector2(10 - 4, 0 + 4) + pos),
                                    new Vertex(new Vector2(10 - 4, 10 - 4) + pos),
                                    new Vertex(new Vector2(0 + 4, 10 - 4) + pos)
                                },
                                holes = new MotioShape[]
                                {
                                    new MotioShape()
                                    {
                                        vertices = new Vertex[]
                                        {
                                            new Vertex(new Vector2(0 + 5.2f, 0 + 5.2f) + pos),
                                            new Vertex(new Vector2(10 - 5.2f, 0 + 5.2f) + pos),
                                            new Vertex(new Vector2(10 - 5.2f, 10 - 5.2f) + pos),
                                            new Vertex(new Vector2(0 + 5.2f, 10 - 5.2f) + pos)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            

            MotioShapeGroup group;
            if(!dataFeed.TryGetChannelData(POLYGON_CHANNEL, out group))
            {
                group = new MotioShapeGroup();
            }
            group.Add(motioPoly);
            dataFeed.SetChannelData(POLYGON_CHANNEL, group);


            StrokeTracerShape tracer = new StrokeTracerShape();
            tracer.Stroke(new[]
            {
                new Vector2(),
                new Vector2(5, 5),
                Properties.GetValue<Vector2>("pos", frame),
            }, 
            new[]
            {
                1f, 1.2f, 1.4f, 1.6f, 1.8f
            }, StrokeTracerShape.EndCap.Square, false);

            //motioPoly = tracer.MotioShape;
            bool inside = motioPoly.HitTest(Properties.GetValue<Vector2>("pos2", frame));
            if (inside)
            {
                for (int i = 0; i < motioPoly.vertices.Count; i++)
                {
                    Vertex v = motioPoly.vertices[i];
                    v.color = Color.Green.ToVector4();
                    motioPoly.vertices[i] = v;
                }
            }
            else
            {
                for (int i = 0; i < motioPoly.vertices.Count; i++)
                {
                    Vertex v = motioPoly.vertices[i];
                    v.color = new Vector4(1, 0, 0, 0.5f);
                    motioPoly.vertices[i] = v;
                }
            }

            MotioShape shape = motioPoly;
            //shape.CalculateNormals();


            //MotioShape shape = new MotioShape()
            //{
            //    vertices = new List<Vertex>()
            //};

            //foreach(Vector2 v in tracer.points)
            //{
            //    shape.vertices.Add(new Vertex(v));
            //}
            dataFeed.SetChannelData(POLYGON_CHANNEL, new MotioShapeGroup(shape));
        }
        
    }
}
