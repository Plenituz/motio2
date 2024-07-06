#if !DEBUG
using Motio.Debuging;
#endif
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Motio.NodeCommon.Utils
{
    public delegate void Calculator(int frame, DataFeed dataFeed);
    public delegate void DoneCalculatingEvent(int frame);
    public delegate DataFeed ConfiguredDataFeedProvider();

    public class NodeChainCache : IChainCache
    {
        int IChainCache.Count => chainCache.Count;
        private ConcurrentDictionary<int, Thread> currentlyCalculating = new ConcurrentDictionary<int, Thread>();
        private ConcurrentList<FrameCache> chainCache = new ConcurrentList<FrameCache>();
        private ConfiguredDataFeedProvider dataFeedProvider;
        /// <summary>
        /// careful this event is called in the calculating thread
        /// </summary>
        public event DoneCalculatingEvent DoneCalculating;
        public event Action BatchCalculationDone;
        public event Action Prepare;

        public NodeChainCache(ConfiguredDataFeedProvider dataFeedProvider)
        {
            this.dataFeedProvider = dataFeedProvider;
        }

        public void Add(ICacheMember cacheMember)
        {
            chainCache.Add(new FrameCache(cacheMember));
        }

        public void AddAt(ICacheMember cacheMember, int index)
        {
            chainCache.AddAt(new FrameCache(cacheMember), index);
            ClearAllFramesAfter(index);
        }

        public void Remove(int index)
        {
            chainCache.Remove(index);
            ClearAllFramesAfter(index);
        }

        public void RemoveAll()
        {
            AbortAnJoinBatch();
            chainCache.RemoveAll();
        }

        public void Move(int indexToMove, int moveTo)
        {
            chainCache.Move(indexToMove, moveTo);
            ClearAllFramesAfter(Math.Min(indexToMove, moveTo));
        }

        //public bool ShouldCalculate(int index, int frame) => chainCache[index].ShouldCalculateFrame(frame);
        public DataFeed GetCache(int index, int frame) => chainCache[index].GetCache(frame);
        public bool TryGetCache(int index, int frame, out DataFeed dataFeed)
        {
            if(chainCache.TryGetValue(index, out FrameCache frameCache))
            {
                dataFeed = frameCache.GetCache(frame);
                return true;
            }
            dataFeed = null;
            return false;
        }

        //public void ClearCache(int index, int frame)
        //{
        //    FrameCache cache = chainCache[index];
        //    cache.ClearCache(frame);
        //}

        //public void ClearAllFrames(int index)
        //{
        //    FrameCache cache = chainCache[index];
        //    cache.ClearAllCache();
        //}

        public bool IsFullyCached(int frame)
        {
            foreach(FrameCache cache in chainCache.Enumerate)
            {
                if (cache.ShouldCalculateFrame(frame))
                    return false;
            }
            return true;
        }

        public bool IsFullyCachedAfter(int index, int frame)
        {
            int i = 0;
            foreach (FrameCache cache in chainCache.Enumerate)
            {
                if (i++ < index)
                    continue;
                if (cache.ShouldCalculateFrame(frame))
                    return false;
            }
            return true;
        }

        public void ClearCacheAfter(int index, int frame)
        {
            int i = 0;
            foreach (FrameCache cache in chainCache.Enumerate)
            {
                if (i++ < index) 
                    continue;
                cache.ClearCache(frame);
                //ClearCache(i, frame);
            }
        }

        /// <summary>
        /// clear all the frames caches for the nodes between [index; Count[
        /// </summary>
        /// <param name="index"></param>
        public void ClearAllFramesAfter(int index)
        {
            int i = 0;
            foreach (FrameCache cache in chainCache.Enumerate)
            {
                if (i++ < index)
                    continue;
                cache.ClearAllCache();
                //ClearAllFrames(i);
            }
        }

        private void CallPrepare()
        {
            ThreadPool.QueueUserWorkItem(_ => Prepare?.Invoke());
        }

        public void StartBatchCalculate(int frameStart, int frameEnd, int nodeNb)
        {
            CallPrepare();
            for(int i = frameStart; i < frameEnd; i++)
            {
                StartCalculate(i, nodeNb);
            }
        }

        void IChainCache.StartSingleFrame(int frame, int nodeIndex, int endNode) => StartSingleFrame(frame, nodeIndex);
        public void StartSingleFrame(int frame, int nodeNb)
        {
            CallPrepare();
            StartCalculate(frame, nodeNb);
        }

        private void StartCalculate(int frame, int nodeNb)
        {
            if (IsFullyCachedAfter(nodeNb, frame))
                return;

            ThreadPool.QueueUserWorkItem(BackgroundWork, new StateObj(frame, nodeNb));
        }

        void IChainCache.StopCalculating() => AbortAnJoinBatch();
        public void AbortAnJoinBatch()
        {
            if (currentlyCalculating.IsEmpty)
                return;
            foreach(Thread thread in currentlyCalculating.Values)
            {
                thread.Abort();
                //thread.Join();//don't join, sometimes the thread don't actually stop since it's a threadpool thread
            }
        }

        private void BackgroundWork(object state)
        {
            StateObj stateObj = (StateObj)state;
            try
            {
                if (!currentlyCalculating.TryAdd(stateObj.frame, Thread.CurrentThread))
                    return;
                //if there is not another thread already calculating this frame

                int max = chainCache.Count;
                for(int i = stateObj.skipNode; i < max; i++)
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
                        DataFeed previousNotCloned = GetCache(i - 1, stateObj.frame);
                        if(previousNotCloned == null)
                        {
                            //go back to calculate preceding node again
                            i -= 2;
                            //Logger.WriteLine("previous was not ready, trying again");
                            continue;
                        }
                        previous = previousNotCloned.Clone();
                    }

                    chainCache[i].CalculateIfNeeded(stateObj.frame, previous);
                }

                currentlyCalculating.TryRemove(stateObj.frame, out Thread t);
                DoneCalculating?.Invoke(stateObj.frame);
            }
            catch (ThreadAbortException)
            {
                currentlyCalculating.TryRemove(stateObj.frame, out Thread t);
            }
#if !DEBUG
            catch(Exception e)
            {
                Logger.WriteLine("error evaluating node:" + e);
            }
#endif
            finally
            {
                if (currentlyCalculating.IsEmpty)
                    BatchCalculationDone?.Invoke();
            }
        }

        private class StateObj
        {
            public int frame;
            public int skipNode;

            public StateObj(int frame, int skipNode)
            {
                this.frame = frame;
                this.skipNode = skipNode;
            }
        }
    }
}
