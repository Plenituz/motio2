using Motio.Renderers.BezierRendering;
using Motio.Selecting;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Motio.UI.Renderers.KeyframeCurveRendering
{
    public class KeyframeCurvePathTangent : BezierVisualItem
    {
        protected override Brush Color => (Brush)Application.Current.Resources["TangentColor"];
        protected override Brush SelectedColor => (Brush)Application.Current.Resources["TangentSelectedColor"];
        public override string DefaultSelectionGroup => Selection.KEYFRAME_CURVES_TANGENTS;

        /// <summary>
        /// empty constructor to satify constaints
        /// </summary>
        public KeyframeCurvePathTangent() : base(null)
        {
        }

        public KeyframeCurvePathTangent(IBezierPoint point) : base(point)
        {
        }

        public override void KeyPressed(KeyEventArgs e)
        {
        }
    }
}
