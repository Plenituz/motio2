using System;
using System.Collections.Concurrent;

namespace Motio.NodeCommon.Utils
{
    public class FrameCache
    {
        private ConcurrentDictionary<int, DataFeed> cache = new ConcurrentDictionary<int, DataFeed>();
        internal ICacheMember cacheMember;

        public FrameCache(ICacheMember cacheMember)
        {
            this.cacheMember = cacheMember;
        }

        public bool ShouldCalculateFrame(int frame)
        {
            bool containsFrame = cacheMember.CalculationRange.ContainsFrame(frame);
            bool cacheContains = cache.ContainsKey(frame);
            int closestFrameInRange = cacheMember.CalculationRange.ClosestPreviousInRange(frame);
            bool cacheContainsClosest = cache.ContainsKey(closestFrameInRange);

            return (containsFrame && !cacheContains) || !cacheContainsClosest;
        }

        public DataFeed GetCache(int frame)
        {
            //if the frame is outside the range, will get change it be in the range,
            //otherwise don't do anything
            int prev = frame;
            frame = cacheMember.CalculationRange.ClosestPreviousInRange(frame);
            if (frame < 0)
                frame = prev;
            cache.TryGetValue(frame, out DataFeed feed);
            return feed;
        }

        /// <summary>
        /// the input feed if going to be modified, make sur it's a clone of the output of the 
        /// previous node and not the actual instance
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="inputFeed"></param>
        /// <returns></returns>
        public void CalculateIfNeeded(int frame, DataFeed inputFeed)
        {
            if (ShouldCalculateFrame(frame))
                ForceCalculate(frame, inputFeed);
        }

        public void ForceCalculate(int frame, DataFeed inputFeed)
        {
            cacheMember.Calculator(frame, inputFeed);
            cache[frame] = inputFeed;
        }

        public void ClearCache(int frame)
        {
            bool removed = cache.TryRemove(frame, out DataFeed feed);
            cacheMember.CacheCleared(frame);
            if (!removed)
                throw new Exception("couldn't remove cached frame");
        }

        public void ClearAllCache()
        {
            cacheMember.CacheCleared(-1);
            cache.Clear();
        }
    }
}
