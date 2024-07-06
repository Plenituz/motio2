using Motio.Debuging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Motio.GLWrapper
{
    /// <summary>
    /// get the result from the opengl render and put it on a bitmap using multithreading and stuff
    /// </summary>
    public class OpenGLRenderer
    {
        public readonly WriteableBitmap backBuffer;
        public readonly int width;
        public readonly int height;
        public bool Ready { get; private set; } = false;
        /// <summary>
        /// event triggered in the background thread where you do all the opengl rendering
        /// </summary>
        public event Action DoPaint;
        /// <summary>
        /// event triggered in the brackgroudn thread where you can init all the opengl stuff
        /// </summary>
        public event Action DoInit;
        /// <summary>
        /// event called in the UI thread when the image is updated
        /// </summary>
        public event Action<WriteableBitmap> ImageUpdated;

        private event Action WorkQueue;

        private bool paint = false;

        private Thread workerThread;
        private GLControl glControl;
        private byte[] readBuf;
        /// <summary>
        /// used to wait until a paint request is sent
        /// </summary>
        AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        public OpenGLRenderer(int width, int height)
        {
            this.backBuffer = new WriteableBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Pbgra32,
                BitmapPalettes.WebPalette);
            this.width = width;
            this.height = height;
        }

        public void Start()
        {
            workerThread = new Thread(new ThreadStart(Starter))
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            workerThread.Start();
        }

        private void Starter()
        {
            Init();
            BackgroundWork();
        }

        private  void Init()
        {
            //most of this I got from
            //https://github.com/freakinpenguin/OpenTK-WPF/blob/master/OpenGLviaFramebuffer/FrameBufferHandler.cs
            var flags = GraphicsContextFlags.Default;
            glControl = new GLControl(new GraphicsMode(DisplayDevice.Default.BitsPerPixel, 16, 0, 4, 0, 2, false), 3, 3, flags);
            glControl.MakeCurrent();

            int frameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);

            int colorBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, colorBuffer);
            GL.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                RenderbufferStorage.Rgba8,
                width,
                height);
            GL.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                RenderbufferTarget.Renderbuffer,
                colorBuffer);

            int depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                RenderbufferStorage.DepthComponent24,
                width,
                height);
            GL.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer,
                depthBuffer);

            FramebufferErrorCode error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception("error creating frame buffer");
            }

            readBuf = new byte[width * height * 4];

            DoInit?.Invoke();

            Ready = true;
        }

        private void BackgroundWork()
        {
            while (true)
            {
                autoResetEvent.WaitOne();
                SetContext();

                WorkQueue?.Invoke();
                WorkQueue = null;
                if (paint)
                {
                    DoPaint?.Invoke();
                    paint = false;
                    ErrorCode error = GL.GetError();
                    if (error != ErrorCode.NoError)
                        Logger.WriteLine("error rendering:" + error);
                    ReadPixels();
                    UpdateImage();
                }
            }
        }

        /// <summary>
        /// call this from ui thread to tell the render thread to update the image
        /// </summary>
        public void RequestPaint()
        {
            paint = true;
            autoResetEvent.Set();
        }

        public void ReadPixels()
        {
            //readPixels probably is just waiting for a while before giving me data se this:
            //https://stackoverflow.com/questions/25127751/opengl-read-pixels-faster-than-glreadpixels
            GL.Finish();
            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, readBuf);
        }

        public void UpdateImage()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //TODO do the pixel copying in the background thread
                //https://stackoverflow.com/questions/9868929/how-to-edit-a-writablebitmap-backbuffer-in-non-ui-thread
                //http://www.andyrace.com/2014/06/manipulating-writeablebitmaps-back.html
                backBuffer.Lock();
                Int32Rect src = new Int32Rect(0, 0, (int)backBuffer.Width, 1);
                for (int y = 0; y < (int)backBuffer.Height; y++)
                {
                    src.Y = (int)backBuffer.Height - y - 1;
                    backBuffer.WritePixels(src, readBuf, backBuffer.BackBufferStride, 0, y);
                }
                backBuffer.AddDirtyRect(new Int32Rect(0, 0, (int)backBuffer.Width, (int)backBuffer.Height));
                backBuffer.Unlock();

                ImageUpdated?.Invoke(backBuffer);
            });
        }

        public void SetContext()
        {
            if (GraphicsContext.CurrentContext != glControl.Context)
                glControl.MakeCurrent();
        }

        public void RunInThread(Action action)
        {
            WorkQueue += action;
            autoResetEvent.Set();
        }
    }
}
