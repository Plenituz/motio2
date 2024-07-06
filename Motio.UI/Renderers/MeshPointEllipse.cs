using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Motio.Meshing;
using Motio.Renderers;
using Motio.Selecting;

namespace Motio.UI.Renderers
{
    public class MeshPointEllipse : CenteredEllipse, ISelectable
    {
        public Mesh mesh;
        public int pIndex;

        public MeshPointEllipse(Mesh mesh, int pIndex)
        {
            this.mesh = mesh;
            this.pIndex = pIndex;
            Fill = (Brush)Application.Current.Resources["CurvePointColor"];
        }

        public bool CanBeSelected => true;
        public string DefaultSelectionGroup => Selection.MESH_POINTS;

        public bool Delete()
        {
            throw new System.NotImplementedException("can't delete mesh points yet");
        }

        public void KeyPressed(KeyEventArgs e){}

        public void OnSelect()
        {
            Fill = (Brush)Application.Current.Resources["CurvePointSelectedColor"];
        }

        public void OnUnselect()
        {
            Fill = (Brush)Application.Current.Resources["CurvePointColor"];
        }
    }
}
