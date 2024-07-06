using Motio.ClickLogic;
using Motio.Selecting;
using Motio.UI.Renderers.KeyframeCurveRendering;
using Motio.UI.Renderers.KeyframeRendering;
using Motio.UI.ViewModels;
using Motio.UICommon;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for CurveView.xaml
    /// </summary>
    public partial class CurveView : UserControl
    {
        CurvePanelViewModel curvePanel;
        ClickAndDragHandler clickAndDragRect;
        ClickAndDragHandler clickAndDragCanvas;
        SimpleRect downRect;

        public CurveView()
        {
            InitializeComponent();
        }

        private void CurveView_Loaded(object sender, RoutedEventArgs args)
        {
            if(curvePanel == null)
            {
                MainControlViewModel mainViewModel = (MainControlViewModel)DataContext;
                curvePanel = mainViewModel.CurvePanel;
                curvePanel.CurveCanvas = curveCanvas;
                curvePanel.Renderer = new KeyframeCurveRenderer(curvePanel);

                Selection.Instance.ItemSelected += OnSelectionItemAdded;
                Selection.Instance.ItemUnSelected += OnSelectionItemRemoved;

                clickAndDragRect = new ClickAndDragHandler(ControlRect)
                {
                    OnClick = RectClick,
                    OnDragEnter = EnterDrag,
                    OnDrag = RectDrag
                };
                clickAndDragCanvas = new ClickAndDragHandler(curveCanvas)
                {
                    clickFilter = (e) => e.MiddleButton == MouseButtonState.Pressed,
                    OnDragEnter = EnterDrag,
                    OnDrag = CanvasDrag
                };
            }
        }

        void OnSelectionItemAdded(string groupName, ISelectable selectable)
        {
            if(selectable is KeyframeTimelineEllipse ellipse)
            {
                curvePanel.Renderer.Add(ellipse.keyframe.Holder);
                curvePanel.ScaleToFit();
            }
        }

        void OnSelectionItemRemoved(string groupName, ISelectable selectable)
        {
            if(selectable is KeyframeTimelineEllipse ellipse)
            {
                curvePanel.Renderer.Remove(ellipse.keyframe.Holder);
            }
        }

        void RectClick(MouseButtonEventArgs e)
        {
            curvePanel.ScaleToFit();
        }

        void EnterDrag(ClickLogic.DragEventArgs e)
        {
            downRect = curvePanel.Bounds;
        }

        void RectDrag(ClickLogic.DragEventArgs e)
        {
            //canvas drag is exactly the movement of the mouse, 
            //whereas rect drag is relative to the size of the currently display area
            Vector deltaStart = e.DeltaStart;
            Vector unit = new Vector(
                Math.Abs(downRect.Right - downRect.Left),
                Math.Abs(downRect.Top - downRect.Bottom)) * 0.01;
            Vector moveBy = new Vector(deltaStart.X * unit.X, deltaStart.Y * unit.Y);

            if (e.currentEvent.LeftButton == MouseButtonState.Pressed)
            {
                moveBy.Y *= -1;
                double left = downRect.Left + moveBy.X;
                double right = downRect.Right - moveBy.X;
                double top = downRect.Top + moveBy.Y;
                double bottom = downRect.Bottom - moveBy.Y;
                curvePanel.SetBounds(left, right, top, bottom);
            }
            if (e.currentEvent.MiddleButton == MouseButtonState.Pressed)
            {
                double left = downRect.Left - moveBy.X;
                double right = downRect.Right - moveBy.X;
                double top = downRect.Top - moveBy.Y;
                double bottom = downRect.Bottom - moveBy.Y;
                curvePanel.SetBounds(left, right, top, bottom);
            }
        }

        void CanvasDrag(ClickLogic.DragEventArgs e)
        {
            //canvas drag is exactly the movement of the mouse, 
            //whereas rect drag is relative to the size of the currently display area
            Point moveBy = curvePanel.Canvas2Keyframe((Point)e.DeltaStart);

            double left = downRect.Left - moveBy.X;
            double right = downRect.Right - moveBy.X;
            double top = downRect.Top - moveBy.Y;
            double bottom = downRect.Bottom - moveBy.Y;
            curvePanel.SetBounds(left, right, top, bottom);
        }
    }
}
