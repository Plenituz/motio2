using Motio.Debuging;
using Motio.Geometry;
using Motio.GLWrapper;
using Motio.GLWrapper.Shading;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;
using Motio.Meshing;

namespace Motio.Rendering
{
    public class MotioRenderer
    {
        private readonly OpenGLRenderer renderer;
        private readonly ObjectPool<MeshModel> modelPool;
        private readonly ShaderBank shaderBank = new ShaderBank();
        private Matrix camera;
        private IEnumerable<IRenderable> renderables;

        private float cameraWidth, cameraHeight, cameraNearPlane, cameraFarPlane;
        private int resolutionWidth, resolutionHeight;
        private Func<int> GetCurrentFrame;
        /// <summary>
        /// event called in the UI thread when the image is updated
        /// </summary>
        public event Action<WriteableBitmap> ImageUpdated;

        public MotioRenderer(int resolutionWidth, int resolutionHeight,
            float cameraWidth, float cameraHeight, float cameraNearPlane, float cameraFarPlane,
            Func<int> getCurrentFrame)
        {
            this.cameraWidth = cameraWidth;
            this.cameraHeight = cameraHeight;
            this.cameraNearPlane = cameraNearPlane;
            this.cameraFarPlane = cameraFarPlane;
            this.resolutionWidth = resolutionWidth;
            this.resolutionHeight = resolutionHeight;
            this.GetCurrentFrame = getCurrentFrame;
            modelPool = new ObjectPool<MeshModel>(() => new MeshModel(shaderBank));


            renderer = new OpenGLRenderer(resolutionWidth, resolutionHeight);
            renderer.DoInit += Init;
            renderer.DoPaint += Paint;
            renderer.ImageUpdated += CallImageUpdated;
            renderer.Start();
        }

        private void Init()
        {
            CompileShaders();
            SetupCamera();
            SetupOpenGL();
            //init models
            //update models
        }

        private void Paint()
        {
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(
                ClearBufferMask.ColorBufferBit |
                ClearBufferMask.DepthBufferBit |
                ClearBufferMask.StencilBufferBit);

            int currentFrame = GetCurrentFrame();
            List<Mesh> meshes = new List<Mesh>();

            foreach (IRenderable renderable in renderables)
            {
                if (!renderable.Visible)
                    continue;
                MeshGroup meshGroup = renderable.GetMeshes(currentFrame);
                if (meshGroup == null)
                    continue;
                for (int i = 0; i < meshGroup.Count; i++)
                {
                    meshes.Add(meshGroup[i]);
                }
            }
            if(meshes.Count == 0)
                return;
            meshes.Sort((m1, m2) => m1.zIndex.CompareTo(m2.zIndex));

            MeshModel[] models = new MeshModel[meshes.Count];
            float[] renderZIndexes = new float[meshes.Count];

            //for the ones that are on the same zindex, on les reparti entre zindex et zindex+1 comme ca ils sont 
            //quand meme transparents
            int group = meshes[0].zIndex;
            int groupStart = 0;
            int groupCount = 1;
            for(int i = 1; i < meshes.Count; i++)
            {
                if(meshes[i].zIndex == group)
                {
                    groupCount++;
                }
                else
                {
                    for(int k = groupStart; k < i; k++)
                    {
                        renderZIndexes[k] = group + ((k - groupStart) / (float)groupCount);
                    }

                    group = meshes[i].zIndex;
                    groupStart = i;
                    groupCount = 1;
                }
            }

            for(int i = groupStart; i < meshes.Count; i++)
            {
                renderZIndexes[i] = group + ((i - groupStart) / (float)groupCount);
            }

            for (int i = 0; i < meshes.Count; i++)
            {
                MeshModel model = modelPool.GetObject();
                models[i] = model;
                meshes[i].ApplyConditions(renderZIndexes[i]);
                model.UpdateBufferAndRender(meshes[i], currentFrame, camera);
            }

            for (int i = 0; i < models.Length; i++)
            {
                modelPool.PutObject(models[i]);
            }
            
        }

        private void CallImageUpdated(WriteableBitmap bitmap)
        {
            ImageUpdated?.Invoke(bitmap);
        }

        private void SetupOpenGL()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.DepthTest);
            GL.Viewport(0, 0, resolutionWidth, resolutionHeight);
        }

        private void SetupCamera()
        {
            float widthOver2 = cameraWidth / 2;
            float heightOver2 = cameraHeight / 2;
            camera = Matrix.CreateOrthographic2(-widthOver2, widthOver2, heightOver2, -heightOver2, cameraNearPlane, cameraFarPlane);
        }

        private void CompileShaders()
        {
            foreach (string f in Directory.GetFiles("Shaders"))
            {
                string file = Path.GetFileName(f);
                ShaderType type;
                if (file.StartsWith("vertex"))
                {
                    type = ShaderType.VertexShader;
                }
                else if (file.StartsWith("fragment"))
                {
                    type = ShaderType.FragmentShader;
                }
                else
                {
                    Logger.WriteLine("shader " + file + " doesn't start with 'fragment' or 'vertex'");
                    continue;
                }
                Shader shader = new Shader(type, Path.Combine("Shaders", file), true);
                string info = shader.Compile();
                if (!string.IsNullOrEmpty(info))
                {
                    Logger.WriteLine("error compiling shader " + file + ": \n" + info);
                    continue;
                }

                string[] split = Path.GetFileNameWithoutExtension(file).Split(new char[] { '_' }, 2);
                if (split.Length != 2)
                    Logger.WriteLine("shader name " + file + "is not in format [fragment|vertex]_[name]");
                string name = split[1];

                shaderBank.AddCompiledShader(name, shader);
            }
        }

        public void RequestPaint(IEnumerable<IRenderable> renderables)
        {
            this.renderables = renderables;
            renderer.RequestPaint();
        }
    }
}
