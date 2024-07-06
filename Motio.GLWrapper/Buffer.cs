using Motio.Geometry;
using Motio.Meshing;
using OpenTK.Graphics.OpenGL;


namespace Motio.GLWrapper
{
    public class BufferObject
    {
        public int ID { get; private set; }
        public BufferTarget Type { get; private set; }

        public BufferObject(BufferTarget type)
        {
            this.Type = type;
            ID = GL.GenBuffer();
        }

        public void SetData(int[] data, BufferUsageHint usage)
        {
            Use();
            GL.BufferData(Type, data.Length * sizeof(int), data, usage);
        }

        public void SetData(float[] data, BufferUsageHint usage)
        {
            Use();
            GL.BufferData(Type, data.Length * sizeof(float), data, usage);
        }

        public void SetData(Vector3[] data, BufferUsageHint usage)
        {
            Use();
            //we know Motio.Geometry.Vector3 has 3 float member so 
            //it's size is sizeof(float)*3
            GL.BufferData(Type, data.Length * sizeof(float) * 3, data, usage);
        }

        public void SetData(Vertex[] data, BufferUsageHint usage)
        {
            Use();
            //we know Motio.Geometry.Vector2 has 2 float member so it's size if sizeof(float)*2
            int sizeV2 = sizeof(float) * 2;
            int sizeV4 = sizeof(float) * 4;
            //              positon +  color + normal + uv
            int sizeVertex = sizeV2 + sizeV4 + sizeV2 + sizeV2;
            GL.BufferData(Type, data.Length * sizeVertex, data, usage);
        }

        public void Use()
        {
            GL.BindBuffer(Type, ID);
        }
    }
}
