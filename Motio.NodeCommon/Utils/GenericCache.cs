using System;
using System.Collections.Concurrent;

namespace Motio.NodeCommon.Utils
{
    public class GenericCache<T>
    {
        private ConcurrentDictionary<int, bool> dataInvalidate = new ConcurrentDictionary<int, bool>();
        private ConcurrentDictionary<int, T> dataCache = new ConcurrentDictionary<int, T>();
        private Func<int, T> CalculateDataForFrame;

        public GenericCache(Func<int, T> CalculateDataForFrame)
        {
            this.CalculateDataForFrame = CalculateDataForFrame;
            if (CalculateDataForFrame == null)
                throw new ArgumentNullException("CalculateDataForFrame can't be null");
        }

        /// <summary>
        /// returns null or the cached data if it exists
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public T GetCachedDataForFrame(int frame)
        {
            if (ShouldCalculateDataForFrame(frame))
            {
                //if the data sould be calculated then it's not cached
                return default(T);
            }
            else
            {
                //other wise it's good to go, extract it from the cache
                T outData = default(T);
                dataCache.TryGetValue(frame, out outData);
                //TryGetValue sets the outData to null if not found so 
                //we can just return it 
                return outData;
            }
        }

        /// <summary>
        /// returns true if the cache is invalid or the data never was cached (on a give frame)
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public bool ShouldCalculateDataForFrame(int frame)
        {
            //if data is invalid, you should recalculate it
            if (dataInvalidate.ContainsKey(frame) && dataInvalidate[frame])
                return true;
            //if the data is not invalid and is already in the list, you should not recalculate
            if (dataCache.ContainsKey(frame))
                return false;
            //if data is not in the dictionnary at all you should calculate it
            return true;
        }

        /// <summary>
        /// force the recalculation of data for the given frame
        /// force meaning it won't check if the data is invalid or already cached
        /// before calculating, so you should do it yourself before calling this, to avoid
        /// unecessary calculations
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public void ForceCalculateAndCacheDataForFrame(int frame)
        {
            //calculate the data 
            T data = CalculateDataForFrame(frame);
            //store/add it to the cache
            if (dataCache.ContainsKey(frame))
                dataCache[frame] = data;
            else
                dataCache.TryAdd(frame, data);
            //validate this data, because we just calculated it
            if(dataInvalidate.ContainsKey(frame))
                dataInvalidate[frame] = false;
        }

        /// <summary>
        /// do the calculation only if the frame is invalid or not never was calculated
        /// </summary>
        /// <param name="frame">frame to calculate</param>
        public bool CalculateAndCacheFrameIfNecessary(int frame)
        {
            if (ShouldCalculateDataForFrame(frame))
            {
                ForceCalculateAndCacheDataForFrame(frame);
                return true;
            }
            return false;
        }

        public void InvalidateDataForFrame(int frame)
        {
            if (dataCache.ContainsKey(frame))
            {
                if (dataInvalidate.ContainsKey(frame))
                    dataInvalidate[frame] = true;
                else
                    dataInvalidate.TryAdd(frame, true);
            }
        }
    }
}
