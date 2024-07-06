using IronPython.Runtime.Types;
using Motio.Debuging;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.ObjectStoring;
using Motio.PythonRunning;
using System;

namespace Motio.NodeImpl.PropertyAffectingNodes
{
    public abstract class PyPropertyAffectingNodeBase : PropertyAffectingNode, IDynamicNode
    {
        public override string ClassName => ((dynamic)this).classNameStatic;

        [CreateLoadInstance]
        static object CreateLoadInstance(NodePropertyBase parent, PythonType createThis)
        {
            //cree le truc, set nodeHost
            PropertyAffectingNode g = (PropertyAffectingNode)Python.Engine.Operations
                .CreateInstance(createThis, new object[] { });
            g.AttachToProperty(parent);
            return g;
        }

        private string ErrorMsg(string func, Exception e)
        {
            return $"Error running {func} on {GetType()}:\n{Python.FormatException(e)}";
        }

        private void LogErr(string func, Exception e)
        {
            Logger.WriteLine(ErrorMsg(func, e));
        }

        protected sealed override void SetupNode()
        {
            base.SetupNode();
            try
            {
                setup_node();
            }
            catch (Exception e)
            {
                LogErr("setup_node", e);
            }
        }

        public sealed override void SetupProperties()
        {
            try
            {
                setup_properties();
            }
            catch (Exception e)
            {
                LogErr("setup_properties", e);
            }
        }

        public sealed override void Prepare()
        {
            base.Prepare();
            try
            {
                prepare();
            }
            catch (Exception e)
            {
                LogErr("prepare", e);
            }
        }

        public sealed override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            try
            {
                evaluate_frame(frame, dataFeed);
            }
            catch (Exception e)
            {
                LogErr("evaluate_frame", e);
            }
        }

        public sealed override Exception ValidatePropertyValue(NodePropertyBase property, object value)
        {
            try
            {
                return validate_property_value(property, value);
            }
            catch (Exception e)
            {
                LogErr("validate_property_value", e);
                return e;
            }
        }

        public sealed override void AttachToProperty(NodePropertyBase property)
        {
            base.AttachToProperty(property);
            try
            {
                attach_to_property(property);
            }
            catch (Exception e)
            {
                LogErr("attach_to_property", e);
            }
        }

        public sealed override void Delete()
        {
            base.Delete();
            try
            {
                delete();
            }
            catch (Exception e)
            {
                LogErr("delete", e);
            }
        }

        ///theses are just callbacks, you can't change the core comportement of the node by overriding theses
        protected virtual void setup_node() { }
        protected abstract void setup_properties();
        protected virtual void prepare() { }
        protected abstract void evaluate_frame(int frame, DataFeed dataFeed);
        protected virtual Exception validate_property_value(NodePropertyBase property, object value) => null;
        protected virtual void attach_to_property(NodePropertyBase property) { }
        protected virtual void delete() { }
    }
}
