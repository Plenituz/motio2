using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Motio.GLWrapper.Shading
{
    public class ShaderBank
    {
        private readonly Dictionary<string, Shader> compiledShaders = new Dictionary<string, Shader>();
        private readonly Dictionary<string, ShaderProgram> linkedPrograms = new Dictionary<string, ShaderProgram>();

        public void AddCompiledShader(string name, Shader shader)
        {
            compiledShaders.Add(name, shader);
        }

        public ShaderProgram GetOrMakeProgram(string name)
        {
            if (!linkedPrograms.TryGetValue(name, out ShaderProgram program))
            {
                string[] split = name.Split(new char[] { '_' }, 2);
                if (split.Length != 2)
                    throw new System.Exception("given name is not in format [vertex]_[fragment]");
                string vertexName = split[0];
                string fragmentName = split[1];

                if (!compiledShaders.TryGetValue(vertexName, out Shader vertexShader))
                    throw new System.Exception("couldn't find vertex shader " + vertexName);
                if (!compiledShaders.TryGetValue(fragmentName, out Shader fragmentShader))
                    throw new System.Exception("couldn't find fragment shader " + fragmentName);

                program = new ShaderProgram();
                program.Attach(vertexShader);
                program.Attach(fragmentShader);
                string info = program.Link();
                if (!string.IsNullOrEmpty(info))
                    throw new System.Exception("error linking shader program " + name + ": \n" + info);
                linkedPrograms.Add(name, program);

            }
            return program;
        }
    }
}
