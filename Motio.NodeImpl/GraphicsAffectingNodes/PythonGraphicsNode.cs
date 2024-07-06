using System;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.NodeCore.Utils;
using Motio.PythonRunning;
using Motio.NodeCommon;
using Motio.NodeCommon.Utils;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public class PythonGraphicsNode : GraphicsAffectingNode
    {
        public PythonGraphicsNode(GraphicsNode nodeHost) : base(nodeHost) { }
        public PythonGraphicsNode() { }

        public static string ClassNameStatic = "Python";
        //public override string ClassName => ClassNameStatic;
        const string defaultPrepare = 
@"import Motio.Meshing as Meshing
import Motio.Geometry as Geo
from Motio.NodeCore import Node as Node
import clr
#load extensions method to get the timeline from the nodeHost
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)";
        const string defaultScript =
@"#Mesh builder is a helper class to create meshes
builder = Meshing.FastMeshBuilder(6, 6)
#this is how you access the value of other properties on this node
#here we get the value of the property which has the unique name 'value' for the 
#frame we are currently calculating
propValue = Properties.GetValue('animated_value', frame)
#we could also get the property named 'code' which contains this text!
#print Properties.GetValue('code', frame)

#FastMeshBuilder has some methods to help us create basic shapes
builder.AddTriangle (
    Geo.Vector2(0, 0),
    Geo.Vector2(propValue, 0),
    Geo.Vector2(propValue, propValue)
)
builder.AddTriangle(
	Geo.Vector2(propValue, propValue),
	Geo.Vector2(0, propValue),
	Geo.Vector2(0, 0)
)
#example of how you would get data from the timeline
#this will print the frame the cursor is at
#print nodeHost.GetTimeline().CurrentFrame

#giving the mesh to the datafeed 
#so the next nodes can process it
dataFeed.SetChannelData(Node.MESH_CHANNEL, Meshing.MeshGroup(builder.Mesh))";

        private CompiledCode compiled_prepare;
        private CompiledCode compiled_code;
        private ScriptScope scope;

        public override Exception ValidatePropertyValue(NodePropertyBase property, object value)
        {
            if (property == Properties["code"])
            {
                PythonCompileErrorListener errors = Compile((string)value, out compiled_code);
                if (errors.HasErrors)
                {
                    return new Exception(errors.ToString());
                }
            }
            if(property == Properties["prepare_code"])
            {
                PythonCompileErrorListener errors = Compile((string)value, out compiled_prepare);
                if (errors.HasErrors)
                {
                    return new Exception(errors.ToString());
                }
            }
            return null;
        }

        PythonCompileErrorListener Compile(string code, out CompiledCode compiled)
        {
            ScriptSource source = Python.Engine
                .CreateScriptSourceFromString(code, SourceCodeKind.File);

            PythonCompileErrorListener listener = new PythonCompileErrorListener();
            compiled = source.Compile(listener);
            return listener;
        }

        public override void SetupProperties()
        {
            var value = new FloatNodeProperty(this, "Access this property via code using Properties.GetValue('animated_value')", "Animated Value");
            var prepare_code = new StringNodeProperty(this, "Code to run only once per batch calculation", "Setup code (runs once)");
            var code = new StringNodeProperty(this, "Code to run every frame", "Script code (runs each frame)");
            Properties.Add("animated_value", value, 1.0);
            Properties.Add("prepare_code", prepare_code, defaultPrepare);
            Properties.Add("code", code, defaultScript);
        }

        public override void Prepare()
        {
            base.Prepare();
            Properties.WaitForProperty("code");
            var code = Properties["code"];
            var prepare_code = Properties["prepare_code"];
            //calling ValidateValue will call ValidatePropertyValue and set errors accordingly
            if (compiled_code == null)
                code.ValidateValue(code.StaticValue);
            if (compiled_prepare == null)
                prepare_code.ValidateValue(prepare_code.StaticValue);
            scope = Python.Engine.CreateScope();
            scope.SetVariable("self", this);
            scope.SetVariable("Properties", Properties);
            scope.SetVariable("nodeHost", nodeHost);

            try
            {
                compiled_prepare.Execute(scope);
                prepare_code.ClearError(1);
            }
            catch(Exception e)
            {
                prepare_code.SetError(1, Python.FormatException(e));
            }
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            if (compiled_code != null)
            {
                scope.SetVariable("frame", frame);
                scope.SetVariable("animatedValue", Properties.GetValue("animated_value", frame));
                scope.SetVariable("dataFeed", dataFeed);

                try
                {
                    compiled_code.Execute(scope);
                    Properties["code"].ClearError(2);
                }
                catch (Exception e)
                {
                    Properties["code"].SetError(2, Python.FormatException(e));
                }
            }
        }
    }
}
