using Motio.Debuging;
using Motio.Geometry;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.NodeCore.Interfaces;
using Motio.NodeCore.Utils;
using Motio.ObjectStoring;
using Motio.PythonRunning;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Motio.NodeCore
{
    /// <summary>
    /// this is the equivalent to a "composition" in after effect
    /// </summary>
    public class AnimationTimeline : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public CacheManager CacheManager { get; } = new CacheManager();

        private int _currentFrame;
        /// <summary>
        /// the current frame being viewed on the timeline.
        /// The viewport and every property should react to this and render accordingly
        /// </summary>
        public int CurrentFrame
        {
            get { return _currentFrame; }
            set
            {
                if(_currentFrame != value)
                {
                    if (value < 0)
                        _currentFrame = 0;
                    else if (value > MaxFrame)
                        _currentFrame = MaxFrame;
                    else
                        _currentFrame = value;
                }
            }
        }

        /// <summary>
        /// the maximum number of frame in this timeline
        /// TODO make this a property that the keyframe view and other bind to
        /// </summary>
        [SaveMe]
        public int MaxFrame { get; set; } = 500;
        /// <summary>
        /// size of the rendering canvas in pixel
        /// </summary>
        [SaveMe]
        public int ResolutionWidth { get; set; } = 1920;
        /// <summary>
        /// size of the rendering canvas in pixel
        /// </summary>
        [SaveMe]
        public int ResolutionHeight { get; set; } = 1080;
        [SaveMe]
        public float Fps { get; set; } = 24;
        [SaveMe]
        public float CameraWidth { get; set; } = 100;
        [SaveMe]
        public Vector3 CameraPosition { get; set; } = new Vector3(0, 0, 500);
        /// <summary>
        /// not used for now
        /// </summary>
        [SaveMe]
        public Vector3 CameraLookDirection { get; set; } = new Vector3(0, 0, -1);
        [SaveMe]
        public float CameraFarPlane { get; set; } = 2000;
        [SaveMe]
        public float CameraNearPlane { get; set; } = -2000;
        public float AspectRatio => ((float)ResolutionHeight / ResolutionWidth);
        public float CameraHeight => CameraWidth * AspectRatio;

        public NodeUUIDGroup uuidGroup = new NodeUUIDGroup();

        [SaveMe]
        public ObservableCollection<GraphicsNode> GraphicsNodes { get; set; } = new ObservableCollection<GraphicsNode>();

        //prevent creation of Timelines, to create a timeline call Instance
        public AnimationTimeline()
        {
            EventHall.Reset();
            GraphicsNodes.CollectionChanged += GraphicsNodes_CollectionChanged;
        }

        private void GraphicsNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EventHall.Trigger(GraphicsNodes, "AnimationTimeline.GraphicsNodes", "CollectionChanged", e);
        }

        //private void HotSwapNode(GraphicsNode graphicsNode, CreatablePythonNode creatableNode)
        //{
        //string creatableNodeClassName = Regex.Replace(creatableNode.className, @"(.*\.)*", "");


        //for (int gAffectingIndex = 0; gAffectingIndex < graphicsNode.attachedNodes.Count; gAffectingIndex++)
        //{
        //    GraphicsAffectingNode fAff = graphicsNode.attachedNodes[gAffectingIndex];
        //    if (typeof(IDynamicNode).IsAssignableFrom(fAff.GetType()))
        //    {
        //        string className = Python.GetClassName(fAff);
        //        if (className.Equals(creatableNodeClassName))
        //        {
        //            //delete the current node 
        //            //fAff.Delete();

        //            //hot swap it in the list cross your finger nothing breaks :D
        //            GraphicsAffectingNode newInstance = (GraphicsAffectingNode)creatableNode.CreateInstance();
        //            if (newInstance == null)
        //                continue;

        //            newInstance.nodeHost = graphicsNode;

        //            graphicsNode.attachedNodes[gAffectingIndex] = newInstance;
        //            //invalidate all the cache 
        //            graphicsNode.InvalidateAllFrames();
        //        }
        //    }
        //}
        //}

        private void ListNodeOfType(IHasAttached hasAttached, CreatablePythonNode creatableNode, List<Node> nodes)
        {
            foreach(Node node in hasAttached.AttachedMembers)
            {
                ListNodeOfType(node, creatableNode, nodes);
            }
        }

        private void ListNodeOfType(Node node, CreatablePythonNode creatableNode, List<Node> nodes)
        {
            //do swap
            if(creatableNode.PythonType.__instancecheck__(node))
            {
                nodes.Add(node);
            }

            if(node is IHasAttached hasAttached)
            {
                ListNodeOfType(hasAttached, creatableNode, nodes);
            }
            for(int i = 0; i < node.Properties.Count; i++)
            {
                ListNodeOfType(node.Properties[i], creatableNode, nodes);
            }
        }

        /// <summary>
        /// right now you can only hot swap graphics nodes 
        /// TODO hot swap property nodes 
        /// </summary>
        /// <param name="creatableNode"></param>
        public void HotSwapNode(CreatablePythonNode creatableNode)
        {
            //first find all the nodes of python type
            List<Node> instances = new List<Node>();

            for (int graphicsIndex = 0; graphicsIndex < GraphicsNodes.Count; graphicsIndex++)
            {
                GraphicsNode graphicsNode = GraphicsNodes[graphicsIndex];
                ListNodeOfType((Node)graphicsNode, creatableNode, instances);
            }
            PythonException compileEx = creatableNode.Recompile();
            if (compileEx != null)
                throw compileEx;

            for(int i = 0; i < instances.Count; i++)
            {
                if(instances[i] is IHasHost hasHost && hasHost.Host is IHasAttached hasAttached)
                {
                    dynamic newInstance = creatableNode.CreateIntance();
                    //set host first, then replace it otherwise the node might try to use 
                    //it's parent while not having one
                    if(newInstance is IHasHost newInstanceHasHost)
                        newInstanceHasHost.Host = hasAttached;
                    hasAttached.ReplaceMemberAt(i, newInstance);
                }
            }
            Logger.WriteLine("hot swapped " + instances.Count + " instances");
        }

        public void AskToCacheFrame(int frame)
        {
            for(int i = 0; i < GraphicsNodes.Count; i++)
            {
                GraphicsNodes[i].StartCalculatingSingleFrame(frame);
            }
        }

        public bool IsFrameCached(int frame)
        {
            for(int i = 0; i < GraphicsNodes.Count; i++)
            {
                if (!CacheManager.IsFrameCached(GraphicsNodes[i], frame))
                {
                    return false;
                }
            }
            return true;
        }

        public void StartBackgroundProcessingAtCurrentFrame()
        {
            for (int i = 0; i < GraphicsNodes.Count; i++)
            {
                GraphicsNodes[i].StartBackgroundProcessing(CurrentFrame);
            }
        }

        public void StopBackgroundProcessing()
        {
            CacheManager.StopAllCalculation();
        }
    }
}
