using Motio.Renderers.BezierRendering;
using Motio.UICommon;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Motio.Renderers
{
    public class GridRenderer
    {
        public SimpleRect Bounds { get; private set; } = new SimpleRect();

        private bool boundsChanged = true;
        private readonly Canvas canvas;
        private readonly CoordinateTransformer World2Canvas;
        private List<UIElement> addedToCanvas = new List<UIElement>();

        public GridRenderer(Canvas canvas, CoordinateTransformer world2canvas)
        {
            this.canvas = canvas;
            this.World2Canvas = world2canvas;
        }

        public void UpdateRender()
        {
            if (!boundsChanged)
                return;

            Clear();

            float width = (float)Math.Abs(Bounds.Right - Bounds.Left);
            float height = (float)Math.Abs(Bounds.Top - Bounds.Bottom);

            float intervalX = RoundToNearest(width/5);
            float intervalY = RoundToNearest(height/5);

            if (intervalX == 0)
                intervalX = 0.1f;
            if (intervalY == 0)
                intervalY = 0.1f;

            for (float x = RoundToNearest((float)Bounds.Left); x < Bounds.Right; x += intervalX)
            {
                if (x < Bounds.Left)
                    continue;
                (float X1, float Y1) = World2Canvas(x, (float)Bounds.Bottom);
                (float X2, float Y2) = World2Canvas(x, (float)Bounds.Top);

                //when bounds are backwards
                if (Y2 < Y1)
                    Y2 = -Y2;

                MakeAndAddLine(X1, Y1, X2, Y2);
                MakeAndAddTextBlock(x.ToString(), x: X1);
            }

            for (float y = RoundToNearest((float)Bounds.Bottom); y < Bounds.Top; y += intervalY)
            {
                if (y < Bounds.Bottom)
                    continue;
                (float X1, float Y1) = World2Canvas((float)Bounds.Left, y);
                (float X2, float Y2) = World2Canvas((float)Bounds.Right, y);

                //when bounds are backwards
                if (X2 < X1)
                    X2 = -X2;

                MakeAndAddLine(X1, Y1, X2, Y2);
                MakeAndAddTextBlock(y.ToString(), y: Y1);
            }

            boundsChanged = false;
        }

        private float RoundToNearest(float v)
        {
            if (Math.Abs(v) >= 10)
                return RoundToNearestTen(v);
            else if (Math.Abs(v) >= 1)
                return RoundToNearestUnit(v);
            else
                return RountToNearestDecimal(v);
        }

        private float RoundToNearestTen(float v)
        {
            return (float)Math.Floor(v / 10) * 10;
        }

        private float RoundToNearestUnit(float v)
        {
            return (float)Math.Floor(v);
        }

        private float RountToNearestDecimal(float v)
        {
            return (float)Math.Floor(v * 10) / 10;
        }

        /// <summary>
        /// set either x or y to a value not both
        /// </summary>
        /// <param name="text"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void MakeAndAddTextBlock(string text, float? x = null, float? y = null) 
        {
            TextBlock textBlock = MakeTextBlock(text);
            Panel.SetZIndex(textBlock, -10);
            Size textSize = ToolBox.MeasureTextBlock(textBlock);
            if(x.HasValue)
            {
                Canvas.SetLeft(textBlock, x.Value);
                Canvas.SetTop(textBlock, textSize.Height);
            }
            else if(y.HasValue)
            {
                Canvas.SetLeft(textBlock, 0);
                Canvas.SetTop(textBlock, y.Value);
            }
            else
            {
                throw new Exception("you must specifie a X or Y value in GridRenderer.MakeAndAddTextBlock");
            }
            addedToCanvas.Add(textBlock);
            canvas.Children.Add(textBlock);
        }

        private void MakeAndAddLine(float X1, float Y1, float X2, float Y2)
        {
            Line line = MakeLine(X1, Y1, X2, Y2);
            Panel.SetZIndex(line, -10);
            addedToCanvas.Add(line);
            canvas.Children.Add(line);
        }

        private TextBlock MakeTextBlock(string text)
        {
            return new TextBlock()
            {
                IsHitTestVisible = false,
                Text = text,
                TextAlignment = TextAlignment.Left,
                Foreground = (Brush)Application.Current.Resources["LighterBackgroundColor"],
                RenderTransform = new ScaleTransform(1, -1),
            };
        }

        private Line MakeLine(float X1, float Y1, float X2, float Y2)
        {
            return new Line()
            {
                IsHitTestVisible = false,
                X1 = X1,
                Y1 = Y1,
                X2 = X2,
                Y2 = Y2,
                Stroke = (Brush)Application.Current.Resources["LighterBackgroundColor"],
                StrokeThickness = 1
            };
        }

        public void Clear()
        {
            for(int i = 0; i < addedToCanvas.Count; i++)
            {
                canvas.Children.Remove(addedToCanvas[i]);
            }
            addedToCanvas.Clear();
        }

        /// <summary>
        /// Change the bounds, note that the bounds are in data space
        /// this will not trigger an UpdateRender
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        public void SetBounds(double left, double right, double top, double bottom)
        {
            boundsChanged = true;
            Bounds.Left = left;
            Bounds.Right = right;
            Bounds.Top = top;
            Bounds.Bottom = bottom;
        }

    }
}
