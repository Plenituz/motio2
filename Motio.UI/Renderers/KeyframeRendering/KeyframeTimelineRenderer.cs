using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;
using System;
using System.ComponentModel;
using Motio.Animation;
using Motio.ClickLogic;
using Motio.UICommon;
using Motio.UI.ViewModels;
using Motio.Renderers;
using Motio.Selecting;

namespace Motio.UI.Renderers.KeyframeRendering
{
    public class KeyframeTimelineRenderer : BoundedItemRenderer<KeyframeFloat, KeyframeTimelineEllipse>
    {
        //TODO add listening to Keyframe property changed as in BaseKeyframeRenderer
        protected Canvas canvas;
        protected KeyframeHolder keyframeHolder;
        protected SelectionAwareClickHandler<KeyframeTimelineEllipse> clickHandler;
        protected AnimationTimelineViewModel timeline;

        public KeyframeTimelineRenderer(Canvas canvas, KeyframeHolder keyframeHolder, AnimationTimelineViewModel timeline)
        {
            this.canvas = canvas;
            this.keyframeHolder = keyframeHolder;
            this.timeline = timeline;

            void UpdateValue(KeyframeTimelineEllipse e, Point p)
            {
                double time = CanvasToKeyframeSpace(p.X, Bounds.Left, Bounds.Right, canvas.ActualWidth);
                e.keyframe.Time = Convert.ToInt32(time).Clamp(0, timeline.MaxFrame);
            }

            clickHandler = new SelectionAwareClickHandler<KeyframeTimelineEllipse>(canvas, Selection.KEYFRAMES_TIMELINE);
            clickHandler.UpdatePosition += UpdateValue;
        }

        protected override IEnumerable<KeyframeFloat> AllDataItems
        {
            get
            {
                IEnumerator<KeyframeFloat> keyframes = keyframeHolder.Enumerator();
                while (keyframes.MoveNext())
                    yield return keyframes.Current;
            }
        }

        protected override KeyframeTimelineEllipse CreateItem(KeyframeFloat dataItem)
        {
            dataItem.PropertyChanged += KeyframeFloat_PropertyChanged;

            KeyframeTimelineEllipse ellipse = new KeyframeTimelineEllipse(dataItem);
            clickHandler.Hook(ellipse);

            UpdateItem(dataItem, ellipse);
            ellipse.AddToCanvas(canvas);
            return ellipse;
        }

        protected override Point DataItemToPoint(KeyframeFloat dataItem)
        {
            return new Point(dataItem.Time, 0);
        }

        protected override Point DataSpaceToVisualSpace(KeyframeFloat dataItem)
        {
            double pos = KeyframeToCanvasSpace(dataItem.Time, Bounds.Left, Bounds.Right, canvas.ActualWidth);
            return new Point(pos, 0);
        }

        protected override void UpdateItem(KeyframeFloat dataItem, KeyframeTimelineEllipse visualItem, Point visualSpacePos)
        {
            visualItem.Update(visualSpacePos, Bounds, canvas);
        }

        protected override void RemoveItem(KeyframeFloat dataItem, KeyframeTimelineEllipse visualItem)
        {
            dataItem.PropertyChanged -= KeyframeFloat_PropertyChanged;
            clickHandler.UnHook(visualItem);
           // visualItem.Delete();
            visualItem.RemoveFromCanvas(canvas);
        }

        private void KeyframeFloat_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is KeyframeFloat keyframe)
            {
                //if the keyframe is no longer in this timeline, 
                //remove this instance from the update list of the keyframe
                //if it is just update the rendering
                if (keyframeHolder.Contains(keyframe))
                    UpdateItem(keyframe);
                else
                    keyframe.PropertyChanged -= KeyframeFloat_PropertyChanged;
            }
        }

        public static double KeyframeToCanvasSpace(double inPoint, double left, double right, double actualWidth)
        {
            //these are the "canvas space" limit
            //basically drawing a square in keyframe space and rendering inside it

            double width = Math.Abs(right - left);

            //move the point to fit pos in frame
            //give the position from the border of the frame in percent
            double percentX = (inPoint - left) / width;
            //then multiply the distance in percent by the size of the canvas to get the position 
            //on the canvas
            double moved = actualWidth * percentX;
            return moved;
        }

        public static double CanvasToKeyframeSpace(double inPoint, double left, double right, double actualWidth)
        {
            double width = Math.Abs(right - left);

            double percentX = inPoint / actualWidth;
            percentX *= width;
            percentX += left;

            return percentX;
        }
    }
}
