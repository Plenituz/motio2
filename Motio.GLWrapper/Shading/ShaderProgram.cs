using Motio.Debuging;
using Motio.Geometry;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Motio.GLWrapper.Shading
{
    public class ShaderProgram
    {
        public int ID { get; private set; }

        private Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

        public ShaderProgram()
        {
            ID = GL.CreateProgram();
        }

        /// <summary>
        /// attach the 2 shaders, link the program and delete the shaders
        /// </summary>
        /// <param name="vertexShader"></param>
        /// <param name="fragmentShader"></param>
        /// <returns></returns>
        public string AttachLinkClean(Shader vertexShader, Shader fragmentShader)
        {
            Attach(vertexShader);
            Attach(fragmentShader);
            string error = Link();
            if (!string.IsNullOrEmpty(error))
                return error;

            vertexShader.Delete();
            fragmentShader.Delete();
            return error;//will be null or empty 
        }

        public void Attach(Shader shader)
        {
            GL.AttachShader(ID, shader.ID);
        }

        public string Link()
        {
            GL.LinkProgram(ID);
            GL.GetProgramInfoLog(ID, out string info);
            return info;
        }

        public void Use()
        {
            GL.UseProgram(ID);
        }

        public int GetUniformLocation(string name)
        {
            if (!uniformLocations.TryGetValue(name, out int location))
            {
                location = GL.GetUniformLocation(ID, name);
                if (location == -1)
                {
                    //Logger.WriteLine("uniform " + name + " unused or not declared");
                    return location;
                }

                uniformLocations.Add(name, location);
            }
            return location;
        }

        public void SetUniform(string name, int value, out int location)
        {
            location = GetUniformLocation(name);
            if (location == -1)
                return;
            SetUniform(location, value);
        }

        public void SetUniform(string name, Matrix value, out int location)
        {
            location = GetUniformLocation(name);
            if (location == -1)
                return;
            SetUniform(location, value);
        }

        public void SetUniform(int location, int value)
        {
            Use();
            GL.Uniform1(location, value);
        }

        public void SetUniform(int location, Matrix matrix, bool transpose = true)
        {
            Use();
            
            OpenTK.Matrix4 convert = new OpenTK.Matrix4(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44);

            GL.UniformMatrix4(location, transpose, ref convert);
        }
    }
}
