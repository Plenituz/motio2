using System.Windows;
using System.Windows.Controls;
using System;
using Motio.Animation;
using Motio.UICommon;
using Motio.Debuging;
using Motio.Selecting;

namespace Motio.UI.Renderers.KeyframeRendering
{
    public class KeyframeTimelineEllipse : KeyframeEllipse
    {
        public override string DefaultSelectionGroup => Selection.KEYFRAMES_TIMELINE;

        public KeyframeTimelineEllipse(KeyframeFloat keyframe) : base(keyframe)
        {
        }

        public override void Update(Point visualSpacePos, SimpleRect bounds, Canvas canvas)
        {
            base.Update(visualSpacePos, bounds, canvas);
            try
            {
                Canvas.SetLeft(this, visualSpacePos.X);
                Canvas.SetTop(this, 10);
            }
            catch (ArgumentException)
            {
                Logger.WriteLine("error setting position in KeyframeTimelineEllipse2");
            }
        }

        public override bool Delete()
        {
            if(keyframe.Holder != null)
                keyframe.Holder.RemoveKeyframe(keyframe);
            return true;
        }
    }
}
