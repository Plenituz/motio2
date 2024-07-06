using Motio.Debuging;
using Motio.Geometry;
using Motio.Graphics;
using Motio.Meshing;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Motio.UICommon
{
    /// <summary>
    /// helper methods to converter from Motio structs to WPF structs
    /// or helper functions for the ui
    /// </summary>
    public static class UIExtensions
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static System.Windows.Point ToWPFPoint(this Vector2 self)
        {
            return new System.Windows.Point(self.X, self.Y);
        }

        public static System.Windows.Vector ToWPFVector(this Vector2 self)
        {
            return new System.Windows.Vector(self.X, self.Y);
        }

        public static Vector2 ToVector2(this System.Windows.Point self)
        {
            return new Vector2((float)self.X, (float)self.Y);
        }

        public static Vector2 ToVector2(this System.Windows.Vector self)
        {
            return new Vector2((float)self.X, (float)self.Y);
        }

        public static Graphics.Color ToMotioColor(this System.Windows.Media.Color color)
        {
            return new Graphics.Color()
            {
                R = color.R,
                G = color.G,
                B = color.B,
                A = color.A
            };
        }

        public static System.Windows.Media.Color ToMediaColor(this Graphics.Color color)
        {
            return new System.Windows.Media.Color()
            {
                R = color.R,
                G = color.G,
                B = color.B,
                A = color.A
            };
        }
    }
}
