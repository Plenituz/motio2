using Motio.Geometry;
using Motio.ObjectStoring;
using Motio.Renderers.BezierRendering;
using PropertyChanged;
using System;
using System.ComponentModel;

namespace Motio.Pathing
{
    public class PathPoint : INotifyPropertyChanged, IBezierPoint, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [SaveMe]
        public Vector2 Position { get; set; }
        /// <summary>
        /// vector from the position
        /// </summary>
        [SaveMe]
        public Vector2 RightHandle { get; set; }
        /// <summary>
        /// vector from the position
        /// </summary>
        [SaveMe]
        public Vector2 LeftHandle { get; set; }

        public Path Host { get; internal set; }

        [DoNotNotify]
        float IBezierPoint.RightHandleX { get => RightHandle.X; set => RightHandle = new Vector2(value, RightHandle.Y); }
        [DoNotNotify]
        float IBezierPoint.RightHandleY { get => RightHandle.Y; set => RightHandle = new Vector2(RightHandle.X, value); }
        [DoNotNotify]
        float IBezierPoint.LeftHandleX { get => LeftHandle.X; set => LeftHandle = new Vector2(value, LeftHandle.Y); }
        [DoNotNotify]
        float IBezierPoint.LeftHandleY { get => LeftHandle.Y; set => LeftHandle = new Vector2(LeftHandle.X, value); }
        [DoNotNotify]
        float IBezierPoint.PointX { get => Position.X; set => Position = new Vector2(value, Position.Y); }
        [DoNotNotify]
        float IBezierPoint.PointY { get => Position.Y; set => Position = new Vector2(Position.X, value); }
        public IBezierPoint NextPoint
        {
            get
            {
                if (Host == null)
                    return null;
                int index = Host.Points.IndexOf(this);
                if (index == -1 || (index == Host.Points.Count - 1 && !Host.Closed))
                    return null;
                if (index == Host.Points.Count - 1 && Host.Closed)
                    return Host.Points[0];
                return Host.Points[index + 1];
            }
        }
        public IBezierPoint PreviousPoint
        {
            get
            {
                if (Host == null)
                    return null;
                int index = Host.Points.IndexOf(this);
                if (index == -1 || (index == 0 && !Host.Closed))
                    return null;
                if (index == 0 && Host.Closed)
                    return Host.Points[Host.Points.Count - 1];

                return Host.Points[index - 1];
            }
        }

        public PathPoint Clone()
        {
            PathPoint copy = new PathPoint
            {
                RightHandle = RightHandle,
                LeftHandle = LeftHandle,
                Position = Position
            };
            return copy;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
