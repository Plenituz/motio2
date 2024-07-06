// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;

namespace Motio.Geometry
{
    public struct BoundingBox2D : IEquatable<BoundingBox2D>
    {
        public Vector2 Min;

        public Vector2 Max;

        public const int CornerCount = 4;


        public BoundingBox2D(Vector2 min, Vector2 max)
        {
            this.Min = min;
            this.Max = max;
        }


        public ContainmentType Contains(BoundingBox2D box)
        {
            //test if all corner is in the same side of a face by just checking min and max
            if (box.Max.X < Min.X
                || box.Min.X > Max.X
                || box.Max.Y < Min.Y
                || box.Min.Y > Max.Y)
                return ContainmentType.Disjoint;


            if (box.Min.X >= Min.X
                && box.Max.X <= Max.X
                && box.Min.Y >= Min.Y
                && box.Max.Y <= Max.Y)
                return ContainmentType.Contains;

            return ContainmentType.Intersects;
        }

        public void Contains(ref BoundingBox2D box, out ContainmentType result)
        {
            result = Contains(box);
        }

        public ContainmentType Contains(Vector2 point)
        {
            ContainmentType result;
            this.Contains(ref point, out result);
            return result;
        }

        public void Contains(ref Vector2 point, out ContainmentType result)
        {
            //first we get if point is out of box
            if (point.X < this.Min.X
                || point.X > this.Max.X
                || point.Y < this.Min.Y
                || point.Y > this.Max.Y)
            {
                result = ContainmentType.Disjoint;
            }
            else
            {
                result = ContainmentType.Contains;
            }
        }

        private static readonly Vector2 MaxVector2 = new Vector2(float.MaxValue);
        private static readonly Vector2 MinVector2 = new Vector2(float.MinValue);

        /// <summary>
        /// Create a bounding box from the given list of points.
        /// </summary>
        /// <param name="points">The list of Vector3 instances defining the point cloud to bound</param>
        /// <returns>A bounding box that encapsulates the given point cloud.</returns>
        /// <exception cref="System.ArgumentException">Thrown if the given list has no points.</exception>
        public static BoundingBox2D CreateFromPoints(IEnumerable<Vector2> points)
        {
            if (points == null)
                throw new ArgumentNullException();

            var empty = true;
            var minVec = MaxVector2;
            var maxVec = MinVector2;
            foreach (var ptVector in points)
            {
                minVec.X = (minVec.X < ptVector.X) ? minVec.X : ptVector.X;
                minVec.Y = (minVec.Y < ptVector.Y) ? minVec.Y : ptVector.Y;

                maxVec.X = (maxVec.X > ptVector.X) ? maxVec.X : ptVector.X;
                maxVec.Y = (maxVec.Y > ptVector.Y) ? maxVec.Y : ptVector.Y;

                empty = false;
            }
            if (empty)
                throw new ArgumentException();

            return new BoundingBox2D(minVec, maxVec);
        }

        public static BoundingBox2D CreateMerged(BoundingBox2D original, BoundingBox2D additional)
        {
            BoundingBox2D result;
            result.Min.X = Math.Min(original.Min.X, additional.Min.X);
            result.Min.Y = Math.Min(original.Min.Y, additional.Min.Y);
            result.Max.X = Math.Max(original.Max.X, additional.Max.X);
            result.Max.Y = Math.Max(original.Max.Y, additional.Max.Y);
            return result;
        }

        public bool Equals(BoundingBox2D other)
        {
            return (this.Min == other.Min) && (this.Max == other.Max);
        }

        public override bool Equals(object obj)
        {
            return (obj is BoundingBox2D) ? this.Equals((BoundingBox2D)obj) : false;
        }

        public Vector2[] GetCorners()
        {
            return new Vector2[] {
                new Vector2(this.Min.X, this.Max.Y),
                new Vector2(this.Max.X, this.Max.Y),
                new Vector2(this.Max.X, this.Min.Y),
                new Vector2(this.Min.X, this.Min.Y),
                new Vector2(this.Min.X, this.Max.Y),
                new Vector2(this.Max.X, this.Max.Y),
                new Vector2(this.Max.X, this.Min.Y),
                new Vector2(this.Min.X, this.Min.Y)
            };
        }

        public override int GetHashCode()
        {
            return this.Min.GetHashCode() + this.Max.GetHashCode();
        }

        public static bool operator ==(BoundingBox2D a, BoundingBox2D b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(BoundingBox2D a, BoundingBox2D b)
        {
            return !a.Equals(b);
        }

        internal string DebugDisplayString
        {
            get
            {
                return string.Concat(
                    "Min( ", this.Min.DebugDisplayString, " )  \r\n",
                    "Max( ", this.Max.DebugDisplayString, " )"
                    );
            }
        }

        public override string ToString()
        {
            return "{{Min:" + this.Min.ToString() + " Max:" + this.Max.ToString() + "}}";
        }

    }
}
