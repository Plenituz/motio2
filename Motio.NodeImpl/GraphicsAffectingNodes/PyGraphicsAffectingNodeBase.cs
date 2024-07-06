using System;
using IronPython.Runtime.Types;
using Motio.Debuging;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.ObjectStoring;
using Motio.PythonRunning;

namespace Motio.NodeImpl.GraphicsAffectingNodes
{
    public abstract class PyGraphicsAffectingNodeBase : GraphicsAffectingNode, IDynamicNode
    {
        public override string ClassName
        {
            get
            {
                dynamic t = this;
                return t.classNameStatic;
            }
        }

        [CreateLoadInstance]
        static object CreateLoadInstance(GraphicsNode parent, PythonType createThis)
        {
            //cree le truc, set nodeHost
            GraphicsAffectingNode g = (GraphicsAffectingNode)Python.Engine.Operations
                .CreateInstance(createThis, new object[] { });
            g.nodeHost = parent;
            return g;
        }

        public PyGraphicsAffectingNodeBase(GraphicsNode node): base(node)
        {
        }
        protected PyGraphicsAffectingNodeBase()
        {
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

        public sealed override Exception ValidatePropertyValue(NodePropertyBase property, object value)
        {
            try
            {
                return validate_property_value(property, value);
            }catch(Exception e)
            {
                LogErr("validate_property_value", e);
                return e;
            }
        }

        protected abstract string get_class_name();
        protected virtual void setup_node() { }
        protected abstract void setup_properties();
        protected abstract void evaluate_frame(int frame, DataFeed dataFeed);
        protected virtual void prepare() { }
        protected virtual void delete() { }
        protected virtual Exception validate_property_value(NodePropertyBase property, object value) => null;
    }
}
