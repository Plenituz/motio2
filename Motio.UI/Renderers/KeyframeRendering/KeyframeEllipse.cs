using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using Motio.Selecting;
using Motio.Animation;
using Motio.UICommon;
using System.Windows.Input;

namespace Motio.UI.Renderers.KeyframeRendering
{
    /// <summary>
    /// this class has been created to allow an overall VisualTree HitTest to find 
    /// ISelectable objects that are in the tree
    /// otherwise it's just an ellipse that relays the ISelectable method calls to an handler
    /// </summary>
    public abstract class KeyframeEllipse : Shape, ISelectable
    {
        /// <summary>
        /// draw a simple sphere centered 
        /// </summary>
        protected override System.Windows.Media.Geometry DefiningGeometry
        {
            get
            {
                return new EllipseGeometry(new System.Windows.Point(0, 0), Width/2, Height/2);
            }
        }
        public KeyframeFloat keyframe;

        public KeyframeEllipse(KeyframeFloat keyframe)
        {
            this.keyframe = keyframe;
            Fill = (Brush)Application.Current.Resources["KeyframeColor"];
            Width = 10;
            Height = 10;
        }

        internal virtual void AddToCanvas(Canvas canvas)
        {
            canvas.Children.Add(this);
        }

        internal virtual void RemoveFromCanvas(Canvas canvas)
        {
            canvas.Children.Remove(this);
        }

        public virtual void UpdateToolTip()
        {
            ToolTip = "Time=" + keyframe.Time + "\n"
                    + "Value=" + keyframe.Value;
        }


        /// <summary>
        /// <paramref name="visualSpacePos"/> is the pos of the parent in visualSpace
        /// </summary>
        /// <param name="visualSpacePos"></param>
        public virtual void Update(System.Windows.Point visualSpacePos, SimpleRect bounds, Canvas canvas)
        {
            UpdateToolTip();
        }

        /// <summary>
        /// do something when the keyframeEllipse gets deleted
        /// </summary>
        public abstract bool Delete();

        public bool CanBeSelected => true;
        public abstract string DefaultSelectionGroup { get; }
        public void OnSelect() => Fill = (Brush)Application.Current.Resources["KeyframeSelectedColor"];
        public void OnUnselect() => Fill = (Brush)Application.Current.Resources["KeyframeColor"];

        public void KeyPressed(KeyEventArgs e)
        {
        }
    }
}
