using Motio.Animation;
using Motio.Renderers.BezierRendering;
using Motio.Selecting;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Motio.UI.Renderers.KeyframeCurveRendering
{
    class KeyframeCurvePathPoint : BezierVisualItem
    {
        protected override Brush Color => (Brush)Application.Current.Resources["KeyframeColor"];
        protected override Brush SelectedColor => (Brush)Application.Current.Resources["KeyframeSelectedColor"];
        public override string DefaultSelectionGroup => Selection.KEYFRAME_CURVES;

        /// <summary>
        /// empty constructor to satify constaints
        /// </summary>
        public KeyframeCurvePathPoint() : base(null)
        {
        }

        public KeyframeCurvePathPoint(IBezierPoint point) : base(point)
        {
        }

        public override void KeyPressed(KeyEventArgs e)
        {
            KeyframeFloat p = (KeyframeFloat)point;
            switch (e.Key)
            {
                case Key.Z:
                    p.RightHandle *= 1.1;
                    p.LeftHandle *= 1.1;
                    break;
                case Key.E:
                    break;
            }
        }
    }
}
