using System.Collections.Generic;

namespace Motio.GLWrapper.Shading
{
    /// <summary>
    /// part of a compound shader
    /// </summary>
    public class ShaderChunk
    {
        public IDictionary<string, Uniform> necessaryUniforms = new Dictionary<string, Uniform>();
        public string code;

        public ShaderChunk(string code, IDictionary<string, Uniform> uniforms)
        {
            this.code = code;
            this.necessaryUniforms = uniforms;
        }
    }
}
