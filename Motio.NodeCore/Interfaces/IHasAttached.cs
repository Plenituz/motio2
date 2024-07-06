using System.Collections;
using System.Collections.Generic;

namespace Motio.NodeCore.Interfaces
{
    public interface IHasAttached<T> : IHasAttached
    {
        new IEnumerable<T> AttachedMembers { get; }
    }

    public interface IHasAttached
    {
        IEnumerable AttachedMembers { get; }
        void ReplaceMemberAt(int index, object member);
    }
}
