using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.PythonRunning;
using System;
using System.Collections.Generic;

namespace Motio.NodeImpl.PropertyAffectingNodes
{
    public class PythonPropertyNode : PropertyAffectingNode
    {
        //public override string ClassName => ClassNameStatic;
        public static string ClassNameStatic = "Expression (OLD)";
        public static IList<Type> AcceptedPropertyTypes = new Type[] { typeof(object) };

        public override IFrameRange IndividualCalculationRange => FrameRange.Infinite;

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
            if (property == Properties["prepare_code"])
            {
                PythonCompileErrorListener errors = Compile((string)value, out compiled_prepare);
                if (errors.HasErrors)
                {
                    return new Exception(errors.ToString());
                }
            }
            return null;
        }

        public override void SetupProperties()
        {
            var setupGroup = new GroupNodeProperty(this, "Open to see setup code", "Setup code");

            var value = new FloatNodeProperty(this, "Access this property via code using animated_value", "Animated Value");
            var prepare_code = new StringNodeProperty(this, "Code to run only once per batch calculation", "Setup code (runs once)");
            var code = new StringNodeProperty(this, "Code to run every frame", "Script code (runs each frame)");
            Properties.Add("animated_value", value, 1.0);
            Properties.Add("setup_group", setupGroup, null);
            setupGroup.Properties.Add("prepare_code", prepare_code, "");
            Properties.Add("code", code, "outValue = inputValue + animatedValue");
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
            scope.SetVariable("propertyHost", propertyHost);

            try
            {
                compiled_prepare.Execute(scope);
                prepare_code.ClearError(1);
            }
            catch (Exception e)
            {
                prepare_code.SetError(1, Python.FormatException(e));
            }
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            if (compiled_code != null)
            {
                scope.SetVariable("frame", frame);
                scope.SetVariable("dataFeed", dataFeed);
                scope.SetVariable("animatedValue", Properties.GetValue("animated_value", frame));
                scope.SetVariable("inputValue", dataFeed.GetChannelData(PROPERTY_OUT_CHANNEL));
                scope.RemoveVariable("outValue");

                try
                {
                    compiled_code.Execute(scope);
                    if (scope.ContainsVariable("outValue"))
                    {
                        dynamic result = scope.GetVariable("outValue");
                        dataFeed.SetChannelData(PROPERTY_OUT_CHANNEL, result);
                    }
                    Properties["code"].ClearError(2);
                }
                catch (Exception e)
                {
                    Properties["code"].SetError(2, Python.FormatException(e));
                }
            }
        }

        PythonCompileErrorListener Compile(string code, out CompiledCode compiled)
        {
            ScriptSource source = Python.Engine
                .CreateScriptSourceFromString(code, SourceCodeKind.File);

            PythonCompileErrorListener listener = new PythonCompileErrorListener();
            compiled = source.Compile(listener);
            return listener;
        }
    }
}
