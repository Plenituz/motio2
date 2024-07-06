using System.Collections.Generic;

namespace Motio.NodeCommon.Utils
{
    public interface ICacheMember
    {
        ICacheHost Host { get; }
        IFrameRange CalculationRange { get; }
        /// <summary>
        /// calculate for the given frame
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="dataFeed"></param>
        void Calculator(int frame, DataFeed dataFeed);
        /// <summary>
        /// the cache for <paramref name="frame"/> has been cleared for the given frame. 
        /// if all frames have been cleared frame will be -1
        /// </summary>
        /// <param name="frame"></param>
        void CacheCleared(int frame);
    }

    public interface ICacheHost
    {
        int IndexOf(ICacheMember member);
        IEnumerable<ICacheMember> Members { get; }
    }
}
