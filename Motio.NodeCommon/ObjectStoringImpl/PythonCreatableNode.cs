using System;
using System.Reflection;
using IronPython.Runtime;
using IronPython.Runtime.Types;
using Motio.ObjectStoring;
using Motio.PythonRunning;

namespace Motio.NodeCommon.ObjectStoringImpl
{
    public class PythonCreatableNodeWrapper : ICreatableNode, ICreatable
    {
        public CreatablePythonNode pythonNode;

        public PythonCreatableNodeWrapper(CreatablePythonNode pythonNode)
        {
            this.pythonNode = pythonNode;
        }

        public object CreateInstance()
        {
            return CreateInstance(new object[] { });
        }

        public object CreateInstance(params object[] args)
        {
            return pythonNode.CreateIntance(args);
        }

        public object CreateInstanceWithCreateLoadInstance(object parent, string typeString)
        {
            if (Python.Engine.Operations.GetMemberNames(pythonNode.PythonType).Contains("CreateLoadInstance"))
            {
                dynamic createLoadInstance = Python.Engine.Operations.GetMember(pythonNode.PythonType, "CreateLoadInstance");
                return createLoadInstance(parent, pythonNode.PythonType);
            }
            return CallCreateLoadInstanceInBases(pythonNode.PythonType, parent, pythonNode.PythonType);
        }

        public string ClassNameStatic => Python.GetClassNameStatic(pythonNode.PythonType);

        public Type TypeCreated => pythonNode.PythonType;

        public bool HasCreateLoadInstance()
        {
            bool hasMethod = HasCreateLoadInstance(pythonNode.PythonType);
            if (hasMethod)
                return true;

            return HasCreateLoadInstanceInBases(pythonNode.PythonType);
        }

        private static object CallCreateLoadInstanceInBases(PythonType type, object parent, PythonType toCreate)
        {
            PythonTuple bases = PythonType.Get__bases__(null, type);
            object res = CallCreateLoadInstance(bases, parent, toCreate);
            if (res != null)
                return res;

            foreach (PythonType baseType in bases)
            {
                res = CallCreateLoadInstanceInBases(baseType, parent, toCreate);
                if (res != null)
                    return res;
            }
            return null;
        }

        private static object CallCreateLoadInstance(PythonTuple bases, object parent, PythonType toCreate)
        {
            foreach (PythonType baseType in bases)
            {
                object res = CallCreateLoadInstance(baseType, parent, toCreate);
                if (res != null)
                    return res;
            }
            return null;
        }

        private static object CallCreateLoadInstance(PythonType typeOn, object parent, PythonType toCreate)
        {
            if (Python.Engine.Operations.GetMemberNames(typeOn).Contains("CreateLoadInstance"))
            {
                dynamic createLoadInstance = Python.Engine.Operations.GetMember(typeOn, "CreateLoadInstance");
                return createLoadInstance(parent, toCreate);
            }
                
            MethodInfo method = TimelineSaver.SearchMethodWithAttr<CreateLoadInstanceAttribute>(typeOn,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
                return method.Invoke(null, new object[] { parent, toCreate });
            return null;
        }

        private static bool HasCreateLoadInstanceInBases(PythonType type)
        {
            PythonTuple bases = PythonType.Get__bases__(null, type);
            if (HasCreateLoadInstance(bases))
                return true;

            foreach(PythonType baseType in bases)
            {
                if (HasCreateLoadInstanceInBases(baseType))
                    return true;
            }
            return false;
        }

        private static bool HasCreateLoadInstance(PythonTuple bases)
        {
            foreach (PythonType baseType in bases)
            {
                if (HasCreateLoadInstance(baseType))
                    return true;
            }
            return false;
        }

        private static bool HasCreateLoadInstance(PythonType type)
        {
            if (Python.Engine.Operations.GetMemberNames(type).Contains("CreateLoadInstance"))
                return true;
            MethodInfo method = TimelineSaver.SearchMethodWithAttr<CreateLoadInstanceAttribute>(type,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return method != null;
        }
    }
}
