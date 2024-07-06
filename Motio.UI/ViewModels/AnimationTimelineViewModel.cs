using System.ComponentModel;
using Motio.Geometry;
using Motio.NodeCore;
using Motio.UI.Utils;
using Motio.Animation;
using System.Windows.Media.Media3D;
using Motio.NodeCommon.ObjectStoringImpl;
using Motio.ObjectStoring;
using Motio.PythonRunning;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Windows;
using Motio.Debuging;

namespace Motio.UI.ViewModels
{
    public class AnimationTimelineViewModel : Proxy<AnimationTimeline>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MainControlViewModel root;
        AnimationTimeline _original;
        public override AnimationTimeline Original => _original;

        public int CurrentFrame             { get => Original.CurrentFrame; set => Original.CurrentFrame = value; }
        public int MaxFrame                 { get => Original.MaxFrame; set => Original.MaxFrame = value; }
        public int ResolutionWidth          { get => Original.ResolutionWidth; set => Original.ResolutionWidth = value; }
        public int ResolutionHeight         { get => Original.ResolutionHeight; set => Original.ResolutionHeight = value; }
        public float AspectRatio                  => Original.AspectRatio;
        public float Fps                    { get => Original.Fps; set => Original.Fps = value; }
        public float CameraWidth            { get => Original.CameraWidth; set => Original.CameraWidth = value; }
        public float CameraHeight                 => Original.CameraHeight;
        public Vector3 CameraPosition       { get => Original.CameraPosition; set => Original.CameraPosition = value; }
        public Vector3 CameraLookDirection  { get => Original.CameraLookDirection; set => Original.CameraLookDirection = value; }
        public float CameraFarPlane         { get => Original.CameraFarPlane; set => Original.CameraFarPlane = value; }
        public float CameraNearPlane        { get => Original.CameraNearPlane; set => Original.CameraNearPlane = value; }

        public ListProxy<GraphicsNode, GraphicsNodeViewModel> GraphicsNodes { get; private set; }

        /// <summary>
        /// all errors are handled in the mthod, so sometimes it returns null but should never crash
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static AnimationTimelineViewModel CreateFromFile(string file)
        {
            JArray viewModels;
            JObject original;
            try
            {
                string raw = File.ReadAllText(file);
                JObject dict = (JObject)JsonConvert.DeserializeObject(raw);
                viewModels = (JArray)dict["viewModels"];
                original = (JObject)dict["original"];
            }
            catch (Exception e)
            {
                MessageBox.Show("Error reading file " + file + ":\n" + e);
                return null;
            }

            AnimationTimeline timeline;
#if !DEBUG
            try
            {
#endif
                timeline = (AnimationTimeline)TimelineLoader.Load(original, new NodeCreatableProvider());
#if !DEBUG
            }
            catch (Exception e)
            {
                MessageBox.Show("Error converting json for 'original':\n" + e);
                return null;
            }
#endif
            AnimationTimelineViewModel viewModel = new AnimationTimelineViewModel(timeline);

#if !DEBUG
            try
            {
#endif
            //the custom loaders rely on the fact that the nodes are already loaded
            foreach (JObject jobj in viewModels)
            {
                //this relies on the fact that viewmodel should add a "uuid" entry to their json
                if (jobj["uuid"] == null)
                    throw new Exception(jobj + " is a view model but doesn't have a uuid entry");
                object parent;
                string uuid = jobj["uuid"].ToString();
                if (uuid.Contains("."))
                {
                    Exception e = timeline.uuidGroup.LookupProperty(uuid, out Node node, out NodePropertyBase prop);
                    if (e != null)
                    {
                        //throw e;
                        Logger.WriteLine("found fantom property " + e.Message);
                        continue;
                    }

                    parent = prop;
                }
                else
                {
                    Node node = timeline.uuidGroup.LookupNode(uuid);
                    parent = node ?? throw new Exception("couldn't find node with uuid " + uuid);
                }

                parent = ProxyStatic.GetProxyOf(parent);
                //this triggers the custom loader on the viewmodels, they assign themself to the right place
                TimelineLoader.PopulateObjectInstance(ref parent, jobj, useCustom: true);
                //call on done loading
                TimelineLoader.CallOnDoneLoading(ref parent);
            }
#if !DEBUG
            }
            catch(Exception e)
            {
                MessageBox.Show("Error converting json for 'viewModels':\n" + e);
                return null;
            }
#endif
            


            return viewModel;
        }

