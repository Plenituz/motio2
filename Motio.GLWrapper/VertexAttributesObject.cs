using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Motio.GLWrapper
{
    /// <summary>
    /// store pointers to vertex attributes
    /// can also store Element buffer objects
    /// </summary>
    public class VertexAttributesObject
    {
        public int ID { get; private set; }

        public VertexAttributesObject()
        {
            ID = GL.GenVertexArray();
        }

        public void Use()
        {
            GL.BindVertexArray(ID);
        }

        /// <summary>
        /// set <paramref name="bind"/> to false if you are sure this VAO is already bound
        /// </summary>
        /// <param name="index"></param>
        /// <param name="attribute"></param>
        /// <param name="bind"></param>
        public void SetVertexAttribute(int index, FloatVertexAttribute attribute, bool bind = true)
        {
            if(bind)
                GL.BindVertexArray(ID);

            attribute.index = index;
            attribute.Set();
            attribute.Use();
        }

        public void AddBuffer(BufferObject buffer, bool bind = true)
        {
            if (bind)
                GL.BindVertexArray(ID);
            buffer.Use();
        }
    }
}
