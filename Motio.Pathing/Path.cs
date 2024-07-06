using Motio.Animation;
using Motio.Geometry;
using Motio.ObjectStoring;
using Motio.Pathing.Undo;
using Motio.Undoing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Motio.Pathing
{
    public class Path : INotifyPropertyChanged, ICloneable
    {
        public delegate void PathPointChangedHandler(PathPoint point, string propertyName);
        /// <summary>
        /// cache of the distance to the next points. 
        /// key: point index
        /// value: distance to next
        /// </summary>
        private readonly Dictionary<int, float> distanceCache = new Dictionary<int, float>();
        internal bool noUndo = false;

        public event PropertyChangedEventHandler PropertyChanged;
        public event PathPointChangedHandler AnyPointPropertyChanged;

        /// <summary>
        /// you can add points directly to this list, PathPoint.Host is set automatically
        /// </summary>
        [SaveMe]
        public ObservableCollection<PathPoint> Points { get; private set; } = new ObservableCollection<PathPoint>();
        [SaveMe]
        public bool Closed { get; set; } = false;
        public float PathLength
        {
            get
            {
                if (Points.Count <= 1)
                    return 0;

                float distance = 0;
                for (int i = 1; i < Points.Count; i++)
                {
                    distance += DistanceToNext(i - 1);
                }
                if (Closed)
                    distance += DistanceToNext(Points.Count - 1);
                return distance;
            }
        }

        [OnDoneLoading]
        void DoneLoading()
        {
            //since points is set to a new instance by the laoder we need to resubscribe
            Points.CollectionChanged += Points_CollectionChanged;
            for(int i = 0; i < Points.Count; i++)
            {
                TakeOwnership(Points[i]);
            }
        }

        public Path()
        {
            Points.CollectionChanged += Points_CollectionChanged;
            PropertyChanged += Path_PropertyChanged;
        }

        /// <summary>
        /// distance from the point at index <paramref name="index"/> to the next one.
        /// the distances are calculated once and cached 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public float DistanceToNext(int index)
        {
            if(distanceCache.TryGetValue(index, out float distance))
            {
                return distance;
            }
            else
            {
                int indexNext = index == Points.Count - 1 ? 0 : index + 1;
                distance = DistanceBetween(Points[index], Points[indexNext]);
                distanceCache.Add(index, distance);
                return distance;
            }
        }

        /// <summary>
        /// calculate the distance between 2 bezier points. if you 
        /// have an instance of <see cref="Path"/>, use <see cref="DistanceToNext(int)"/> instead
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static float DistanceBetween(PathPoint p1, PathPoint p2)
        {
            float distance = 0;
            float step = 0.001f;
            Vector2 previousEch = p1.Position;

            for (float i = step; i < 1; i += step)
            {
                Vector2 ech2 = DoubleInterpolator.GetBezierPoint(p1.Position, p1.Position + p1.RightHandle, p2.Position + p2.LeftHandle, p2.Position, i);

                distance += Vector2.Distance(previousEch, ech2);
                previousEch = ech2;
            }
            return distance;
        }

        /// <summary>
        /// get the point a <paramref name="distance"/> on this path
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Vector2? PointAtDistance(float distance)
        {
            if (Points.Count == 0)
                return null;
            if (Points.Count == 1)
                return Points[0].Position;

            bool found = false;
            PathPoint from = null, to = null;
            float currentDist = 0;
            int max = Closed ? Points.Count + 1 : Points.Count;
            for (int i = 1; i < max; i++)
            {
                float newDist = DistanceToNext(i - 1);
                currentDist += newDist;
                if (currentDist > distance)
                {
                    from = Points[i - 1];
                    to = Closed && i == Points.Count ? Points[0] : Points[i];
                    currentDist -= newDist;
                    found = true;
                    break;
                }
            }
            if (!found)
                return null;

            float step = 0.001f;
            float percent = 0;
            Vector2 previousEch = from.Position;
            while (currentDist < distance)
            {
                Vector2 ech2 = DoubleInterpolator.GetBezierPoint(
                    from.Position,
                    from.Position + from.RightHandle,
                    to.Position + to.LeftHandle,
                    to.Position,
                    percent);
                currentDist += Vector2.Distance(previousEch, ech2);
                percent += step;
                previousEch = ech2;
            }
            return previousEch;
        }

        /// <summary>
        /// return an interpolated point on the path at <paramref name="percent"/> percent
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="distanceBased">is set to true the curve is sampled so the point is at the right distance
        /// instead of the usual mathematical percent. Usefull when doing a motion path for example. Note that this is more computer intensive</param>
        /// <returns></returns>
        public Vector2? PointAtPercent(float percent, bool distanceBased = false)
        {
            if (Points.Count == 0)
                return null;
            if (Points.Count == 1)
                return Points[0].Position;

            PathPoint from = null, to = null;
            int fromIndex = 0;
            float startPercent = 0, endPercent = 0;
            bool found = false;

            float lenghtPath = PathLength;
            float wantedDist = lenghtPath * percent;
            float currentDist = 0;
            int max = Closed ? Points.Count + 1 : Points.Count;
            for (int i = 1; i < max; i++)
            {
                startPercent = currentDist / lenghtPath;
                currentDist += DistanceToNext(i - 1);
                if(currentDist > wantedDist)
                {
                    endPercent = currentDist / lenghtPath;
                    fromIndex = i - 1;
                    from = Points[i - 1];
                    to = Closed && i == Points.Count ? Points[0] : Points[i];
                    found = true;
                    break;
                }
            }
            if (!found)
                return null;

            double localPercent = DoubleInterpolator.InverseLinear(percent, startPercent, endPercent);
            if (distanceBased)
                return EvaluateBezier(fromIndex, from, to, (float)localPercent);
            else
                return DoubleInterpolator.GetBezierPoint(from.Position, from.Position + from.RightHandle, to.Position + to.LeftHandle, to.Position, localPercent);
        }

        private Vector2 EvaluateBezier(int fromIndex, PathPoint from, PathPoint to, float percent)
        {
            float wantedDist = DistanceToNext(fromIndex)*percent;
            float currentDist = 0;

            float step = 0.001f;
            Vector2 previousEch = Points[fromIndex].Position;

            for (float i = step; i < 1f; i += step)
            {
                Vector2 ech2 = DoubleInterpolator.GetBezierPoint(
                    from.Position, 
                    from.Position + from.RightHandle, 
                    to.Position + to.LeftHandle, 
                    to.Position, 
                    i);
                currentDist += Vector2.Distance(ech2, previousEch);
                if(currentDist >= wantedDist)
                {
                    return ech2;
                }
                previousEch = ech2;
            }
            return to.Position;
        }

        private void Path_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!noUndo && e.PropertyName.Equals(nameof(Closed)))
            {
                UndoStack.Push(new ClosePathCommand(this, Closed));
            }
        }

        private void Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //change the host of the points that get added to this list
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int i = 0;
                        foreach (PathPoint point in e.NewItems)
                        {
                            TakeOwnership(point);
                            point.PropertyChanged += Point_PropertyChanged;
                            if(!noUndo)
                                UndoStack.Push(new AddToPathCommand(point, e.NewStartingIndex + i));
                            i++;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        int i = 0;
                        foreach(PathPoint point in e.OldItems)
                        {
                            //do it first so the Host of the point is still set
                            if (!noUndo)
                                UndoStack.Push(new RemoveFromPathCommand(point, e.OldStartingIndex + i));

                            LeaveOnTheSideOfTheRoad(point);
                            point.PropertyChanged -= Point_PropertyChanged;
                            i++;
                        }
                        //clear cache from the previous point to the deleted one
                        int indexToRemove = e.OldStartingIndex - 1;
                        if (indexToRemove == -1)
                            indexToRemove = Points.Count - 1;
                        distanceCache.Remove(indexToRemove);


                        if (e.OldItems.Count > 1)
                            throw new NotImplementedException();//just in case
                        
                        if (Points.Count <= 1 && Closed)
                            Closed = false;
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        foreach(PathPoint point in e.OldItems)
                        {
                            LeaveOnTheSideOfTheRoad(point);
                            point.PropertyChanged -= Point_PropertyChanged;
                        }
                        foreach (PathPoint point in e.NewItems)
                        {
                            TakeOwnership(point);
                            point.PropertyChanged += Point_PropertyChanged;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        while (Points.Count != 0)
                        {
                            Points[0].PropertyChanged -= Point_PropertyChanged;
                            LeaveOnTheSideOfTheRoad(Points[0]);
                        }
                    }
                    break;
            }
        }

        private void Point_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PathPoint point = (PathPoint)sender;
            AnyPointPropertyChanged?.Invoke(point, e.PropertyName);

            //remove distance cache for the moved point and the previous point
            int index = Points.IndexOf(point);
            distanceCache.Remove(index);
            if (index != 0)
                distanceCache.Remove(index - 1);
            else
                distanceCache.Remove(Points.Count - 1);
        }

        private void TakeOwnership(PathPoint point)
        {
            if (point.Host != null)
            {
                point.Host.Points.Remove(point);
            }
            point.Host = this;
        }

        /// <summary>
        /// leave the poor point without any owner and remove the point from my list
        /// </summary>
        /// <param name="point"></param>
        private void LeaveOnTheSideOfTheRoad(PathPoint point)
        {
            if (point.Host != this)
                throw new ArgumentException("can't remove point that's not from this path, dummy");
            Points.Remove(point);
            point.Host = null;
        }

        public Path Clone()
        {
            Path copy = new Path()
            {
                Closed = Closed
            };
            copy.noUndo = true;
            for(int i = 0; i < Points.Count; i++)
            {
                copy.Points.Add(Points[i].Clone());
            }
            copy.noUndo = false;
            return copy;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
