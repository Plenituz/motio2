using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Motio.Renderers.BezierRendering;
using Motio.Selecting;

namespace Motio.UI.Renderers.PathRendering
{
    public class VisualPathTangent : BezierVisualItem
    {
        protected override Brush Color => (Brush)Application.Current.Resources["TangentColor"];
        protected override Brush SelectedColor => (Brush)Application.Current.Resources["TangentSelectedColor"];
        public override string DefaultSelectionGroup => Selection.PATH_TANGENTS;

        /// <summary>
        /// empty constructor to satify constaints
        /// </summary>
        public VisualPathTangent() : base(null)
        {
        }

        public VisualPathTangent(IBezierPoint point) : base(point)
        {
        }

        public override void KeyPressed(KeyEventArgs e)
        {

        }
    }
}
