using Motio.Animation;
using Motio.UI.Utils;
using Motio.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Motio.UI.Views.TimelineDisplays
{
    /// <summary>
    /// Interaction logic for KeyframeButtonGroup.xaml
    /// </summary>
    public partial class KeyframeButtonGroup : UserControl
    {
        public KeyframeButtonGroup()
        {
            InitializeComponent();
        }

        private void Curve_Click(object sender, MouseEventArgs e)
        {
            NodePropertyBaseViewModel property = (NodePropertyBaseViewModel)DataContext;
            CurvePanelViewModel curvePanel = property.FindMainViewModel().CurvePanel;
            KeyframeHolder holder = property.KeyframeHolder;

            if (curvePanel.Renderer.Contains(holder))
                curvePanel.Renderer.Remove(holder);
            else
                curvePanel.Renderer.Add(holder);
        }

        private void Prev_Click(object sender, MouseEventArgs e)
        {
            NodePropertyBaseViewModel property = (NodePropertyBaseViewModel)DataContext;
            AnimationTimelineViewModel timeline = property.FindMainViewModel().AnimationTimeline;
            KeyframeHolder holder = property.KeyframeHolder;
            KeyframeFloat keyframe;

            if (holder.Count == 1)
            {
                keyframe = holder.KeyframeAt(0);
                if (keyframe.Time < timeline.CurrentFrame)
                    timeline.CurrentFrame = keyframe.Time;
                return;
            }

            holder.GetNextClosestKeyframe(timeline.CurrentFrame, out keyframe, out int index);
            if (index == 0)
                return;
            if(index == holder.Count - 1 && keyframe.Time < timeline.CurrentFrame)
            {
                timeline.CurrentFrame = keyframe.Time;
                return;
            }
            index--;

            timeline.CurrentFrame = holder.KeyframeAt(index).Time;
        }

        private void Add_Click(object sender, MouseEventArgs e)
        {
            NodePropertyBaseViewModel property = (NodePropertyBaseViewModel)DataContext;
            AnimationTimelineViewModel timeline = property.FindMainViewModel().AnimationTimeline;
            KeyframeHolder holder = property.KeyframeHolder;

            if (holder.GetNextClosestKeyframe(timeline.CurrentFrame, out _, out _))
                return;

            holder.AddKeyframe(new KeyframeFloat(timeline.CurrentFrame, (float)property.StaticValue));
        }

        private void Next_Click(object sender, MouseEventArgs e)
        {
            NodePropertyBaseViewModel property = (NodePropertyBaseViewModel)DataContext;
            AnimationTimelineViewModel timeline = property.FindMainViewModel().AnimationTimeline;
            KeyframeHolder holder = property.KeyframeHolder;
            KeyframeFloat keyframe;

            if (holder.Count == 1)
            {
                keyframe = holder.KeyframeAt(0);
                if (keyframe.Time > timeline.CurrentFrame)
                    timeline.CurrentFrame = keyframe.Time;
                return;
            }

            bool exact = holder.GetNextClosestKeyframe(timeline.CurrentFrame, out keyframe, out int index);
            if (exact)
                index++;
            if (index == holder.Count)
                return;
            keyframe = holder.KeyframeAt(index);
            if(keyframe.Time > timeline.CurrentFrame)
                timeline.CurrentFrame = keyframe.Time;
        }
    }
}
