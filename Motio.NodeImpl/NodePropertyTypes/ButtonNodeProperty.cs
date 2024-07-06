using Motio.NodeCore;
using Motio.ObjectStoring;
using Motio.PythonRunning;
using System;
using System.Reflection;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class ButtonNodeProperty : NodePropertyBase
    {
        public override bool IsKeyframable => false;
        public override object StaticValue
        {
            get => 0;
            set { }
        }
        [SaveMe]
        public string funcName;

        public Action ClickFunc
        {
            get
            {
                MethodInfo method = nodeHost.GetType().GetMethod(funcName, BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static);
                if(method == null)
                {
                    if(Python.Engine.Operations.TryGetMember(nodeHost, funcName, out dynamic methodPy))
                        return () => methodPy();
                    else
                        return () => { };
                }
                return () => method.Invoke(nodeHost, new object[0]);
            }
        }

        public ButtonNodeProperty(Node nodeHost) : base(nodeHost)
        {

        }

        public ButtonNodeProperty(Node nodeHost, string description, string name, string funcName)
            : base(nodeHost, description, name)
        {
            this.funcName = funcName;
        }

        public override void CreateAnimationNode()
        {
        }
    }
}
