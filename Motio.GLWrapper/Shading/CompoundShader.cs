using System.Collections.Generic;
using System.Text;

namespace Motio.GLWrapper.Shading
{
    /// <summary>
    /// shader made of multiple modulable shader chunks,
    /// not implemented yet
    /// </summary>
    public class CompoundShader
    {
        private List<ShaderChunk> chunks = new List<ShaderChunk>();

        public void AddChunk(ShaderChunk chunk)
        {
            //don't check for uniform here, do it at the end only once to gain time
            chunks.Add(chunk);
        }

        public string BuildSource()
        {
            //list uniform, simplify if 2 are the same name/type if same name but different type throw
            Dictionary<string, Uniform> uniforms = new Dictionary<string, Uniform>();
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < chunks.Count; i++)
            {
                ShaderChunk chunk = chunks[i];
                builder.AppendLine(chunk.code);
                
                foreach(var pair in chunk.necessaryUniforms)
                {
                    if (!uniforms.TryGetValue(pair.Key, out Uniform uniform))
                    {
                        
                    }
                }
            }
            return "";
        }
    }
}
