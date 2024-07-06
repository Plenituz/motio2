using Motio.Animation;
using Motio.UI.Renderers.KeyframeCurveRendering;
using Motio.UI.Renderers.KeyframeRendering;
using Motio.UI.Views;
using Motio.UICommon;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class CurvePanelViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        MainControlViewModel mainViewModel;

        //determines the bounds of what is displayed in the curve view
        public double Left { get; private set; } = 0;
        public double Right { get; private set; } = 100;
        public double Top { get; private set; } = 100;
        public double Bottom { get; private set; } = -100;
        private CurveViewCanvas _curveCanvas;
        public CurveViewCanvas CurveCanvas
        {
            get => _curveCanvas;
            set
            {
                if (_curveCanvas == value)
                    return;
                if (_curveCanvas != null)
                    _curveCanvas.SizeChanged -= CurveCanvas_SizeChanged;
                _curveCanvas = value;
                if(_curveCanvas != null)
                    _curveCanvas.SizeChanged += CurveCanvas_SizeChanged;
            }
        }

        public KeyframeCurveRenderer Renderer { get; set; }

        public SimpleRect Bounds => new SimpleRect()
        {
            Left = Left,
            Right = Right,
            Top = Top,
            Bottom = Bottom
        };

        public CurvePanelViewModel(MainControlViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
        }

        public void SetBounds(double left, double right, double top, double bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Renderer.UpdateBounds();
        }

        public virtual void ScaleToFit()
        {
            SimpleRect bounds = new SimpleRect()
            {
                Left = double.MaxValue,
                Right = double.MinValue,
                Top = double.MinValue,
                Bottom = double.MaxValue
            };
            foreach (KeyframeFloat keyframe in Renderer.RenderedKeyframes)
            {
                if (keyframe.Time < bounds.Left)
                    bounds.Left = keyframe.Time;
                if (keyframe.Time > bounds.Right)
                    bounds.Right = keyframe.Time;
                if (keyframe.Value < bounds.Bottom)
                    bounds.Bottom = keyframe.Value;
                if (keyframe.Value > bounds.Top)
                    bounds.Top = keyframe.Value;
            }
            //this is a unit relative to the bounds so it's always roughly the same perceived result
            Vector unit = new Vector(Math.Abs(bounds.Right - bounds.Left),
                    Math.Abs(bounds.Top - bounds.Bottom)) * 0.01;

            //padding
            bounds.Left -= unit.X * 50;
            bounds.Right += unit.X * 50;
            bounds.Top += unit.Y * 50;
            bounds.Bottom -= unit.Y * 50;

            //Security
            if (bounds.Top == bounds.Bottom)
            {
                //TODO this might cause problem if the Rect is inverted (bottom is top)
                bounds.Top += 5;
                bounds.Bottom -= 5;
            }
            if (bounds.Left == bounds.Right)
            {
                bounds.Left -= 5;
                bounds.Right += 5;
            }
            SetBounds(bounds.Left, bounds.Right, bounds.Top, bounds.Bottom);
        }

        private void CurveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Renderer.UpdateBounds();
        }

        public Point Keyframe2Canvas(Point inPoint)
        {
            return KeyframeToCanvasSpace(inPoint, Bounds, CurveCanvas.ActualWidth, CurveCanvas.ActualHeight);
        }

        public Point Canvas2Keyframe(Point inPoint)
        {
            return CanvasToKeyframeSpace(inPoint, Bounds, CurveCanvas.ActualWidth, CurveCanvas.ActualHeight);
        }

        /// <summary>
        /// transform from keyframe space (where a point is (Time, Value) ) 
        /// to Canvas space according to the given borders (canvas space being the content of the bounds given)
        /// </summary>
        /// <param name="inPoint"></param>
        /// <param name="bounds"></param>
        /// <param name="canvas"></param>
        /// <returns></returns>
        public static Point KeyframeToCanvasSpace(Point inPoint, SimpleRect bounds, double actualWidth, double actualHeight)
        {
            //these are the "canvas space" limit
            //basically drawing a square in keyframe space and rendering inside it

            double width = Math.Abs(bounds.Right - bounds.Left);
            double height = Math.Abs(bounds.Top - bounds.Bottom);

            //move the point to fit pos in frame
            //give the position from the border of the frame in percent
            double percentY, percentX;
            if (height == 0)
                percentY = 0;
            else
                percentY = (inPoint.Y - bounds.Bottom) / height;

            if(width == 0)
                percentX = 0;
            else
                percentX = (inPoint.X - bounds.Left) / width;
            //then multiply the distance in percent by the size of the canvas to get the position 
            //on the canvas
            Point moved = new Point(actualWidth * percentX, actualHeight * percentY);
            return moved;
        }

        /// <summary>
        /// Transform a position on the canvas to a value to put in a keyframe
        /// </summary>
        /// <param name="inPoint"></param>
        /// <param name="bounds"></param>
        /// <param name="canvas"></param>
        /// <returns></returns>
        public static Point CanvasToKeyframeSpace(Point inPoint, SimpleRect bounds, double actualWidth, double actualHeight)
        {
            double width = Math.Abs(bounds.Right - bounds.Left);
            double height = Math.Abs(bounds.Top - bounds.Bottom);

            double percentX = inPoint.X / actualWidth;
            double percentY = inPoint.Y / actualHeight;
            percentX *= width;
            percentY *= height;
            percentX += bounds.Left;
            percentY += bounds.Bottom;

            return new Point(percentX, percentY);
        }
    }
}
