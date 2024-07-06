using System;
using System.Windows;
using System.Windows.Input;

namespace Motio.ClickLogic
{
    public class ClickAndDragHandler
    {
        private IInputElement clicked;
        private Point downPos;
        private bool moved = false;
        private bool calledStart = false;
        private DragEventArgs dragEventArgs;

        /// <summary>
        /// public so you can checked what is hooked 
        /// </summary>
        public UIElement currentlyHooked;
        public Action<MouseButtonEventArgs> OnClick;
        public Action<object, MouseButtonEventArgs> OnClickWithSender;
        public Action<DragEventArgs> OnDragEnter;
        public Action<object, DragEventArgs> OnDragEnterWithSender;
        /// <summary>
        /// this is called on drag, the first argument the delta position 
        /// </summary>
        public Action<DragEventArgs> OnDrag;
        public Action<object, DragEventArgs> OnDragWithSender;
        public Action<DragEventArgs> OnDragEnd;
        public Action<object, DragEventArgs> OnDragEndWithSender;
        public Predicate<MouseButtonEventArgs> clickFilter;
        /// <summary>
        /// distance squared of drag allowed before the click is no longer concidered a click but a drag
        /// </summary>
        public double DragEpsilonSquared = 25;
        
        /// <summary>
        /// since only one element can be hooked at a time, we hook the given element for you
        /// </summary>
        /// <param name="element"></param>
        public ClickAndDragHandler(UIElement element, bool usePreview = false)
        {
            Hook(element, usePreview);
        }

        public void Hook(UIElement element, bool usePreview = false)
        {
            if (currentlyHooked != null)
                UnHook(currentlyHooked);
            currentlyHooked = element;
            if(usePreview)
            {
                element.PreviewMouseDown += MouseDown;
                element.PreviewMouseMove += MouseMove;
                element.PreviewMouseUp += MouseUp;
            }
            else
            {
                element.MouseDown += MouseDown;
                element.MouseMove += MouseMove;
                element.MouseUp += MouseUp;
            }
        }

        public void UnHook(UIElement element)
        {
            if (element != currentlyHooked)
                throw new ArgumentException("can't unhook what's not hooked");

            element.MouseDown -= MouseDown;
            element.MouseMove -= MouseMove;
            element.MouseUp -= MouseUp;
            element.PreviewMouseDown -= MouseDown;
            element.PreviewMouseMove -= MouseMove;
            element.PreviewMouseUp -= MouseUp;
            currentlyHooked = null;
        }

        private void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(Mouse.Captured == null && (clickFilter?.Invoke(e) ?? true) )
            {
                moved = false;
                clicked = (IInputElement)sender;
                downPos = e.GetPosition(clicked);
                dragEventArgs = new DragEventArgs()
                {
                    startEvent = e,
                    currentEvent = e,
                    startPos = downPos,
                    currentPos = downPos
                };

                clicked.CaptureMouse();
            }
        }

        private void MouseMove(object sender, MouseEventArgs e)
        {
            if(Mouse.Captured == clicked && sender == clicked)
            {
                Point currentPos = e.GetPosition(clicked);
                if (!moved && (downPos - currentPos).LengthSquared > DragEpsilonSquared)
                {
                    moved = true;
                    OnDragEnter?.Invoke(dragEventArgs);
                    OnDragEnterWithSender?.Invoke(sender, dragEventArgs);
                }
                if(moved)
                {
                    dragEventArgs.lastPos = dragEventArgs.currentPos;
                    dragEventArgs.lastEvent = dragEventArgs.currentEvent;
                    dragEventArgs.currentEvent = e;
                    dragEventArgs.currentPos = currentPos;
                    OnDrag?.Invoke(dragEventArgs);
                    OnDragWithSender?.Invoke(sender, dragEventArgs);
                }
            }
        }

        private void MouseUp(object sender, MouseButtonEventArgs e)
        {
            //no click filter on up, we always want to release
            if(Mouse.Captured == clicked && sender == clicked)
            {
                clicked.ReleaseMouseCapture();
                if (moved)
                {
                    Point currentPos = e.GetPosition(clicked);
                    dragEventArgs.currentEvent = e;
                    dragEventArgs.currentPos = currentPos;
                    OnDragEnd?.Invoke(dragEventArgs);
                    OnDragEndWithSender?.Invoke(sender, dragEventArgs);
                }
                else
                {
                    OnClick?.Invoke(e);
                    OnClickWithSender?.Invoke(sender, e);
                    clicked = null;
                }
            } 
        }
    }
}
