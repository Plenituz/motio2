using System.Runtime.InteropServices;

namespace Motio.Geometry
{
    /// <summary>
    /// a struct representing a point in a mesh.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector2 position;
        public Vector4 color;
        public Vector2 normal;
        public Vector2 uv;

        public Vertex(float x, float y) : this(new Vector2(x, y))
        {

        }

        public Vertex(Vector2 position) 
            : this(position, new Vector4(0.86f, 0.62f, 0.86f, 1f), Vector2.Zero, position)
        {
        }

        public Vertex(Vector2 position, Vector4 color) : this(position, color, Vector2.Zero, position)
        {
        }

        public Vertex(Vector2 position, Vector4 color, Vector2 normal, Vector2 uv)
        {
            this.position = position;
            this.color = color;
            this.normal = normal;
            this.uv = uv;
        }

        public void SetPos(Vector2 position) => this.position = position;
        public void SetColor(Vector4 color) => this.color = color;
        public void SetNormal(Vector2 normal) => this.normal = normal;
        public void SetUv(Vector2 uv) => this.uv = uv;
    }
}
