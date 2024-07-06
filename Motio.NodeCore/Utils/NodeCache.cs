using Motio.NodeCommon;
using Motio.NodeCommon.Utils;
using System.Threading;

namespace Motio.NodeCore.Utils
{
    /// <summary>
    /// cache values for frames 
    /// </summary>
    public class NodeCache
    {
        private GraphicsNode nodeHost;
        private GenericCache<DataFeed> cache;
        private bool continueWorkingInBackground = false;
        private bool singleFrame = false;
        private bool callPrepare = false;
        private object boolsLock = new object();
        /// <summary>
        /// the frame currently being calculated in the background
        /// kept as a field because 
        /// is the background thread is asked to stop and start very fast it might not have stopped at all
        /// but the currently calculating value will still be the asked frame
        /// </summary>
        private int currentlyCalculating = 0;

        public NodeCache(GraphicsNode nodeHost)
        {
            this.nodeHost = nodeHost;
            //cache = new GenericCache<DataFeed>(nodeHost.EvaluateFrame);
        }

        public DataFeed GetCachedDataAtFrame(int frame)
        {
            return cache.GetCachedDataForFrame(frame);
        }

        public bool IsFrameCached(int frame)
        {
            return !cache.ShouldCalculateDataForFrame(frame);
        }

        public void StartOrGoBackgroundProcessing(int atFrame)
        {
            StartOrGoBackgroundProcessing(atFrame, false);
        }

        /// <summary>
        /// single frame is used to only calculate the given frame and not
        /// the entire timeline starting at the given frame
        /// </summary>
        /// <param name="atFrame"></param>
        /// <param name="singleFrame"></param>
        public void StartOrGoBackgroundProcessing(int atFrame, bool singleFrame)
        {
            lock (boolsLock)
            {
                if(this.singleFrame == singleFrame 
                    && currentlyCalculating == atFrame 
                    && continueWorkingInBackground)
                {
                    //it's already going do touch it
                    return;
                }
                this.singleFrame = singleFrame;

                callPrepare = true;
                if (continueWorkingInBackground)
                {
                    //if already background processing skip forward
                    Interlocked.Exchange(ref currentlyCalculating, atFrame);
                }
                else
                {
                    //set the currently calculating to the asked frame
                    //using Interlocked because th backgroudn thread might be still going 
                    //even tho we asked it to stop
                    continueWorkingInBackground = true;
                    Interlocked.Exchange(ref currentlyCalculating, atFrame);
                    ThreadPool.QueueUserWorkItem(BackgroundWork);
                }
            }
        }

        /// <summary>
        /// this doesn't join the thread 
        /// </summary>
        public void StopBackgroundProcessing()
        {
            //thread listens to this value so setting it to false should stop the thread 
            //after the calculation of the next frame
            lock(boolsLock)
            {
                continueWorkingInBackground = false;
            }
        }

        public void InvalidateFrame(int frame)
        {
            cache.InvalidateDataForFrame(frame);
        }

        /// <summary>
        /// this method whould only be called in the background thread 
        /// it evaluates each nodes 
        /// </summary>
        /// <param name="arg"></param>
        private void BackgroundWork(object arg)
        {
            //while the thread is still allowed to run 
            // and we didn't reach the end of the timeline
            bool going = true;
            while(going)
            {
                lock (boolsLock)
                {
                    going = continueWorkingInBackground
                        && currentlyCalculating < nodeHost.timelineHost.MaxFrame;
                    if (callPrepare)
                    {
                        callPrepare = false;
                        nodeHost.PrepareBatchProcessing();
                    }
                }

                bool calculated = cache.CalculateAndCacheFrameIfNecessary(currentlyCalculating);
                //if (calculated)
                //    nodeHost.timelineHost.TriggerCacheUpdate(currentlyCalculating);

               // AnimationTimeline.Instance.CacheUpdated = !AnimationTimeline.Instance.CacheUpdated;
                //check if you can still go after before incrementing
                //otherwise you could skip a frame
                //because the new value of currentlyCalculting would have been set 
                //by the main thread and you incremented it

                lock (boolsLock)
                {
                    if (!continueWorkingInBackground || singleFrame)
                        going = false;
                }
                
                //currentlyCalculating++ but thread safe
                //because it could have been changed by the main thread
                if(going)
                    Interlocked.Increment(ref currentlyCalculating);
            }
            lock (boolsLock)
            {
                continueWorkingInBackground = false;
            }
        }
    }
}
