#if !DEBUG
using Motio.Debuging;
#endif
using System;
using System.Collections.Generic;

namespace Motio.NodeCommon.Utils
{
    /// <summary>
    /// <see cref="NodeChainCache"/> but not multithreaded
    /// </summary>
    public class SyncNodeChainCache : IChainCache
    {
        public delegate void CacheClearedAction(int frame, int frameNodeIndex);

        private readonly List<FrameCache> chainCache = new List<FrameCache>();
        private readonly ConfiguredDataFeedProvider dataFeedProvider;
        public event Action Prepare;
        /// <summary>
        /// the frame argument can be -1 significating to clear all the frames
        /// </summary>
        public event CacheClearedAction AnyCacheCleared;
        public int Count => chainCache.Count;

        public SyncNodeChainCache(ConfiguredDataFeedProvider dataFeedProvider)
        {
            this.dataFeedProvider = dataFeedProvider;
        }

        void IChainCache.StopCalculating()
        {
            //it's sync so nothing to stop
        }

        public void Add(ICacheMember cacheMember)
        {
            chainCache.Add(new FrameCache(cacheMember));
            AnyCacheCleared?.Invoke(-1, chainCache.Count - 1);
        }

        public void AddAt(ICacheMember cacheMember, int index)
        {
            chainCache.Insert(index, new FrameCache(cacheMember));
            ClearAllFramesAfter(index);
        }

        public void Remove(int index)
        {
            chainCache.RemoveAt(index);
            ClearAllFramesAfter(index);
        }

        public void RemoveAll()
        {
            chainCache.Clear();
            AnyCacheCleared?.Invoke(-1, 0);
        }

        public void Move(int indexToMove, int moveTo)
        {
            FrameCache data = chainCache[indexToMove];
            chainCache.RemoveAt(indexToMove);
            chainCache.Insert(moveTo, data);
            ClearAllFramesAfter(Math.Min(indexToMove, moveTo));
        }

        public DataFeed GetCache(int index, int frame)
        {
            return chainCache[index].GetCache(frame);
        }

        public bool TryGetCache(int index, int frame, out DataFeed cache)
        {
            if(chainCache.Count > index)
            {
                cache = chainCache[index].GetCache(frame);
                return true;
            }
            cache = null;
            return false;
        }

        public bool IsFullyCached(int frame)
        {
            for(int i = 0; i < chainCache.Count; i++)
            {
                if (chainCache[i].ShouldCalculateFrame(frame))
                    return false;
            }
            return true;
        }

        public bool IsFullyCachedAfter(int index, int frame)
        {
            for(int i = index; i < chainCache.Count; i++)
            {
                if (chainCache[i].ShouldCalculateFrame(frame))
                    return false;
            }
            return true;
        }

        public void ClearCacheAfter(int index, int frame)
        {
            for(int i = index; i < chainCache.Count; i++)
            {
                chainCache[i].ClearCache(frame);
            }
            AnyCacheCleared?.Invoke(frame, index);
        }

        /// <summary>
        /// clear all the frames caches for the nodes between [index; Count[
        /// </summary>
        /// <param name="index"></param>
        public void ClearAllFramesAfter(int index)
        {
            for(int i = index; i < chainCache.Count; i++)
            {
                chainCache[i].ClearAllCache();
            }
            AnyCacheCleared?.Invoke(-1, index);
        }

        /// <summary>
        /// does calculate if not necessary
        /// </summary>
        /// <param name="frameStart"></param>
        /// <param name="frameEnd"></param>
        /// <param name="nodeNb"></param>
        public void StartBatchCalculate(int frameStart, int frameEnd, int nodeNb)
        {
            Prepare?.Invoke();
            for (int i = frameStart; i < frameEnd; i++)
            {
                Calculate(i, nodeNb, chainCache.Count);
            }
        }

        /// <summary>
        /// does calculate if not necessary
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="nodeNb"></param>
        public void StartSingleFrame(int frame, int nodeNb, int endNode = -1)
        {
            if (endNode == -1)
                endNode = chainCache.Count;
            Prepare?.Invoke();
            Calculate(frame, nodeNb, endNode);
        }

        private void Calculate(int frame, int nodeNb, int endNode)
        {
            if (IsFullyCachedAfter(nodeNb, frame))
                return;

            DoCalculationWork(frame, nodeNb, endNode);
        }

        private void DoCalculationWork(int frame, int skipNode, int untilNode)
        {
#if !DEBUG
            try
            {
#endif
            for (int i = skipNode; i < untilNode; i++)
            {
                DataFeed previous;
                //if this is the first node, create a new datafeed
                //otherwise clone the datafeed from the previous node 
                if (i == 0)
                {
                    previous = dataFeedProvider();
                }
                else
                {
                    DataFeed previousNotCloned = GetCache(i - 1, frame);
                    if (previousNotCloned == null)
                    {
                    //go back to calculate preceding node again
                    //i -= 2;
                    //continue;
                        throw new Exception("this is not possible, previous is not cached in SyncNodeChainCache");
                    }
                    previous = previousNotCloned.Clone();
                }

                chainCache[i].CalculateIfNeeded(frame, previous);
            }
#if !DEBUG
            }
            catch(Exception e)
            {
                Logger.WriteLine("error evaluating node:" + e);
            }
#endif
        }
    }
}
