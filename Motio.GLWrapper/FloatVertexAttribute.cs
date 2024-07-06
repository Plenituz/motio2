using OpenTK.Graphics.OpenGL;
using System;

namespace Motio.GLWrapper
{
    public class FloatVertexAttribute
    {
        public int? index;
        public int? size;
        public bool? normalized;
        public int? stride;
        public int? offset;

        public void Set()
        {
            if (!index.HasValue)
                throw new ArgumentNullException(nameof(index));
            if (!size.HasValue)
                throw new ArgumentNullException(nameof(size));
            if (!normalized.HasValue)
                throw new ArgumentNullException(nameof(normalized));
            if (!stride.HasValue)
                throw new ArgumentNullException(nameof(stride));
            if (!offset.HasValue)
                throw new ArgumentNullException(nameof(offset));

            GL.VertexAttribPointer(
                index.Value,
                size.Value,
                VertexAttribPointerType.Float,
                normalized.Value,
                stride.Value * sizeof(float),
                offset.Value * sizeof(float));
            //GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        }

        public void Use()
        {
            if (!index.HasValue)
                throw new ArgumentNullException(nameof(index));
            GL.EnableVertexAttribArray(index.Value);
        }
    }
}
