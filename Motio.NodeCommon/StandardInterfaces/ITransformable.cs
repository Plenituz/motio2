using Motio.Geometry;

namespace Motio.NodeCommon.StandardInterfaces
{
    public interface ITransformable
    {
        void AppendTransform(Matrix matrix);
        void OverrideTransform(Matrix matrix);
    }
}
