using Motio.Configuration;
using Motio.NodeCommon.ObjectStoringImpl;
using Motio.PythonRunning;
using Motio.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Motio.UI.Utils.Export
{
    public abstract class TimelineExporter
    {
        public static List<ICreatableNode> exporters = new List<ICreatableNode>()
        {
            new NativeCreatableNode(typeof(PNGExporter))
        };

        public static void PopulateExporters()
        {
            foreach(var pair in Python.PythonPool)
            {
                if(typeof(TimelineExporter).IsAssignableFrom(pair.Value.PythonType) 
                    && Python.GetClassNameStatic(pair.Value.PythonType) != null)
                {
                    exporters.Add(new PythonCreatableNodeWrapper(pair.Value));
                }
            }
        }

        private DispatcherTimer exportTimer;

        protected readonly MainControlViewModel mainViewModel;
        protected int startFrame, endFrame;

        public event Action Done;
        public event Action<int> Progress;
        public event Action<Exception> Error;

        public string path;
        public IList<ConfigEntry> OptionsProp => MakeOptions();

        /// <summary>
        /// is set by dialog before render
        /// </summary>
        public Dictionary<string, ConfigEntry> Options { get; set; }

        public TimelineExporter(MainControlViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            exportTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1f / mainViewModel.AnimationTimeline.Fps)
            };
            exportTimer.Tick += (_, __) => TimerTick();
        }

        private void TimerTick()
        {
            mainViewModel.RenderView.RenderCurrentFrame();
            if (mainViewModel.RenderView.CurrentFrameRendered)
            {
                ExportCurrentFrame(path.Replace("#", mainViewModel.AnimationTimeline.CurrentFrame.ToString()));
                mainViewModel.AnimationTimeline.CurrentFrame++;
                Progress?.Invoke(mainViewModel.AnimationTimeline.CurrentFrame);

                if (mainViewModel.AnimationTimeline.CurrentFrame > endFrame)
                {
                    StopExport();
                    Done?.Invoke();
                }
            }
        }

        public void StartExportRange(int from, int to)
        {
            if (mainViewModel.RenderView.IsPlaying)
                mainViewModel.RenderView.ExecutePlayPause();
            mainViewModel.AnimationTimeline.CurrentFrame = from;
            mainViewModel.AnimationTimeline.StartBackgroundProcessingAtCurrentFrame();

            this.startFrame = from;
            this.endFrame = to;

            BeforeExport();

            exportTimer.Start();
        }

        public void StopExport()
        {
            exportTimer.Stop();
            mainViewModel.AnimationTimeline.StopBackgroundProcessing();
            AfterExport();
        }

        protected virtual void BeforeExport()
        {

        }

        protected abstract void ExportCurrentFrame(string path);

        protected virtual void AfterExport()
        {

        }

        public virtual IList<ConfigEntry> MakeOptions()
        {
            return new ConfigEntry[] { };
        }

        public abstract string Filter();

        protected void TriggerError(Exception ex)
        {
            StopExport();
            Error?.Invoke(ex);
        }

    }
}
