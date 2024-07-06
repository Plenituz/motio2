using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using Motio.Meshing;
using Motio.ObjectStoring;
using Motio.NodeCore.Interfaces;
using System.Collections.Specialized;
using Motio.Configuration;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCommon.Utils;
using Motio.Debuging;
using Motio.Pathing;
using System.ComponentModel;
using Motio.NodeCore.Utils;
using Motio.Rendering;

namespace Motio.NodeCore
{
    //[AddINotifyPropertyChangedInterface]
    public class GraphicsNode : Node, IHasHost, ISetParent, IHasAttached<GraphicsAffectingNode>, ICacheHost, IRenderable
    {
        ///notify property change for the name that may change in the timeline etc
       // public event PropertyChangedEventHandler PropertyChanged;

        //--------------Clip view stuff
        public AnimationTimeline timelineHost;

        //public IMeshDisplayer meshDisplayer;
        /// <summary>
        /// time (in frames) si graphics starts on the clip view
        /// </summary>
        [SaveMe]
        public int startTime;
        /// <summary>
        /// duration in frame on the clip view
        /// </summary>
        [SaveMe]
        public int duration;
        //TODO suposed time etc
        [SaveMe]
        public bool Visible { get; set; } = true;

        //--------------Node stuff
        /// <summary>
        /// all the node affecting this, in order of call with 0 the first evaluated node 
        /// </summary>
        [SaveMe]
        public ObservableCollection<GraphicsAffectingNode> attachedNodes
            { get; private set; } = new ObservableCollection<GraphicsAffectingNode>();

        //protected NodeCache cache;
        //protected NodeChainCache cacheChain;

        IEnumerable<GraphicsAffectingNode> IHasAttached<GraphicsAffectingNode>.AttachedMembers => attachedNodes;
        IEnumerable IHasAttached.AttachedMembers => attachedNodes;
        IEnumerable<ICacheMember> ICacheHost.Members => attachedNodes;

        public object Host { get => timelineHost; set => timelineHost = (AnimationTimeline)value; }
        public Action RequestPaint;

        private int batchProcessingAt = -1;

        //Node cache object, doing the caching and GetValueForFrame
        //c'est la node qui gere le multithreading
        //the caching is done via multiple threads
        //seul la graphics node fait du multi threading pour le moment

        //if you add properties to the graphics node don't forget to invalidate them in InvalidataFrame


        [CreateLoadInstance]
        static GraphicsNode CreateLoadInstance(AnimationTimeline parent, Type createThis)
        {
            return new GraphicsNode(parent, false);
        }

        protected GraphicsNode(AnimationTimeline timeline, bool addToTl)
        {
            this.timelineHost = timeline;
            //this has to be after the this.timelineHost  = .
            //and before the add to GraphicsNodes
            UserGivenName = "GraphicsNode";
            if (addToTl)
                timelineHost.GraphicsNodes.Add(this);
            attachedNodes.CollectionChanged += AttachedNodes_CollectionChanged;

            NodeChainCache cacheChain = new NodeChainCache(NewConfiguredFeed);
            cacheChain.BatchCalculationDone += CacheChain_BatchCalculationDone;
            cacheChain.Prepare += PrepareBatchProcessing;
            timeline.CacheManager.AddCache(this, cacheChain);

            PropertyChanged += GraphicsNode_PropertyChanged;

            AfterConstructor();
        }

