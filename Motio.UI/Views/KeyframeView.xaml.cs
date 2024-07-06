using System.Windows.Controls;
using System.Windows.Input;
using System;
using Motio.UI.ViewModels;
using Motio.UI.Renderers.KeyframeRendering;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for KeyframeView.xaml
    /// </summary>
    public partial class KeyframeView : UserControl
    {
        MainControlViewModel mainViewModel;

        public KeyframeView()
        {
            InitializeComponent();          
        }

        private void KeyframeView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if(mainViewModel == null)
            {
                mainViewModel = (MainControlViewModel)DataContext;
            }
        }

        //for some reason since we have a window event for mousedown/up/move
        //the rectangle only receives the down events and not the move and up
        //so we have to capture the mouse on down 
        private void clickAndDragCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(sender is Canvas rec && Mouse.Captured == null)
            {
                rec.CaptureMouse();
            }
        }

        private void clickAndDragCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(sender is Canvas rec && Mouse.Captured == rec)
            {
                if(e.LeftButton == MouseButtonState.Pressed)
                {
                    UpdateCurrentFrame(e.GetPosition(timeSliderCanvas).X);
                }
            }
        }

        private void clickAndDragCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(sender is Canvas rec && Mouse.Captured == rec)
            {
                rec.ReleaseMouseCapture();
                UpdateCurrentFrame(e.GetPosition(timeSliderCanvas).X);
            }
        }

        private void UpdateCurrentFrame(double x)
        {

            mainViewModel.AnimationTimeline.CurrentFrame =
                    Convert.ToInt32(KeyframeTimelineRenderer.CanvasToKeyframeSpace(
                        inPoint: x,
                        left: mainViewModel.KeyframePanel.Left,
                        right: mainViewModel.KeyframePanel.Right,
                        actualWidth: dockPanel.ActualWidth));
        }
    }
}
