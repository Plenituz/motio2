using System;

namespace Motio.NodeCommon.ObjectStoringImpl
{
    public interface ICreatableNode
    {
        object CreateInstance(params object[] args);
        string ClassNameStatic { get; }
        Type TypeCreated { get; }
    }
}