        private void GraphicsNode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Visible)))
            {
                UpdateModel();
            }
        }

        public GraphicsNode(AnimationTimeline timeline) : this(timeline, true)
        {

        }

        [OnDoneLoading]
        void OnDone()
        {
            timelineHost.CacheManager.AddAllToChain(this, attachedNodes);
            attachedNodes.CollectionChanged += AttachedNodes_CollectionChanged;
        }

        private void AttachedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //listen to change in attachedNodes to sync the NodeChainCache
            EventHall.Trigger(this, UUID.ToString() + ".AttachedNodes", "CollectionChanged", e);
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        GraphicsAffectingNode added = (GraphicsAffectingNode)e.NewItems[0];
                        timelineHost.CacheManager.AddToChain(this, added, e.NewStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        timelineHost.CacheManager.MoveInChain(this, e.OldStartingIndex, e.NewStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        if(e.OldItems.Count > 1)
                            throw new ArgumentException();
                        GraphicsAffectingNode removed = (GraphicsAffectingNode)e.OldItems[0];
                        timelineHost.CacheManager.RemoveFromChain(this, e.OldStartingIndex);
                        UpdateModel();
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        if (e.NewItems.Count > 1)
                            throw new ArgumentException();
                        GraphicsAffectingNode added = (GraphicsAffectingNode)e.NewItems[0];
                        GraphicsAffectingNode removed = (GraphicsAffectingNode)e.OldItems[0];
                        timelineHost.CacheManager.ReplaceInChain(this, e.OldStartingIndex, added, e.NewStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        timelineHost.CacheManager.ClearChain(this);
                    }
                    break;
            }
            StartCalculatingSingleFrame(timelineHost.CurrentFrame);
        }

        /// <summary>
        /// TODO probably going to have to change the attachedNode way of working
        /// to allow for easier order swapping ?
        /// </summary>
        /// <param name="node"></param>
        public void AttachNode(GraphicsAffectingNode node)
        {
            attachedNodes.Add(node);
            //the cache automatically invalidates only the necessary parts when adding an item to attachedNodes
        }

        public void StartCalculatingSingleFrame(int frame)
        {
            batchProcessingAt = -1;
            timelineHost.CacheManager.StartSingleFrame(this, frame, 0);
            //cache.StartOrGoBackgroundProcessing(frame, true);
        }

        public void StartBackgroundProcessing(int atFrame)
        {
            if (atFrame < 0 && atFrame > timelineHost.MaxFrame)
                return;
            batchProcessingAt = atFrame + Configs.GetValue<int>(Configs.FrameBatchSize);
            timelineHost.CacheManager.StartBatchCalculate(this, atFrame, batchProcessingAt, 0);
            //cache.StartOrGoBackgroundProcessing(atFrame, false);
        }

        public void StopBackgroundProcessing()
        {
            batchProcessingAt = -1;
            timelineHost.CacheManager.StopCalculationOnlyMe(this);
        }

        internal virtual void PrepareBatchProcessing()
        {
            for (int i = 0; i < attachedNodes.Count; i++)
            {
                if (attachedNodes[i].Enabled)
                    attachedNodes[i].Prepare();
            }
        }

        private static DataFeed NewConfiguredFeed()
        {
            DataFeed dataFeed = new DataFeed();
            dataFeed.EnforceChannelType(MESH_CHANNEL, typeof(MeshGroup));
            dataFeed.EnforceChannelType(PATH_CHANNEL, typeof(PathGroup));
            dataFeed.EnforceChannelType(POLYGON_CHANNEL, typeof(MotioShapeGroup));
            return dataFeed;
        }

        private void CacheChain_BatchCalculationDone()
        {
            if (batchProcessingAt != -1)
            {
                if (batchProcessingAt > timelineHost.MaxFrame)
                    return;
                int nextMax = batchProcessingAt + Configs.GetValue<int>(Configs.FrameBatchSize);
                if (batchProcessingAt > timelineHost.MaxFrame)
                    nextMax = timelineHost.MaxFrame;
                timelineHost.CacheManager.StartBatchCalculate(this, batchProcessingAt, nextMax, 0);
                batchProcessingAt = nextMax;
            }
            else
            {
                //timelineHost.TriggerCacheUpdate(-1);
                UpdateModel();
            }
        }

        public void InvalidateCache(int frame, GraphicsAffectingNode fromNode)
        {
            int index = attachedNodes.IndexOf(fromNode);
            if (index == -1)
            {
                Logger.WriteLine("tried to invalidate a node not on this graphics node");
                return;
            }
            timelineHost.CacheManager.ClearCacheAfter(this, index, frame);

            //for(int i = 0; i < attachedNodes.Count; i++)
            //{
            //    attachedNodes[i].Properties.InvalidateFrame(frame);
            //}
            StartCalculatingSingleFrame(timelineHost.CurrentFrame);
        }

        public void InvalidateAllCachedFrames(GraphicsAffectingNode fromNode)
        {
            int index = attachedNodes.IndexOf(fromNode);
            if (index == -1)
            {
                Logger.WriteLine("tried to invalidate a node not on this graphics node");
                return;
            }
            timelineHost.CacheManager.ClearAllFramesAfter(this, index);
            
            //for(int i = 0; i < attachedNodes.Count; i++)
            //{
            //    for(int f = 0; f < timelineHost.MaxFrame; f++)
            //    {
            //        attachedNodes[i].Properties.InvalidateFrame(f);
            //    }
            //}
            StartCalculatingSingleFrame(timelineHost.CurrentFrame);
        }

        public DataFeed GetCache(int frame, int nodeIndex)
        {
            return timelineHost.CacheManager.GetCache(this, frame, nodeIndex);
        }

        public DataFeed GetCache(int frame, GraphicsAffectingNode node)
        {
            int index = attachedNodes.IndexOf(node);
            if (index == -1)
                throw new Exception("tried to access a node not on this graphics node");
            return GetCache(index, frame);
        }

        public bool GetCachedMeshForFrame(int frame, out MeshGroup meshGroup)
        {
            if (attachedNodes.Count == 0)
            {
                meshGroup = new MeshGroup();
                return true;
            }

            if(!timelineHost.CacheManager.TryGetCache(this, attachedNodes.Count - 1, frame, out DataFeed dataFeed) || dataFeed == null)
            {
                meshGroup = new MeshGroup();
                return false;
            }

            //if(dataFeed.TryGetChannelData(TRIANGULATION_RESULT_CHANNEL, out MeshGroup cachedTriangulation))
            //{
            //    meshGroup = cachedTriangulation;
            //    return true;
            //}

            meshGroup = new MeshGroup();

            if (dataFeed.TryGetChannelData(MESH_CHANNEL, out MeshGroup channelGroup))
            {
                meshGroup.AddAll(channelGroup);
            }

            if (dataFeed.TryGetChannelData(POLYGON_CHANNEL, out MotioShapeGroup shapeGroup))
            {
                for(int i = 0; i < shapeGroup.Count; i++)
                {
                    if(shapeGroup.Count != 0)
                    {
                        try
                        {
                            MeshGroup fromShape = MotioShape2Mesh.Convert(shapeGroup[i]);
                            meshGroup.AddAll(fromShape);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLine("Error triangulating, you probably gave null points or points with component equal to infinity or NaN.\n" + ex);
                        }
                    }
                }
            }
            //dataFeed.SetChannelData(TRIANGULATION_RESULT_CHANNEL, meshGroup);

            return true;
        }

        public bool UpdateModel()
        {
            bool gotData = GetCachedMeshForFrame(timelineHost.CurrentFrame, out MeshGroup mesh);
            if (gotData && RequestPaint != null)
                RequestPaint();
            return gotData;
        }

        public override void SetupProperties()
        {
        }

        public override void Delete()
        {
            base.Delete();
            while (attachedNodes.Count != 0)
                attachedNodes[0].Delete();
            timelineHost.GraphicsNodes.Remove(this);
            //the cache invalidates itself when nodes are removed
        }

        public void SetParent(object parent) => Host = parent;

        public void ReplaceMemberAt(int index, object member)
        {
            attachedNodes[index] = (GraphicsAffectingNode)member;
        }

        int ICacheHost.IndexOf(ICacheMember member)
        {
            if (!(member is GraphicsAffectingNode gAff))
                throw new Exception("cant get the index of a not GraphicsAffectingNode");
            return attachedNodes.IndexOf(gAff);
        }

        MeshGroup IRenderable.GetMeshes(int frame)
        {
            GetCachedMeshForFrame(frame, out var meshes);
            return meshes;
        }
    }
}
