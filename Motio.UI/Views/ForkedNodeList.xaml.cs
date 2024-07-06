using Motio.ClickLogic;
using Motio.Debuging;
using Motio.NodeCore;
using Motio.UI.Utils;
using Motio.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for ForkedNodeList.xaml
    /// </summary>
    public partial class ForkedNodeList : UserControl
    {
        private PropertyPanelDisplay latestHit;
        private PropertyPanelDisplay clicked;
        private PropertyPanelDisplay adonner;
        private Rectangle positionPreview;
        private bool left = false;

        public ForkedNodeList()
        {
            InitializeComponent();
        }

        private void PropertyPanelDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            if(element.Tag == null)
            {
                ClickAndDragHandler handler = new ClickAndDragHandler(element, true)
                {
                    OnDragEnterWithSender = DragEnter_,
                    OnDrag = DragStay_,
                    OnDragEnd = DragLeave_,
                    OnClickWithSender = Click_
                };
            }
        }

        private void Click_(object sender, MouseEventArgs e)
        {
            NodeViewModel clickContext = ((FrameworkElement)sender).DataContext as NodeViewModel;
            this.FindMainViewModel().PropertyPanel.AddToPropertyPanel(clickContext);
        }

        private void DragEnter_(object sender, ClickLogic.DragEventArgs e)
        {
            if (!(sender is PropertyPanelDisplay pd))
                return;

            clicked = pd;
            Canvas overEverything = this.FindMainViewModel().CanvasOverEverything;
            Point position = Mouse.GetPosition(overEverything);

            //clicked.Opacity = 0;
            //the datacontext can only be used by one PropertyPanelDisplay at a time
            //since it's cached in the viewmodel
            //so setting the datacontect on the adonner will make clicked invisible
            adonner = new PropertyPanelDisplay
            {
                DataContext = clicked.DataContext,
                Opacity = 0.5,
                IsHitTestVisible = false
            };
            overEverything.Children.Add(adonner);
            //move it 10 away from the mouse so the hit test doesn't get it (even with HitTestVisible set to false)
            Canvas.SetLeft(adonner, position.X + 10);
            Canvas.SetTop(adonner, position.Y + 10);

            positionPreview = new Rectangle()
            {
                Fill = (Brush)Application.Current.Resources["UnderlineColor"],
                Width = 5,
                Height = clicked.ActualHeight
            };
            overEverything.Children.Add(positionPreview);
            UpdatePositionPreview();
        }

        private void UpdatePositionPreview()
        {
            Canvas overEverything = this.FindMainViewModel().CanvasOverEverything;
            Point position;
            double widthOver2 = positionPreview.Width / 2;
            if (latestHit == null || latestHit == clicked)
            {
                position = new Point(-5, 0);
            }
            else
            {
                Point toTransform = left ? new Point(-widthOver2, 0) : new Point(latestHit.ActualWidth - widthOver2, 0);
                try
                {
                    position = latestHit.TransformToVisual(overEverything).Transform(toTransform);
                }
                catch (Exception e)
                {
                    Logger.WriteLine("error click and drag:" + e);
                    position = new Point(-5, 0);
                }
            }
            Canvas.SetLeft(positionPreview, position.X);
            Canvas.SetTop(positionPreview, position.Y);
        }

        private void DragStay_(ClickLogic.DragEventArgs e)
        {
            var wind = Window.GetWindow(this);
            //the filter gets the containers but the resultCallback get the details (paths, textblock etc)
            //so we only use the filter and stop at the first result of the resultCallback
            Canvas overEverything = this.FindMainViewModel().CanvasOverEverything;
            Point position = Mouse.GetPosition(overEverything);
            //move it 10 away from the mouse so the hit test doesn't get it (even with HitTestVisible set to false)
            Canvas.SetLeft(adonner, position.X + 10);
            Canvas.SetTop(adonner, position.Y + 10);

            VisualTreeHelper.HitTest(wind,
                HitTestFilter,
                t => HitTestResultBehavior.Stop,
                new PointHitTestParameters(Mouse.GetPosition(wind)));
            UpdatePositionPreview();
        }

        private HitTestFilterBehavior HitTestFilter(DependencyObject o)
        {
            if (!(o is PropertyPanelDisplay display) 
                || (display.DataContext as NodeViewModel)?.Host != (clicked.DataContext as NodeViewModel)?.Host)
                return HitTestFilterBehavior.Continue;
            latestHit = display;

            Point position = Mouse.GetPosition(latestHit);
            left = position.X < latestHit.ActualWidth / 2;

            return HitTestFilterBehavior.Stop;
        }

        private void DragLeave_(ClickLogic.DragEventArgs e)
        {
            Canvas overEverything = this.FindMainViewModel().CanvasOverEverything;
            //clicked.Opacity = 1;
            overEverything.Children.Remove(adonner);
            adonner.DataContext = null;
            adonner = null;
            //re set the datacontext since now it's no longer used by the adonner
            var d = clicked.DataContext;
            clicked.DataContext = null;
            clicked.DataContext = d;

            overEverything.Children.Remove(positionPreview);
            positionPreview = null;

            if (latestHit != null && latestHit != clicked)
            {
                if(latestHit.DataContext is GraphicsAffectingNodeViewModel gAffLatest
                    && clicked.DataContext is GraphicsAffectingNodeViewModel gAffClicked
                    && gAffLatest._host == gAffClicked._host)
                {
                    GraphicsNode parent = (GraphicsNode)gAffLatest._host.Original;

                    GraphicsAffectingNode clickedNode = (GraphicsAffectingNode)gAffClicked.Original;
                    GraphicsAffectingNode exchangeWith = (GraphicsAffectingNode)gAffLatest.Original;
                    int index1 = parent.attachedNodes.IndexOf(clickedNode);
                    int index2 = parent.attachedNodes.IndexOf(exchangeWith);
                    //si index 1 est plus petit que index 2 la node se met a droite
                    //si index 1 est plus grand que index 2 la node se met a gauche
                    if (index1 < index2 && left)
                        index2 -= 1;
                    if (index1 > index2 && !left)
                        index2 += 1;

                    index1 = Geometry.MathHelper.Clamp(index1, 0, parent.attachedNodes.Count - 1);
                    index2 = Geometry.MathHelper.Clamp(index2, 0, parent.attachedNodes.Count - 1);
                    parent.attachedNodes.Move(index1, index2);
                }
                else if(latestHit.DataContext is PropertyAffectingNodeViewModel pAff)
                {
                    throw new NotImplementedException("you didn't implement move pAff yet");
                }
            }
        }
    }
}