        public static AnimationTimelineViewModel Create()
        {
            AnimationTimeline timeline = new AnimationTimeline();
            AnimationTimelineViewModel viewModel = new AnimationTimelineViewModel(timeline);
            return viewModel;
        }

        public AnimationTimelineViewModel(AnimationTimeline original) : base(original)
        {
            _original = original;

            GraphicsNodes = new ListProxy<GraphicsNode, GraphicsNodeViewModel>
                (Original.GraphicsNodes, Original.GraphicsNodes);
            Original.PropertyChanged += Original_PropertyChanged;
            //Original.CacheUpdated += Original_CacheUpdated;
        }

        internal void SetupViewModels()
        {
            foreach(object proxy in ProxyStatic.original2proxy.KeysSecond())
            {
                if (proxy is NodeViewModel nodevm)
                    nodevm.TrySetupViewModel();
            }
        }

        /// <summary>
        /// function called by PropertyChanged.Fody to trigger PropertyChanged
        /// also used to manually trigger PropertyChanged
        /// </summary>
        /// <param name="propertyName"></param>
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //TODO the ui layer should use something else than creatable node, the viewmodel will create the creatableNode 
        //public void HotSwapNode(CreatableNode creatableNode)
        //{
        //    _original.HotSwapNode(creatableNode);
        //}

        private void Original_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public void StartBackgroundProcessingAtCurrentFrame() => Original.StartBackgroundProcessingAtCurrentFrame();
        public void StopBackgroundProcessing() => Original.StopBackgroundProcessing();
        public void AskToCacheFrame(int frame) => Original.AskToCacheFrame(frame);
        public bool IsFrameCached(int frame) => Original.IsFrameCached(frame);
        public void HotSwapNode(CreatablePythonNode node) => Original.HotSwapNode(node);

        /// <summary>
        /// gizmo canvas position to world position
        /// </summary>
        /// <param name="screenPos"></param>
        /// <returns></returns>
        public System.Windows.Point Canv2World(System.Windows.Point screenPos)
        {
            //center is (0,0). top left is (-width/2, height/2) bot right is (width/2, -height/2)
            //this is a value with (0, 0) in to left and (ScreenWidth, ScreenHeight) in bot right
            System.Windows.Point screenPercent = new System.Windows.Point(
                screenPos.X / ResolutionWidth,
                screenPos.Y / ResolutionHeight);

            System.Windows.Point worldPos = new System.Windows.Point(
                x: MathHelper.Lerp(-CameraWidth / 2, CameraWidth / 2, (float)screenPercent.X),
                y: MathHelper.Lerp(CameraHeight / 2, -CameraHeight / 2, (float)screenPercent.Y));
            //Z is always 0

            return worldPos;
        }

        /// <summary>
        /// world position to gizmo canvas position
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public System.Windows.Point World2Canv(Point3D worldPos)
        {
            Vector2 worldPercent = new Vector2(
                (float)DoubleInterpolator.InverseLinear(worldPos.X, -CameraWidth / 2, CameraWidth / 2),
                (float)DoubleInterpolator.InverseLinear(worldPos.Y, CameraHeight / 2, -CameraHeight / 2)
                );
            System.Windows.Point canvPos = new System.Windows.Point(
                worldPercent.X * ResolutionWidth,
                worldPercent.Y * ResolutionHeight);
            return canvPos;
        }
    }
}
