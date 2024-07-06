using Motio.Meshing;

namespace Motio.Rendering
{
    public interface IRenderable
    {
        MeshGroup GetMeshes(int frame);
        bool Visible { get; }
    }
}
