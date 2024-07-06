using System;
using System.Collections.Generic;

namespace Motio.NodeCommon.Utils
{
    /// <summary>
    /// represents a range of frame, continuous or not 
    /// </summary>
    public class FrameRange : IFrameRange
    {
        List<IFrameRange> ranges = new List<IFrameRange>();

        public static EmptyFrameRange Empty { get; } = new EmptyFrameRange();
        public static InfinityFrameRange Infinite { get; } = new InfinityFrameRange();

        public void Add(IFrameRange range)
        {
            ranges.Add(range);
        }

        public int ClosestPreviousInRange(int frame)
        {
            if (ranges.Count == 0)
                return -1;
            int closestFrame = ranges[0].ClosestPreviousInRange(frame);
            int closestDistance = Math.Abs(closestFrame - frame);
            //early out if one of the range includes the current frame
            if (closestDistance == 0)
                return closestFrame;

            for (int i = 1; i < ranges.Count; i++)
            {
                int closestCurrentFrame = ranges[i].ClosestPreviousInRange(frame);
                int distance = Math.Abs(closestCurrentFrame - frame);
                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFrame = closestCurrentFrame;
                }
                //early out if one of the range includes the current frame
                if (closestDistance == 0)
                    return closestFrame;
            }
            return closestFrame;
        }

        public bool ContainsFrame(int frame)
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                if (ranges[i].ContainsFrame(frame))
                    return true;
            }
            return false;
        }
    }

    public struct EmptyFrameRange : IFrameRange
    {
        public int ClosestPreviousInRange(int frame)
        {
            return frame;
        }

        public bool ContainsFrame(int frame)
        {
            return false;
        }
    }

    public class UniqueFrameRange : IFrameRange
    {
        private int calculatedFrame = -1;

        public int ClosestPreviousInRange(int frame)
        {
            //return the calculated frame when possible so 
            //we read from it's cache
            if (calculatedFrame == -1)
                return frame;
            else
                return calculatedFrame;
        }

        public bool ContainsFrame(int frame)
        {
            return calculatedFrame == -1;
        }

        public void FrameWasCalculated(int frame)
        {
            calculatedFrame = frame;
        }

        public void CacheWasCleared()
        {
            calculatedFrame = -1;
        }
    }

    /// <summary>
    /// a frame range that contains all the frames 
    /// </summary>
    public struct InfinityFrameRange : IFrameRange
    {
        public int ClosestPreviousInRange(int frame)
        {
            return frame;
        }

        public bool ContainsFrame(int frame)
        {
            return true;
        }
    }

    /// <summary>
    /// a range of frame that is continuous (can't have holes)
    /// </summary>
    public struct ClosedFrameRange : IFrameRange
    {
        /// <summary>
        /// included start
        /// </summary>
        public readonly int start;
        /// <summary>
        /// included end
        /// </summary>
        public readonly int end;

        public ClosedFrameRange(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        public int ClosestPreviousInRange(int frame)
        {
            bool before = frame < start;
            bool after = frame > end;

            //inside the range
            if (!before && !after)
                return frame;
            if (before)
                return start;
            if (after)
                return end;
            throw new System.Exception("this is litteraly impossible");
        }

        public bool ContainsFrame(int frame)
        {
            return frame >= start && frame <= end;
        }
    }

    public interface IFrameRange
    {
        bool ContainsFrame(int frame);
        /// <summary>
        /// returns the frame closest to <paramref name="frame"/> but that is in the range. if not possible return <paramref name="frame"/>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        int ClosestPreviousInRange(int frame);
    }
}
