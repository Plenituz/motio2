using System.Windows;
using System.Windows.Input;

namespace Motio.ClickLogic
{
    public class DragEventArgs
    {
        /// <summary>
        /// the event that started the drag
        /// </summary>
        public MouseButtonEventArgs startEvent;
        /// <summary>
        /// the event created by the previous click
        /// </summary>
        public MouseEventArgs lastEvent;
        /// <summary>
        /// the event that was created by the current click
        /// </summary>
        public MouseEventArgs currentEvent;
        public Point startPos;
        /// <summary>
        /// the click position from the last event relative to the clicked item
        /// </summary>
        public Point lastPos;
        /// <summary>
        /// the current click position relative to the clicked item
        /// </summary>
        public Point currentPos;
        /// <summary>
        /// difference of position from the first event to the current event
        /// </summary>
        public Vector DeltaStart => currentPos - startPos;
        /// <summary>
        /// difference of position from the previous event to the current event
        /// </summary>
        public Vector DeltaLast => currentPos - lastPos;
    }
}
