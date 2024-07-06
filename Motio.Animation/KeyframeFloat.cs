using Motio.Geometry;
using Motio.NodeCommon;
using Motio.ObjectStoring;
using Motio.Renderers.BezierRendering;
using System;
using System.ComponentModel;

namespace Motio.Animation
{
    /// <summary>
    /// creating a new class to put extra data about the bezier handles
    /// </summary>
    public class KeyframeFloat : IComparable, INotifyPropertyChanged, IBezierPoint
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [SaveMe]
        public int Time { get; set; }
        [SaveMe]
        public virtual float Value { get; set; }
        /// <summary>
        /// this vector is relative to the value
        /// </summary>
        [SaveMe]
        public Vector2 LeftHandle { get; set; } = Vector2.Zero;
        /// <summary>
        /// this vector is relative to the value
        /// the values dont go from 0 to 1 but are in "keyframe space" (not "interpolation space")
        /// </summary>
        [SaveMe]
        public Vector2 RightHandle { get; set; } = Vector2.Zero;
        /// <summary>
        /// the holder that contains this keyframe, or null
        /// </summary>
        public KeyframeHolder Holder { get; internal set; }

        float IBezierPoint.RightHandleX    { get => RightHandle.X; set => RightHandle = new Vector2(value, RightHandle.Y); }
        float IBezierPoint.RightHandleY    { get => RightHandle.Y; set => RightHandle = new Vector2(RightHandle.X, value); }
        float IBezierPoint.LeftHandleX     { get => LeftHandle.X;  set => LeftHandle = new Vector2(value, LeftHandle.Y); }
        float IBezierPoint.LeftHandleY     { get => LeftHandle.Y;  set => LeftHandle = new Vector2(LeftHandle.X, value); }
        float IBezierPoint.PointX          { get => Time;          set => Time = ToolBox.ConvertToInt(value); }
        float IBezierPoint.PointY          { get => Value;         set => Value = value; }

        public IBezierPoint NextPoint
        {
            get
            {
                if (Holder == null)
                    return null;
                int index = Holder.IndexOf(this);
                if (index == -1 || index == Holder.Count - 1)
                    return null;
                return Holder.KeyframeAt(index + 1);
            }
        }

        public IBezierPoint PreviousPoint
        {
            get
            {
                if (Holder == null)
                    return null;
                int index = Holder.IndexOf(this);
                if (index <= 0)
                    return null;
                return Holder.KeyframeAt(index - 1);
            }
        }

        //Vector2 IBezierPoint.RightHandleWorld { get => RightHandle; set => RightHandle = value; }
        //Vector2 IBezierPoint.LeftHandleWorld { get => LeftHandle; set => LeftHandle = value; }
        //Vector2 IBezierPoint.PointWorld { get => new Vector(Time, Value); set { Time = Convert.ToInt32(value.X); Value = value.Y; } }

        [CreateLoadInstance]
        static object CreateLoadInstance(object parent, Type t)
        {
            return new KeyframeFloat();
        }

        protected KeyframeFloat(){}

        public KeyframeFloat(int time, float value)
        {
            this.Time = time;
            this.Value = value;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return -1;

            if (obj is KeyframeFloat other)
            {
                if (Time > other.Time)
                    return 1;
                if (Time < other.Time)
                    return -1;
                return 0;
            }
            else
            {
                throw new ArgumentException("Object is not a Keyframe");
            }
        }

        /// <summary>
        /// not used anymore, but kept in case I ever need to create a keyframe that interpolate
        /// between something that's not a double (text ?)
        /// </summary>
        /// <returns></returns>
        public IInterpolator GetInterpolator()
        {
            return new DoubleInterpolator();
        }
    }
}
