using Motio.Geometry;
using Motio.GLWrapper.Shading;
using Motio.Meshing;
using OpenTK.Graphics.OpenGL;
using System.Linq;

namespace Motio.GLWrapper
{
    public class MeshModel
    {
        private readonly ShaderBank shaderBank;
        private readonly VertexAttributesObject VAO;
        private readonly BufferObject vertexBuffer;
        private readonly BufferObject triangleIndices;
        private int timeUniformLocation = -1;
        private int cameraUniformLocation = -1;
        private int transformUniformLocation = -1;

        public MeshModel(ShaderBank shaderBank)
        {
            this.shaderBank = shaderBank;
            VAO = new VertexAttributesObject();
            vertexBuffer = new BufferObject(BufferTarget.ArrayBuffer);
            triangleIndices = new BufferObject(BufferTarget.ElementArrayBuffer);

            VAO.Use();
            VAO.AddBuffer(vertexBuffer, false);
            VAO.AddBuffer(triangleIndices, false);
            FloatVertexAttribute positionAttrib = new FloatVertexAttribute()
            {
                normalized = false,
                offset = 0,
                size = 2,
                stride = 10
            };
            FloatVertexAttribute colorAttrib = new FloatVertexAttribute()
            {
                normalized = false,
                offset = 2,
                size = 4,
                stride = 10
            };
            FloatVertexAttribute normalAttrib = new FloatVertexAttribute()
            {
                normalized = false,
                offset = 6,
                size = 2,
                stride = 10
            };
            FloatVertexAttribute uvAttrib = new FloatVertexAttribute()
            {
                normalized = false,
                offset = 8,
                size = 2,
                stride = 10
            };
            VAO.SetVertexAttribute(0, positionAttrib, false);
            VAO.SetVertexAttribute(1, colorAttrib, false);
            VAO.SetVertexAttribute(2, normalAttrib, false);
            VAO.SetVertexAttribute(3, uvAttrib, false);

            GL.BindVertexArray(0);
        }

        public void UpdateBufferAndRender(Mesh mesh, int frame, Matrix camera)
        {
            UpdateBuffer(mesh);
            Render(mesh, frame, camera);
        }

        private void UpdateBuffer(Mesh mesh)
        {
            VAO.Use();
            vertexBuffer.SetData(mesh.vertices.ToArray(), BufferUsageHint.StreamDraw);
            triangleIndices.SetData(mesh.triangles.ToArray(), BufferUsageHint.StreamDraw);
            GL.BindVertexArray(0);
        }

        private void Render(Mesh mesh, int frame, Matrix camera)
        {
            ShaderProgram shaderProgram = shaderBank.GetOrMakeProgram(mesh.shader);
            VAO.Use();
            shaderProgram.Use();

            if (timeUniformLocation == -1)
                shaderProgram.SetUniform("frame", frame, out timeUniformLocation);
            else
                shaderProgram.SetUniform(timeUniformLocation, frame);

            if (cameraUniformLocation == -1)
                shaderProgram.SetUniform("camera", camera, out cameraUniformLocation);
            else
                shaderProgram.SetUniform(cameraUniformLocation, camera);

            if (transformUniformLocation == -1)
                shaderProgram.SetUniform("transform", mesh.transform, out transformUniformLocation);
            else
                shaderProgram.SetUniform(transformUniformLocation, mesh.transform, false);



            //Turn on wireframe mode
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            GL.DrawElements(PrimitiveType.Triangles, mesh.triangles.Count, DrawElementsType.UnsignedInt, 0);

            //Turn off wireframe mode
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        /// <summary>
        /// after calling this you can no longer call <see cref="Render"/> or <see cref="UpdateBuffer"/>
        /// or unexpected behaviour will occur
        /// </summary>
        public void Delete()
        {
            GL.BindVertexArray(0);
            GL.DeleteVertexArray(VAO.ID);
            GL.DeleteBuffer(vertexBuffer.ID);
            GL.DeleteBuffer(triangleIndices.ID);
        }
    }
}
