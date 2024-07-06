using Motio.NodeCommon.ObjectStoringImpl;
using Motio.NodeCore;
using Motio.ObjectStoring;
using Motio.UI.Utils;
using Motio.UI.Views;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class MainControlViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentFile { get; set; }
        public AnimationTimelineViewModel AnimationTimeline { get; private set; }
        public PropertyPanelViewModel PropertyPanel { get; private set; }
        public KeyframePanelViewModel KeyframePanel { get; private set; }
        public CurvePanelViewModel CurvePanel { get; private set; }
        public Canvas CanvasOverEverything { get; set; }
        public FrameworkElement MenuQuiDecaleTout { get; set; }
        private RenderView _renderView;

        public RenderView RenderView
        {
            get => _renderView;
            set
            {
                bool wasNull = _renderView == null;

                _renderView = value;

                if (wasNull)
                    InitRenderViewDependant();
            }
        }

        public MainControlViewModel(AnimationTimelineViewModel timeline)
        {
            AnimationTimeline = timeline;
            AnimationTimeline.root = this;
            KeyframePanel = new KeyframePanelViewModel(this);
            CurvePanel = new CurvePanelViewModel(this);
        }

        /// <summary>
        /// this is only called when the renderview is not null
        /// </summary>
        void InitRenderViewDependant()
        {
            PropertyPanel = new PropertyPanelViewModel(AnimationTimeline, this);
            AnimationTimeline.SetupViewModels();
        }

        public void NewNode()
        {
            var newNode = new GraphicsNode(AnimationTimeline.Original);
            PropertyPanel.AddGraphicsNode(ProxyStatic.GetProxyOf<GraphicsNodeViewModel>(newNode));
        }

        public void SaveToFile(string path)
        {
            List<object> viewModels = new List<object>();
            object nodeTimelineData;
            try
            {
                nodeTimelineData = TimelineSaver.Save(AnimationTimeline.Original, new NodeSavableManager());
            }
            catch(Exception e)
            {
                MessageBox.Show("Error saving nodes:\n" + e);
                return;
            }

            try
            {
                foreach(object vm in ProxyStatic.original2proxy.KeysSecond())
                {
                    object data = TimelineSaver.Save(vm, new NodeSavableManager());
                    if (data != vm)
                        viewModels.Add(data);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error saving view models:\n" + e);
                return;
            }
            Hashtable fullData = new Hashtable(2);
            fullData.Add("original", nodeTimelineData);
            fullData.Add("viewModels", viewModels);

            try
            {
                string json = JsonConvert.SerializeObject(fullData, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error saving to file:\n" + e);
            }
        }
    }
}
