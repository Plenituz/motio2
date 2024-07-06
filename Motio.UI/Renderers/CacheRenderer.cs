using Motio.UI.Renderers.KeyframeRendering;
using Motio.UI.ViewModels;
using Motio.UICommon;
using Motio.UI.Utils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Motio.Configuration;

namespace Motio.UI.Renderers
{
    public class CacheRenderer
    {
        private SimpleRect _bounds = new SimpleRect();
        public SimpleRect Bounds
        {
            get
            {
                return _bounds;
            }
            set
            {
                _bounds = value;
                UpdateRender();
            }
        }
        /// <summary>
        /// this is what the elements are going to be rendered on
        /// </summary>
        private Canvas canvas;
        /// <summary>
        /// this is what is used to get the ActualWidth 
        /// </summary>
        private FrameworkElement container;
        private LinkedList<Rectangle> addedRects = new LinkedList<Rectangle>();
        private AnimationTimelineViewModel animationTimeline;

        public CacheRenderer(Canvas canvas, FrameworkElement container)
        {
            this.canvas = canvas;
            this.container = container;
            this.animationTimeline = canvas.FindMainViewModel().AnimationTimeline;
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Configs.GetValue<double>(Configs.CacheDisplayTimer))
            };
            timer.Tick += (_, __) =>
            {
                UpdateRender();
            };
            timer.Start();
        }

        public void UpdateRender()
        {
            //Calculating the size of a frame in canvas space, then the start position of the current 
            //Bounds in canvas space and placing the rectangles based on that
            double frame1 = KeyframeTimelineRenderer.KeyframeToCanvasSpace(
                1, Bounds.Left, Bounds.Right, container.ActualWidth);
            double frame0 = KeyframeTimelineRenderer.KeyframeToCanvasSpace(
                0, Bounds.Left, Bounds.Right, container.ActualWidth);

            //most the time, frame 0 and 1 are before the canvas and therefore will give 2 negative values
            double sizeFrame = Math.Abs(frame1 - frame0);

            //using Math.Ceiling to avoid the start that might be fucked up and 
            //push all the other frame rectangles out of place 
            double startPos = KeyframeTimelineRenderer.KeyframeToCanvasSpace(
                (int)Math.Ceiling(Bounds.Left), Bounds.Left, Bounds.Right, container.ActualWidth);

            //clear canvas of older rects
            while(addedRects.First != null)
            {
                canvas.Children.Remove(addedRects.First.Value);
                addedRects.RemoveFirst();
            }

            for (int i = 0; i < Bounds.Right - Bounds.Left; i++)
            {
                int currentFrame = (int)Math.Ceiling(Bounds.Left) + i;
                Brush brush = animationTimeline.IsFrameCached(currentFrame)
                    ? (Brush)Application.Current.Resources["CachedFrameColor"] :
                      (Brush)Application.Current.Resources["NotCachedFrameColor"];
                Rectangle rect = new Rectangle()
                {
                    IsHitTestVisible = false,
                    Fill = brush,
                    Width = sizeFrame,
                    Height = 5
                };
                addedRects.AddFirst(rect);
                Canvas.SetLeft(rect, startPos + sizeFrame * i);
                canvas.Children.Add(rect);
            }
        }

    }
}
