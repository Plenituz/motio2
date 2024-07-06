using Motio.Configuration;
using Motio.UI.Renderers.KeyframeRendering;
using Motio.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for TimelineLimitsAdjuster.xaml
    /// </summary>
    public partial class TimelineLimitsAdjuster : UserControl
    {
        private int MinSizeInFrame => Configs.GetValue<int>(Configs.MinTimelineViewSize);

        public double? Size
        {
            get => GetValue(SizeProperty) as double?;
            set => SetValue(SizeProperty, value);
        }
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(double), typeof(TimelineLimitsAdjuster), null);

        /// <summary>
        /// we need to know the container to be able to place the object on the canvas properly
        /// in mouse up and move
        /// </summary>
        public DockPanel Container {
            get => GetValue(ContainerProperty) as DockPanel;
            set => SetValue(ContainerProperty, value);
        }
        public static readonly DependencyProperty ContainerProperty =
            DependencyProperty.Register("Container", typeof(DockPanel), typeof(TimelineLimitsAdjuster), null);

        public double SizeSideFlap { get; set; } = 8;

        private double downPos;
        private Vector limitsOnDown;
        private MainControlViewModel mainViewModel;

        public TimelineLimitsAdjuster()
        {
            InitializeComponent();
        }

        private void TimelineLimitsAdjuster_Loaded(object sender, RoutedEventArgs e)
        {
            if(mainViewModel == null)
            {
                mainViewModel = (MainControlViewModel)DataContext;
                DataContext = this;
            }
        }

        private void MidRect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(sender is Rectangle rectangle && Mouse.Captured == null)
            {
                downPos = e.GetPosition(Container).X;
                limitsOnDown = new Vector(
                    mainViewModel.KeyframePanel.Left,
                    mainViewModel.KeyframePanel.Right);
                rectangle.CaptureMouse();
            }
        }

        private void MidRect_MouseMove(object sender, MouseEventArgs e)
        {
            if(sender is Rectangle rectangle && Mouse.Captured == rectangle)
            {

                double deltaMouse = e.GetPosition(Container).X - downPos;
                deltaMouse = KeyframeTimelineRenderer.CanvasToKeyframeSpace(
                    deltaMouse,
                    0,
                    mainViewModel.AnimationTimeline.MaxFrame,
                    Container.ActualWidth);


                double newLeft = limitsOnDown.X + deltaMouse;
                double newRight = limitsOnDown.Y + deltaMouse;
                //size of the region on click down
                //we use that to make sure the region doesn't shrink when dragging
                //it to the side 
                double originalSize = limitsOnDown.Y - limitsOnDown.X;
                //making sure you can't go over the timeline's limits
                if (newLeft < 0)
                {
                    newLeft = 0;
                    newRight = originalSize;
                }
                if(newRight > mainViewModel.AnimationTimeline.MaxFrame)
                {
                    newRight = mainViewModel.AnimationTimeline.MaxFrame;
                    newLeft = mainViewModel.AnimationTimeline.MaxFrame - originalSize;
                }

                mainViewModel.KeyframePanel.Left = newLeft;
                mainViewModel.KeyframePanel.Right = newRight;
            }
        }

        private void MidRect_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(sender is Rectangle rectangle && Mouse.Captured == rectangle)
            {
                rectangle.ReleaseMouseCapture();
            }
        }

        private void LeftAdjust_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Rectangle rectangle && Mouse.Captured == rectangle)
            {
                double newPos = e.GetPosition(Container).X;
                double newLeft = KeyframeTimelineRenderer.CanvasToKeyframeSpace(
                    newPos,
                    0,
                    mainViewModel.AnimationTimeline.MaxFrame,
                    Container.ActualWidth);

                //making sure you can't go over the timeline's limits
                if (newLeft < 0)
                    newLeft = 0;
                if (newLeft >= limitsOnDown.Y - MinSizeInFrame)
                    newLeft = limitsOnDown.Y - MinSizeInFrame;

                mainViewModel.KeyframePanel.Left = newLeft;
            }
        }

        private void RightAdjust_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Rectangle rectangle && Mouse.Captured == rectangle)
            {
                double newPos = e.GetPosition(Container).X;
                //convert back to keyframe space
                double newRight = KeyframeTimelineRenderer.CanvasToKeyframeSpace(
                    newPos, 
                    0,
                    mainViewModel.AnimationTimeline.MaxFrame,
                    Container.ActualWidth);

                //making sure you can't go over the timeline's limits
                if (newRight > mainViewModel.AnimationTimeline.MaxFrame)
                    newRight = mainViewModel.AnimationTimeline.MaxFrame;
                if (newRight <= limitsOnDown.X + MinSizeInFrame)
                    newRight = limitsOnDown.X + MinSizeInFrame;

                mainViewModel.KeyframePanel.Right = newRight;
            }
        }
    }
}
