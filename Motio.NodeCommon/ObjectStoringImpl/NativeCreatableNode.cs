using Motio.Debuging;
using System;
using System.Reflection;

namespace Motio.NodeCommon.ObjectStoringImpl
{
    public class NativeCreatableNode : ICreatableNode
    {
        private Type type;

        public NativeCreatableNode(Type type)
        {
            this.type = type;
        }

        public object CreateInstance(params object[] args)
        {
            return Activator.CreateInstance(type, args);
        }

        public string ClassNameStatic
        {
            get
            {
                FieldInfo info = type.GetField("ClassNameStatic");
                if (info == null)
                {
                    Logger.WriteLine(type.Name + "doesn't have a ClassNameStatic");
                    return type.Name;
                }

                if (info.FieldType != typeof(string))
                {
                    Logger.WriteLine(type.Name + "have a ClassNameStatic that doesn't return a string");
                    return type.Name;
                }

                return info.GetValue(null) as string;
            }
        }

        public Type TypeCreated => type;
    }
}
