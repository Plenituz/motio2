using Motio.NodeCommon.ObjectStoringImpl;
using Motio.NodeImpl.GraphicsAffectingNodes;
using Motio.NodeImpl.PropertyAffectingNodes;
using Motio.PythonRunning;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Motio.NodeCore;
using Motio.NodeImpl.GraphicsAffectingNodes.ContextNodes;

namespace Motio.NodeImpl
{
    public class NodeScanner
    {
        static List<ICreatableNode> nativeUserAccesibleGraphicsNodes = new List<ICreatableNode>()
        {
            new NativeCreatableNode(typeof(BasicSquareGraphicsNode)),
            new NativeCreatableNode(typeof(PathGraphicsNode)),
            new NativeCreatableNode(typeof(PythonGraphicsNode)),
            //new NativeCreatableNode(typeof(DebugGraphicsNode)),
            new NativeCreatableNode(typeof(VoxelizeGraphicsNode)),
            new NativeCreatableNode(typeof(BakeTransformGraphicsNode)),
            new NativeCreatableNode(typeof(ImageGraphicsNode)),
            //new NativeCreatableNode(typeof(PolygonGraphicsNode)),
            new NativeCreatableNode(typeof(SolidGraphicsNode)),
            new NativeCreatableNode(typeof(DuplicateGraphicsNode)),
            new NativeCreatableNode(typeof(EditGraphicsNode)),
            //new NativeCreatableNode(typeof(BooleanGraphicsNode)),
            new NativeCreatableNode(typeof(TextGraphicsNode)),
            
            //new NativeCreatableNode(typeof(CloneGraphicsNode)),
            //new NativeCreatableNode(typeof(CSGGraphicsNode)),
            //CreatableNode.Create(typeof(StrokeGraphicsNode2)),
            //CreatableNode.Create(typeof(TestQuentinGraphicsNode)),
            //CreatableNode.Create(typeof(ColorGraphicsNode)),
            new NativeCreatableNode(typeof(TransformGraphicsNode)),
            //CreatableNode.Create(typeof(ShapeDeformerGraphicsNode))

        };
        public static List<ICreatableNode> UserAccesibleGraphicsNodes = new List<ICreatableNode>();

        static List<ICreatableNode> nativeUserAccesiblePropertyNodes = new List<ICreatableNode>
        {
            //new NativeCreatableNode(typeof(PythonPropertyNode)),
            //CreatableNode.Create(typeof(MultiplyPropertyEveryFrame)),
            //CreatableNode.Create(typeof(ExpressionPropertyNode)),
            //CreatableNode.Create(typeof(CSharpPropertyNode)),
            //CreatableNode.Create(typeof(NormalizePropertyNode)),
            new NativeCreatableNode(typeof(TestNode)),
            new NativeCreatableNode(typeof(CopyPropertyNode))
        };
        public static List<ICreatableNode> UserAccesiblePropertyNodes = new List<ICreatableNode>();

        /// <summary>
        /// returns a string containing the error that occured
        /// </summary>
        /// <returns></returns>
        public static string ScanDynamicNodes()
        {
            StringBuilder errors = new StringBuilder();
            foreach (PythonException pyEx in Python.Instance.CompileAddons())
            {
                errors.Append("Error with python file " + pyEx.File + "\n" + pyEx.Message + "\n\n");
            }
            UpdateUserAccesibleList();
            return errors.ToString();
        }

        public static ICreatableNode GetByName(string classNameStatic)
        {
            var g = UserAccesibleGraphicsNodes.Where(n => n.ClassNameStatic.Equals(classNameStatic)).FirstOrDefault();
            if (g != null)
                return g;

            var p = UserAccesiblePropertyNodes.Where(n => n.ClassNameStatic.Equals(classNameStatic)).FirstOrDefault();
            return p;
        }

        static void UpdateUserAccesibleList()
        {
            bool KeepGraphicsAffectingNodes(CreatablePythonNode node)
            {
                return typeof(GraphicsAffectingNode).IsAssignableFrom(node.PythonType);
            }

            bool KeepPropertyAffectingNodes(CreatablePythonNode node)
            {
                return typeof(PropertyAffectingNode).IsAssignableFrom(node.PythonType);
            }

            IEnumerable<ICreatableNode> graphics = Python.CreatableNodes
                .Where(KeepGraphicsAffectingNodes)
                .Select(c => new PythonCreatableNodeWrapper(c));
            IEnumerable<ICreatableNode> properties = Python.CreatableNodes
                .Where(KeepPropertyAffectingNodes)
                .Select(c => new PythonCreatableNodeWrapper(c));

            UserAccesiblePropertyNodes.Clear();
            UserAccesiblePropertyNodes.AddRange(nativeUserAccesiblePropertyNodes);
            UserAccesiblePropertyNodes.AddRange(properties);

            UserAccesibleGraphicsNodes.Clear();
            UserAccesibleGraphicsNodes.AddRange(nativeUserAccesibleGraphicsNodes);
            UserAccesibleGraphicsNodes.AddRange(graphics);

            UserAccesibleGraphicsNodes.Sort((x, y) => x.ClassNameStatic.CompareTo(y.ClassNameStatic));
            UserAccesiblePropertyNodes.Sort((x, y) => x.ClassNameStatic.CompareTo(y.ClassNameStatic));
        }
    }
}
