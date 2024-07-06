using Motio.UICommon;
using System;
using System.Windows;

namespace Motio.ClickLogic
{
    public class ClickAndDragPropertyValue
    {
        public ClickAndDragHandler handler;
        public Func<float> GetValue;
        public Action<float> SetValue;
        public float sensitivity = 1;

        float dragStartValue;
        float multiplier = 1;

        public ClickAndDragPropertyValue(UIElement element)
        {
            handler = new ClickAndDragHandler(element)
            {
                OnDragEnter = DragStart,
                OnDrag = Drag,
                OnDragEnd = Drag
            };
        }

        private void DragStart(DragEventArgs e)
        {
            dragStartValue = GetValue();
        }

        private void Drag(DragEventArgs e)
        {
            //EVENT HIJACKING HANDS UP!
            //I use the fact that the DragEvent is the same object for a whole "click session" 
            //(from the onclickdown to the onclickup) so I can modify the startPos of the event 
            //it will stay until the next OnDrag event call
            if (ToolBox.IsShiftHeld() && multiplier != 10)
            {
                multiplier = 10;
                dragStartValue = GetValue();
                e.startPos = e.currentPos;
            }
            if (ToolBox.IsCtrlHeld() && multiplier != 0.1)
            {
                multiplier = 0.1f;
                dragStartValue = GetValue();
                e.startPos = e.currentPos;
            }
            if (!ToolBox.IsShiftHeld() && !ToolBox.IsCtrlHeld() && multiplier != 1)
            {
                multiplier = 1;
                dragStartValue = GetValue();
                e.startPos = e.currentPos;
            }
            SetValue(dragStartValue + (float)e.DeltaStart.X * multiplier * sensitivity);
            //property.StaticValue = ;
            //property.UpdateModel();
        }

    }
}
