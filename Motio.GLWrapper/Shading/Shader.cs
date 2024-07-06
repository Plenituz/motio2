using OpenTK.Graphics.OpenGL;
using System.IO;

namespace Motio.GLWrapper.Shading
{
    public class Shader
    {
        public int ID { get; private set; }
        public bool Compiled { get; private set; } = false;
        public bool Deleted { get; private set; } = false;

        public Shader(ShaderType type)
        {
            ID = GL.CreateShader((OpenTK.Graphics.OpenGL.ShaderType)type);
        }

        public Shader(ShaderType type, string src, bool srcIsFile = false) : this(type)
        {
            if (srcIsFile)
                SetSrcFromFile(src);
            else
                SetSrc(src);
        }

        public void SetSrc(string src)
        {
            GL.ShaderSource(ID, src);
        }

        public void SetSrcFromFile(string path)
        {
            SetSrc(File.ReadAllText(path));
        }

        /// <summary>
        /// return the info log result of the compilation
        /// </summary>
        /// <returns></returns>
        public string Compile()
        {
            GL.CompileShader(ID);
            GL.GetShaderInfoLog(ID, out string info);
            if (string.IsNullOrEmpty(info))
                Compiled = true;
            return info;
        }

        public void Delete()
        {
            GL.DeleteShader(ID);
            Deleted = true;
        }
    }
}
