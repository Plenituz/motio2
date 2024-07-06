using System;

namespace Motio.Renderers.BezierRendering
{
    public interface IBezierPoint
    {
        float RightHandleX { get; set; }
        float RightHandleY { get; set; }
        float LeftHandleX { get; set; }
        float LeftHandleY { get; set; }
        float PointX { get; set; }
        float PointY { get; set; }
        /// <summary>
        /// the next point on the curve or null
        /// </summary>
        IBezierPoint NextPoint { get; }
        /// <summary>
        /// the previous point on the curve or null
        /// </summary>
        IBezierPoint PreviousPoint { get; }
    }
}
