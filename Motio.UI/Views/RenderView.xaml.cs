using Motio.Debuging;
using Motio.ClickLogic;
using Motio.Configuration;
using Motio.UI.Gizmos;
using Motio.UI.Utils;
using Motio.UI.ViewModels;
using Motio.UICommon;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using OpenTK.Graphics.OpenGL;
using System.Windows.Media.Imaging;
using Motio.GLWrapper;
using Motio.NodeCore;
using Motio.Geometry;
using System.IO;
using Motio.GLWrapper.Shading;
using System.Threading;
using System.Collections.Generic;
using Motio.Rendering;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for RenderView.xaml
    /// </summary>
    public partial class RenderView : UserControl
    {
        public AnimationTimelineViewModel Timeline { get; private set; }
        public bool CurrentFrameRendered { get; private set; } = false;
        public bool IsPlaying { get; private set; } = false;
        public double InverseContentScale => 1 / ContentScale;
        public double ContentScale => zoomableCanvas.Scale;
        public Matrix camera;
        //public ShaderBank ShaderBank { get; private set; } = new ShaderBank();

        /// <summary>
        /// this timer is set to the comp's framerate and is used when playing
        /// TODO when changing the fps in AnimationTimeline change the timer
        /// </summary>
        DispatcherTimer timer;
        ClickAndDragHandler gizmoCanvasClickAndDrag;
        ClickAndDragHandler bgCanvasClickAndDrag;
        SelectionSquareHandler selectionSquareHandlerBg;

        private System.Windows.Point startOffset;
        private System.Windows.Point downPos;

        /// <summary>
        /// object taking care of copying
        /// </summary>
        //OpenGLRenderer renderer;
        public MotioRenderer motioRenderer;

        /// <summary>
        /// remember to set Handled to true if it is 
        /// </summary>
        public event Action<MouseEventArgs> ViewportClicked;
        public event Action<ClickLogic.DragEventArgs> ViewportDragStart;
        public event Action<ClickLogic.DragEventArgs> ViewportDrag;
        public event Action<ClickLogic.DragEventArgs> ViewportDragEnd;
        public event Action<double> ContentScaleChanged;

        public RenderView()
        {
            InitializeComponent();
        }

        private void RenderView_Loaded(object sender, RoutedEventArgs e)
        {
            if (Timeline == null)
            {
                Timeline = (AnimationTimelineViewModel)DataContext;
                if (Timeline.root.RenderView != null)
                    throw new Exception("already a renderview in this timeline");

                //renderer = new OpenGLRenderer(Timeline.ResolutionWidth, Timeline.ResolutionHeight);
                //renderer.DoInit += InitRenderView;
                //renderer.DoPaint += Paint;
                //renderer.ImageUpdated += Renderer_ImageUpdated;
                //renderer.Start();
                Timeline.GraphicsNodes.CollectionChanged += GraphicsNodes_CollectionChanged;
                foreach (GraphicsNodeViewModel g in Timeline.GraphicsNodes)
                {
                    SetRequestPaint(g);
                }

                motioRenderer = new MotioRenderer(Timeline.ResolutionWidth, Timeline.ResolutionHeight, 
                    Timeline.CameraWidth, Timeline.CameraHeight, 
                    Timeline.CameraNearPlane, Timeline.CameraFarPlane,
                    () => Timeline.CurrentFrame);
                motioRenderer.ImageUpdated += Renderer_ImageUpdated;
                Timeline.PropertyChanged += Timeline_PropertyChanged;

                PrepareTimer();
                timer.Start();

                InitClickHandlers();

                //set last because some element lazy load when this is set
                //so make sure everything is initialized before setting the RenderView in root
                Timeline.root.RenderView = this;
                motioRenderer.RequestPaint(Timeline.Original.GraphicsNodes);
                
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    //Thread.Sleep(1000);
                    Application.Current.Dispatcher.Invoke(ScaleToFit);
                });
            }
            var gizmoOffsetX = zoomableCanvas.ActualWidth / 2 - Timeline.ResolutionWidth / 2;
            var gizmoOffsetY = zoomableCanvas.ActualHeight / 2 - Timeline.ResolutionHeight / 2;
            Canvas.SetLeft(gizmoCanvas, gizmoOffsetX);
            Canvas.SetTop(gizmoCanvas, gizmoOffsetY);
            Gizmo.Init(gizmoCanvas, Timeline);

            Canvas.SetLeft(border, gizmoOffsetX);
            Canvas.SetTop(border, gizmoOffsetY);
        }

        private void Renderer_ImageUpdated(WriteableBitmap bitmap)
        {
            viewport.Source = bitmap;
        }

        //private void InitModels()
        //{
        //    Timeline.GraphicsNodes.CollectionChanged += GraphicsNodes_CollectionChanged;
        //    foreach (GraphicsNodeViewModel g in Timeline.GraphicsNodes)
        //    {
        //        CreateModel(g);
        //    }
        //}

        //public void InitRenderView()
        //{
        //    //CompileShaders();
        //    //float widthOver2 = Timeline.CameraWidth / 2;
        //    //float heightOver2 = Timeline.CameraHeight / 2;
        //    //camera = Matrix.CreateOrthographic2(-widthOver2, widthOver2, heightOver2, -heightOver2, Timeline.CameraNearPlane, Timeline.CameraFarPlane);

        //    //GL.Enable(EnableCap.Blend);
        //    //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        //    //GL.Enable(EnableCap.DepthTest);
        //    //GL.Viewport(0, 0, Timeline.ResolutionWidth, Timeline.ResolutionHeight);

        //    InitModels();
        //    foreach(GraphicsNodeViewModel node in Timeline.GraphicsNodes)
        //    {
        //        node.UpdateModel();
        //    }
        //}

        //private void Paint()
        //{
        //    GL.ClearColor(0, 0, 0, 0);
        //    GL.Clear(
        //        ClearBufferMask.ColorBufferBit |
        //        ClearBufferMask.DepthBufferBit |
        //        ClearBufferMask.StencilBufferBit);


        //    Dictionary<Node, MeshModel> models = new Dictionary<Node, MeshModel>();
        //    //changer l'archi pour avoir un meshmodel par Node stocké directement dans la node (pas dans un dict pour eviter le lookup)
        //    //var nodes = Timeline.GraphicsNodes.originalList;
        //    List<GraphicsNode> nodes = new List<GraphicsNode>(Timeline.GraphicsNodes.originalList);
        //    //nodes.Reverse();
        //    for(int i = 0; i < nodes.Count; i++)
        //    {
        //        GraphicsNode node = nodes[i];
        //        GraphicsNodeMeshDisplayer displayer = (GraphicsNodeMeshDisplayer)node.meshDisplayer;
        //        if (!node.Visible)
        //            continue;
        //        for(int k = 0; k < node.attachedNodes.Count; k++)
        //        {
        //            GraphicsAffectingNode gAff = node.attachedNodes[k];
        //            models.Add(gAff, displayer.modelGroup[k]);
        //        }
        //        //GraphicsNodeMeshDisplayer displayer = (GraphicsNodeMeshDisplayer)node.meshDisplayer;
        //        //displayer.modelGroup.UpdateAllBuffers();
        //        //displayer.modelGroup.RenderAll(Timeline.CurrentFrame, camera);
        //    }
        //}

        public void PrepareTimer()
        {
            /*
             * 			// https://evanl.wordpress.com/2009/12/06/efficient-optimal-per-frame-eventing-in-wpf/
			var args = (RenderingEventArgs)e;
			if ( args.RenderingTime == mLast )
			{
				return;
			}
			mLast = args.RenderingTime;
             * 
             * 
             */

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1f / Timeline.Fps)
            };
            timer.Tick += (_, __) => OnTimerTick();
        }

        /// <summary>
        /// this is the function called by the timer every frame (when playing)
        /// </summary>
        public void OnTimerTick()
        {
            //we only need to modify the current frame, the rest of the app will react 
            //automatically
            if (!CurrentFrameRendered)
                RenderCurrentFrame();
            if (CurrentFrameRendered && IsPlaying)
                Timeline.CurrentFrame++;
        }

        /// <summary>
        /// this fonction will render the current frame if it has been cached 
        /// otherwise it won't do shit bruh
        /// </summary>
        public void RenderCurrentFrame()
        {
            foreach (GraphicsNodeViewModel node in Timeline.GraphicsNodes)
            {
                if (!node.UpdateModel())
                {
                    if (IsPlaying)
                        Timeline.StartBackgroundProcessingAtCurrentFrame();
                    else
                        Timeline.AskToCacheFrame(Timeline.CurrentFrame);
                    return;
                }
            }
            CurrentFrameRendered = true;
        }

        /// <summary>
        /// this is called by the MainWindow when pressing Space
        /// it should play the video
        /// </summary>
        public void ExecutePlayPause()
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                Timeline.StopBackgroundProcessing();

                try
                {
                    GC.EndNoGCRegion();
                }
                catch (InvalidOperationException)
                {
                    Logger.WriteLine("end no gc threw up, if this keeps on going and you experience"
                        + " some lags, try increasing the 'GC Start Alloc' value in Edit>Config");
                }
            }
            else
            {
                try
                {
                    GC.TryStartNoGCRegion(Configs.GetValue<long>(Configs.NoGCStartAlloc));
                }
                catch (Exception)
                {
                    Logger.WriteLine("the value in 'GC Start Alloc' broke all the things, change it in Edit>Config");
                }
                IsPlaying = true;
                Timeline.StartBackgroundProcessingAtCurrentFrame();
            }
        }

        public void SwitchToToolClick()
        {
            if(gizmoCanvasClickAndDrag.currentlyHooked == null)
            {
                selectionSquareHandlerBg.UnHook(grid);

                bgCanvasClickAndDrag.Hook(clickCanvas);
                gizmoCanvasClickAndDrag.Hook(gizmoCanvas);
            }
        }

        public void SwitchToSelectionClick()
        {
            if(gizmoCanvasClickAndDrag.currentlyHooked == gizmoCanvas)
            {
                gizmoCanvasClickAndDrag.UnHook(gizmoCanvas);
                bgCanvasClickAndDrag.UnHook(clickCanvas);

                selectionSquareHandlerBg.Hook(grid);
            }
        }

        private void InitClickHandlers()
        {
            //we can only hook to one of the click handler because they both require 
            //to capture the mouse, so one will not work if they are both hooked

            selectionSquareHandlerBg = new SelectionSquareHandler(grid);
            selectionSquareHandlerBg.Hook(grid);
            selectionSquareHandlerBg.OnClick += GizmoCanvas_OnClick;
            selectionSquareHandlerBg.clearOnDown = false;

            //the Handler hooks the given element by default
            gizmoCanvasClickAndDrag = new ClickAndDragHandler(gizmoCanvas)
            {
                OnDragEnter = GizmoCanvas_OnDragEnter,
                OnDrag = GizmoCanvas_OnDrag,
                OnDragEnd = GizmoCanvas_OnDragEnd,
                OnClick = GizmoCanvas_OnClick
            };
            //unhooked by default because the SelectionSquareHandler is hooked by default
            gizmoCanvasClickAndDrag.UnHook(gizmoCanvas);
            bgCanvasClickAndDrag = new ClickAndDragHandler(clickCanvas)
            {
                OnDragEnter = GizmoCanvas_OnDragEnter,
                OnDrag = GizmoCanvas_OnDrag,
                OnDragEnd = GizmoCanvas_OnDragEnd,
                OnClick = GizmoCanvas_OnClick
            };
            bgCanvasClickAndDrag.UnHook(clickCanvas);

            //only for middle click drag
            new ClickAndDragHandler(clickCanvas)
            {
                clickFilter = e => e.MiddleButton == MouseButtonState.Pressed,
                OnDrag = GizmoCanvas_OnDrag,
                OnDragEnter = GizmoCanvas_OnDragEnter
            };
            new ClickAndDragHandler(gizmoCanvas)
            {
                clickFilter = e => e.MiddleButton == MouseButtonState.Pressed,
                OnDrag = GizmoCanvas_OnDrag,
                OnDragEnter = GizmoCanvas_OnDragEnter
            };

            zoomableCanvas.ScaleChanged += zoomableCanvas_SizeChanged;
        }

        private void GizmoCanvas_OnClick(MouseButtonEventArgs e)
        {
            //check if there is nothing between the canvas and the mouse (a gizmo for example)
            if (Mouse.DirectlyOver == clickCanvas || Mouse.DirectlyOver == gizmoCanvas)
            {
                //this should set e.Handled to true if an active tool received the click
                ViewportClicked?.Invoke(e);
            }
        }

        private void GizmoCanvas_OnDragEnter(ClickLogic.DragEventArgs e)
        {
            startOffset = zoomableCanvas.Offset;
            downPos = Mouse.PrimaryDevice.GetPosition(Window.GetWindow(this));
            if (e.currentEvent.MiddleButton == MouseButtonState.Pressed)
                return;
            if(gizmoCanvasClickAndDrag.currentlyHooked != null)
                ViewportDragStart?.Invoke(e);
        }

        private void GizmoCanvas_OnDrag(ClickLogic.DragEventArgs e)
        {
            if (e.startEvent.MiddleButton == MouseButtonState.Pressed)
            {
                var currentPos = e.currentEvent.GetPosition(Window.GetWindow(this));
                var delta =  downPos - currentPos;
                var offset = (delta + startOffset);
                SetOffset(offset);
            }
            else
            {
                ViewportDrag?.Invoke(e);
            }
        }

        private void GizmoCanvas_OnDragEnd(ClickLogic.DragEventArgs e)
        {
            if (e.lastEvent.MiddleButton == MouseButtonState.Pressed)
                return;
            ViewportDragEnd?.Invoke(e);
        }

        private void SetRequestPaint(GraphicsNodeViewModel node)
        {
            ((GraphicsNode)node.Original).RequestPaint = () => motioRenderer.RequestPaint(Timeline.Original.GraphicsNodes);
        }

        private void RemoveRequestPaint(GraphicsNodeViewModel node)
        {
            ((GraphicsNode)node.Original).RequestPaint = null;
        }

        private void GraphicsNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        foreach (GraphicsNodeViewModel g in e.NewItems)
                        {
                            SetRequestPaint(g);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        foreach (GraphicsNodeViewModel g in e.OldItems)
                        {
                            RemoveRequestPaint(g);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        foreach (GraphicsNodeViewModel g in e.OldItems)
                        {
                            RemoveRequestPaint(g);
                        }
                        foreach (GraphicsNodeViewModel g in e.NewItems)
                        {
                            SetRequestPaint(g);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        //e.OldItems is not set with Reset events
                        throw new Exception("can't clear the list must delete elements one by one");
                    }
            }
        }

        private void Timeline_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //manually intersept the change on CurrentFrame. So when the current frame changes, the render updates too
            if (e.PropertyName.Equals(nameof(AnimationTimelineViewModel.CurrentFrame)))
            {
                CurrentFrameRendered = false;
                RenderCurrentFrame();
            }
        }

        private void zoomableCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var x = Math.Pow(2, e.Delta / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            SetScale(zoomableCanvas.Scale * x);

            // Adjust the offset to make the point under the mouse stay still.
            var position = (Vector)e.GetPosition(Window.GetWindow(this));
            SetOffset((System.Windows.Point)((Vector)(zoomableCanvas.Offset + position) * x - position));

            e.Handled = true;
        }

        private void SetScale(double scale)
        {
            //if scaling in or scaling out and can scale out
            if (scale > zoomableCanvas.Scale || (scale < zoomableCanvas.Scale && CanScaleOut(scale)))
            {
                zoomableCanvas.Scale = scale;
                //gizmoCanvas.Scale = scale;
                //prevent the windo from being badly offset by forcing the offset back through
                //the filter
                SetOffset(zoomableCanvas.Offset);
            }
        }

        private bool CanScaleOut(double scale)
        {
            return !(zoomableCanvas.ActualWidth * scale < scroll.ActualWidth
                || zoomableCanvas.ActualHeight * scale < scroll.ActualHeight);
        }

        private void SetOffset(System.Windows.Point point)
        {
            var limitX = zoomableCanvas.ActualWidth * zoomableCanvas.Scale - scroll.ActualWidth;
            if (point.X >  limitX)
                point.X = limitX;
            var limitY = zoomableCanvas.ActualHeight * zoomableCanvas.Scale - scroll.ActualHeight;
            if (point.Y > limitY)
                point.Y = limitY;

            if (point.X < 0)
                point.X = 0;
            if (point.Y < 0)
                point.Y = 0;

            zoomableCanvas.Offset = point;
            // gizmoCanvas.Offset = new Vector(gizmoOffsetX, gizmoOffsetY) + point;
        }

        private void zoomableCanvas_SizeChanged(double size)
        {
            ContentScaleChanged?.Invoke(size);
        }

        private void ZoomPointToViewportCenter(double newContentScale, System.Windows.Point contentZoomFocus)
        {
            double minScale = scroll.ActualWidth / zoomableCanvas.ActualWidth;
            double maxScale = double.MaxValue;
            newContentScale = Math.Min(Math.Max(newContentScale, minScale), maxScale);

            zoomableCanvas.Scale = newContentScale;
            double offsetX = contentZoomFocus.X - (scroll.ActualWidth / 2);
            double offsetY = contentZoomFocus.Y - (scroll.ActualHeight / 2);
            zoomableCanvas.Offset = new System.Windows.Point(offsetX, offsetY);
        }

        public void ZoomTo(Rect contentRect)
        {
            if (scroll.ActualWidth == 0 || scroll.ActualHeight == 0)
            {
                //ThreadPool.QueueUserWorkItem((o) =>
                //{
                //    Thread.Sleep(1000);
                //    Application.Current.Dispatcher.Invoke(() => ZoomTo(contentRect));
                //});
                return;
            }
            double scaleX = scroll.ActualWidth / contentRect.Width;
            double scaleY = scroll.ActualHeight / contentRect.Height;
            double newScale = zoomableCanvas.Scale * Math.Min(scaleX, scaleY);

            ZoomPointToViewportCenter(newScale, new System.Windows.Point(contentRect.X + (contentRect.Width / 2), contentRect.Y + (contentRect.Height / 2)));
        }

        public void ScaleToFit()
        {
            ZoomTo(new Rect(0, 0, Timeline.ResolutionWidth, Timeline.ResolutionHeight));
        }
    }
}
