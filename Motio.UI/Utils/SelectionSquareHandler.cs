using Motio.Debuging;
using Motio.Selecting;
using Motio.UI.ViewModels;
using Motio.UICommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Motio.UI.Utils
{
    public class SelectionSquareHandler
    {
        private UIElement container;
        /// <summary>
        /// stores the position of the mouse on down
        /// </summary>
        private Point windowMouseDownPos;
        private Point containerMouseDownPos;
        /// <summary>
        /// if this is false, on mouse up nothing should be done
        /// other wise on mouse move draw a rect from start to move
        /// and on mouse up select it's content
        /// </summary>
        private bool drawingSelectionRect = false;
        /// <summary>
        /// say weither or not the mouse moved in between down and up
        /// </summary>
        private bool didMove = false;
        /// <summary>
        /// selection saved on down 
        /// </summary>
        private Dictionary<ISelectable, string> tmpSelection;
        /// <summary>
        /// if this is not null, only the *hyper groups* in this list can be deselected 
        /// </summary>
        private string[] canOnlyDeselect = null;
        /// <summary>
        /// the graphics representation of the selection square
        /// </summary>
        SelectionRect selectionRect = new SelectionRect();
        public event Action<MouseButtonEventArgs> OnClick;
        /// <summary>
        /// only element in this hashset can send events to this handler
        /// </summary>
        public HashSet<object> senderWhiteList = new HashSet<object>();
        public bool clearOnDown = true;


        MainControlViewModel mainViewModel;
        IInputElement mainWindow;

        public SelectionSquareHandler(UIElement container)
        {
            this.container = container;

            Window window = Window.GetWindow(container);
            mainViewModel = (MainControlViewModel)window.DataContext;
            this.mainWindow = window;
        }

        public void Hook(UIElement element)
        {
            senderWhiteList.Add(element);
            element.MouseDown += MainWindow_MouseDown_SelectionRect;
            element.MouseMove += MainWindow_MouseMove_SelectionRect;
            element.MouseUp += MainWindow_MouseUp_SelectionRect;
        }

        public void UnHook(UIElement element)
        {
            senderWhiteList.Remove(element);
            element.MouseDown -= MainWindow_MouseDown_SelectionRect;
            element.MouseMove -= MainWindow_MouseMove_SelectionRect;
            element.MouseUp -= MainWindow_MouseUp_SelectionRect;
        }

        private ISelectionClearer ClearSelectionGroupUnderMouse(Point position)
        {
            //the hit test is done over the window not the canvas, which doesn't have children
            //most of the time
            ISelectionClearer clearer = null;
            VisualTreeHelper.HitTest(container, null,
                (o) =>
                {
                    if (o.VisualHit is ISelectionClearer c)
                    {
                        clearer = c;
                        return HitTestResultBehavior.Stop;
                    }
                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(position));
            return clearer;
        }

        public void MainWindow_MouseDown_SelectionRect(object sender, MouseButtonEventArgs e)
        {
            if (!senderWhiteList.Contains(sender))
                return;

            //clear focus when you click in the void of the window
            try
            {
                DependencyObject scope = FocusManager.GetFocusScope((DependencyObject)Keyboard.FocusedElement);
                FocusManager.SetFocusedElement(scope, container);
            }
            catch (Exception)
            {
                Logger.WriteLine("error clearing focus in MainWindow");
            }

            //only do stuff when the left button if pressed
            if (e.LeftButton == MouseButtonState.Released)
                return;
            //we save the position relative to the window to display the rect 
            //and the position relative to the container to calculate the actual selection square
            windowMouseDownPos = e.GetPosition(mainWindow);
            containerMouseDownPos = e.GetPosition(container);
            didMove = false;

            //detect if there is a selectable directly below the mouse, if so don't interfere with 
            //the selection process
            //and if the mouse is already captured by another element, don't interfere either
            //otherwise start drawing the selection rect

            bool alreadyCleared = false;
            ISelectionClearer clearer = Mouse.DirectlyOver as ISelectionClearer;//ClearSelectionGroupUnderMouse(e.GetPosition(container));
            canOnlyDeselect = null;

            if (clearer != null && !ToolBox.IsShiftHeld())
            {
                canOnlyDeselect = clearer.HyperGroupsToClear;
                for (int i = 0; i < canOnlyDeselect.Length; i++)
                {
                    Selection.ClearHyperGroup(canOnlyDeselect[i]);
                }
                alreadyCleared = true;
            }

            drawingSelectionRect = Mouse.Captured == null;

            //capture the mouse so that when you let go of the mouse 
            //on top of another element, the up gets called here and not on the element
            if (drawingSelectionRect)
            {
                //if shift is not held, clear selection before
                //if it is, lock the current selection to not loose it,
                //we will be unlocking it on mouse up
                if (ToolBox.IsShiftHeld())
                {
                    tmpSelection = Selection.All().ToDictionary(p => p.Value, p => p.Key);
                }
                else
                {
                    if (!alreadyCleared && clearOnDown)
                        Selection.ClearAll();
                }
                //Calling capturemouse() may trigger the move event
                //so we have to capture the mous elast thing
                container.CaptureMouse();
            }
        }

        public void MainWindow_MouseMove_SelectionRect(object sender, MouseEventArgs e)
        {
            if (!senderWhiteList.Contains(sender))
                return;
            //only do stuff when the left button if pressed
            if (e.LeftButton == MouseButtonState.Released)
                return;
            if (!drawingSelectionRect)
            {
                HideSelectionSquare();
            }
            else
            {
                Point mouseContainerPos = e.GetPosition(container);
                Point mouseWindowPos = e.GetPosition(mainWindow);
                Vector diffContainer = mouseContainerPos - containerMouseDownPos;

                if (!didMove)
                {
                    //calculate the distance to the mouse down point and if it's more than 1, 
                    //note everything is squared for performance reasons 
                    //concidere the mouse moved
                    double distanceSquared = diffContainer.X * diffContainer.X + diffContainer.Y * diffContainer.Y;
                    if (distanceSquared > 2)
                        didMove = true;
                }
                if (!didMove)
                    return;
                //scale the square and stuff
                AdaptSelectionSquare(new Rect(windowMouseDownPos, mouseWindowPos));

                //make sure the rect is visible and added to the canvas
                ShowSelectionSquare();

                //this is a hit test over a rectangular area, represented by the selection rect
                HashSet<ISelectable> objectsInArea = new HashSet<ISelectable>();
                RectangleGeometry hitArea = new RectangleGeometry(new Rect(containerMouseDownPos, e.GetPosition(container)));
                try
                {
                    VisualTreeHelper.HitTest(
                        container, null,
                        (t) => HitTestResultCallback(t, objectsInArea),
                        new GeometryHitTestParameters(hitArea)
                        );
                    HandleSelectionRectObjs(objectsInArea);
                }
                catch (NotSupportedException)
                {
                    //TODO this is not a solution 
                    //pb : you can't geometry hittest over the 3D viewport, because it's a 3D environment not a 2D
                    //https://stackoverflow.com/questions/4428853/wpf3d-rectangle-hit-test
                    //see comment on the given answer (not a vaillable answer)
                    MainWindow_MouseUp_SelectionRect(sender,
                        new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
                    Logger.WriteLine("error select");
                    return;
                }
            }
        }

        public void MainWindow_MouseUp_SelectionRect(object sender, MouseButtonEventArgs e)
        {
            if (!senderWhiteList.Contains(sender))
                return;
            //always release mouse, you never know 
            container.ReleaseMouseCapture();
            tmpSelection = null;

            //if we didn't draw a selection rect it means
            //we shouldn't interfere with an ongoing selection process
            if (drawingSelectionRect)
            {
                drawingSelectionRect = false;
                HideSelectionSquare();

                //if the mouse did not move in between down and up events
                //clear the selection, the user just clicked in the void
                if (!didMove)
                {
                    e.Handled = false;
                    OnClick?.Invoke(e);
                    if (!e.Handled)
                    {
                        e.Handled = true;
                        ISelectionClearer clearer = ClearSelectionGroupUnderMouse(e.GetPosition(container));
                        if (clearer != null)
                        {
                            string[] hyperGroups = clearer.HyperGroupsToClear;
                            for (int i = 0; i < hyperGroups.Length; i++)
                            {
                                Selection.ClearHyperGroup(hyperGroups[i]);
                            }
                        }
                    }
                    return;
                }
            }
        }

        private void HandleSelectionRectObjs(HashSet<ISelectable> objectsInArea)
        {
            //selection logic
            foreach (ISelectable selectable in objectsInArea)
            {
                if (tmpSelection != null && tmpSelection.ContainsKey(selectable))
                {
                    //si il etait selectionné avant le debut du rect
                    //deselectionne le 
                    Selection.Remove(tmpSelection[selectable], selectable);
                }
                else
                {
                    //si il était pas selectionné avant, selectionne le 
                    Selection.Add(selectable.DefaultSelectionGroup, selectable);
                }
            }
            if (tmpSelection != null)
            {
                //pour tous les elements qui sont dans tmpSelection et pas dans objecstInArea,
                //les rajouter a la selection
                foreach(var pair in tmpSelection.Where(o => !objectsInArea.Contains(o.Key)))
                    Selection.Add(pair.Value, pair.Key);

                //pour tous les objects qui sont dans la selections 
                //current mais pas dans l'area et pas dans tmpselection, les deselectionner
                var enumerable = Selection.All().Where(o => !objectsInArea.Contains(o.Value) && !tmpSelection.ContainsKey(o.Value));
                foreach (var pair in enumerable)
                    Selection.Remove(pair.Key, pair.Value);
            }
            else
            {
                //pour tous les objects selectionnés qui sont pas dans la zone, on les deselectionnes
                var enumerable = Selection.All().Where(o => !objectsInArea.Contains(o.Value)
                            && (canOnlyDeselect == null ? true : canOnlyDeselect.Any(hypergroup => o.Key.Contains(hypergroup))));
                foreach (var pair in enumerable)
                    Selection.Remove(pair.Key, pair.Value);
            }
        }

        private void AdaptSelectionSquare(Rect rect)
        {
            //proper invalidate : https://stackoverflow.com/questions/5458802/how-to-properly-refresh-a-custom-shape-in-wpf
            //[SCOTCH] but hey it works
            rect.Offset(0, -mainViewModel.MenuQuiDecaleTout.ActualHeight);
            selectionRect.geometry = new RectangleGeometry(rect);
            selectionRect.InvalidateVisual();
        }


        private void HideSelectionSquare()
        {
            if (selectionRect.Visibility != Visibility.Collapsed)
                selectionRect.Visibility = Visibility.Collapsed;
        }

        private void ShowSelectionSquare()
        {
            if (selectionRect.Visibility != Visibility.Visible)
                selectionRect.Visibility = Visibility.Visible;
            if (!mainViewModel.CanvasOverEverything.Children.Contains(selectionRect))
                mainViewModel.CanvasOverEverything.Children.Add(selectionRect);
        }

        private HitTestResultBehavior HitTestResultCallback(HitTestResult hitTestResult,
            HashSet<ISelectable> selectableInArea)
        {
            IntersectionDetail idDetail = ((GeometryHitTestResult)hitTestResult).IntersectionDetail;
            if (hitTestResult.VisualHit is ISelectable selectable)
            {
                selectableInArea.Add(selectable);

            }
            return HitTestResultBehavior.Continue;
        }
    }
}
