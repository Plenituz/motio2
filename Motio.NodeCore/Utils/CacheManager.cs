using Motio.Debuging;
using Motio.NodeCommon.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Motio.NodeCore.Utils
{
    public class CacheManager
    {
        private ConcurrentDictionary<ICacheHost, IChainCache> caches = new ConcurrentDictionary<ICacheHost, IChainCache>();
        /// <summary>
        /// key: main node, value: list of node to invalidate when main node is invalidated, (ie list of dependants)
        /// </summary>
        private ConcurrentDictionary<ICacheMember, List<ICacheMember>> dependancies = new ConcurrentDictionary<ICacheMember, List<ICacheMember>>();

        public void AddCache(ICacheHost node, IChainCache cache)
        {
            if (!caches.TryAdd(node, cache))
            {
                Logger.WriteLine("couldnt add uuid " + node + " to cache manager with " + cache);
            }
        }
            
        public void RemoveCache(ICacheHost node)
        {
            caches.TryRemove(node, out _);
        }

        public void AddToChain(ICacheHost node, ICacheMember member, int atIndex)
        {
            IChainCache chainCache = caches[node];
            chainCache.AddAt(member, atIndex);
        }

        public void RemoveFromChain(ICacheHost node, int atIndex)
        {
            IChainCache chainCache = caches[node];
            chainCache.Remove(atIndex);
        }

        public void MoveInChain(ICacheHost node, int fromIndex, int toIndex)
        {
            IChainCache chainCache = caches[node];
            chainCache.Move(fromIndex, toIndex);
        }

        public void ReplaceInChain(ICacheHost node, int removeIndex, ICacheMember toAdd, int addIndex)
        {
            IChainCache chainCache = caches[node];
            chainCache.Remove(removeIndex);
            chainCache.AddAt(toAdd, addIndex);
        }

        public void ClearChain(ICacheHost node)
        {
            IChainCache chainCache = caches[node];
            chainCache.RemoveAll();
        }

        public void AddAllToChain(ICacheHost node, IEnumerable<ICacheMember> members)
        {
            IChainCache chainCache = caches[node];
            foreach (var member in members)
            {
                chainCache.Add(member);
            }
        }

        /// <summary>
        /// remove the first items from the cache until the cache count matches <paramref name="untilCount"/>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="firstXItems"></param>
        public void RemoveFirstsFromChain(ICacheHost node, int untilCount)
        {
            IChainCache chainCache = caches[node];
            while(chainCache.Count != untilCount)
            {
                chainCache.Remove(0);
            }
        }

        /// <summary>
        /// remove the lasts items from the cache until the cache count matches <paramref name="untilCount"/>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="lastXItems"></param>
        public void RemoveLastsFromChain(ICacheHost node, int untilCount)
        {
            IChainCache chainCache = caches[node];
            while (chainCache.Count != untilCount)
            {
                chainCache.Remove(chainCache.Count - 1);
            }
        }

        private IEnumerable<(IChainCache cache, int startIndex)> RetrieveDependancies(ICacheMember node, HashSet<ICacheMember> alreadyFound = null)
        {
            if (!dependancies.TryGetValue(node, out var dependants))
                yield break;

            if (alreadyFound == null)
                alreadyFound = new HashSet<ICacheMember>();
            if (alreadyFound.Contains(node))
                yield break;
            alreadyFound.Add(node);

            for(int i = 0; i < dependants.Count; i++)
            {
                ICacheMember dependant = dependants[i];
                ICacheHost dependantHost = dependant.Host;
                IChainCache dependantCache = caches[dependantHost];
                int index = dependantHost.IndexOf(dependant);
                yield return (dependantCache, index);

                foreach (var tuple in RetrieveDependancies(dependant, alreadyFound))
                    yield return tuple;
            }
        }

        private void RunOnDependants(ICacheHost node, int fromIndex, Action<(IChainCache cache, int startIndex)> action)
        {
            foreach(ICacheMember member in node.Members)
            {
                foreach (var tuple in RetrieveDependancies(member))
                {
                    action(tuple);
                }
            }
        }

        public void StartSingleFrame(ICacheHost node, int frame, int index, int endNode = -1)
        {
            IChainCache cache = caches[node];
            cache.StartSingleFrame(frame, index);

            RunOnDependants(node, index, 
                (tuple) => tuple.cache.StartSingleFrame(frame, tuple.startIndex, endNode));
        }

        public void StartBatchCalculate(ICacheHost node, int startFrame, int endFrame, int nodeIndex)
        {
            IChainCache cache = caches[node];
            cache.StartBatchCalculate(startFrame, endFrame, nodeIndex);

            RunOnDependants(node, nodeIndex, 
                (tuple) => tuple.cache.StartBatchCalculate(startFrame, endFrame, tuple.startIndex));
        }

        /// <summary>
        /// this method doesnt take in account the dependants
        /// </summary>
        /// <param name="node"></param>
        public void StopCalculationOnlyMe(ICacheHost node)
        {
            IChainCache cache = caches[node];
            cache.StopCalculating();
        }

        public void StopAllCalculation()
        {
            foreach(var item in caches)
            {
                item.Value.StopCalculating();
            }
        }

        public bool IsFrameCached(ICacheHost node, int frame)
        {
            IChainCache cache = caches[node];
            return cache.IsFullyCached(frame);
        }

        public DataFeed GetCache(ICacheHost node, int nodeIndex, int frame)
        {
            IChainCache cache = caches[node];
            return cache.GetCache(nodeIndex, frame);
        }

        public bool TryGetCache(ICacheHost node, int index, int frame, out DataFeed dataFeed)
        {
            IChainCache cache = caches[node];
            return cache.TryGetCache(index, frame, out dataFeed);
        }

        public void ClearCacheAfter(ICacheHost node, int nodeIndex, int frame)
        {
            IChainCache cache = caches[node];
            cache.ClearCacheAfter(nodeIndex, frame);

            RunOnDependants(node, nodeIndex, 
                (tuple) => tuple.cache.ClearCacheAfter(tuple.startIndex, frame));
        }

        public void ClearAllFramesAfter(ICacheHost node, int nodeIndex)
        {
            IChainCache cache = caches[node];
            cache.ClearAllFramesAfter(nodeIndex);

            RunOnDependants(node, nodeIndex, 
                (tuple) => tuple.cache.ClearAllFramesAfter(tuple.startIndex));
        }

        /// <summary>
        /// register <paramref name="uuidDependant"/> as a dependant of <paramref name="uuidMain"/>.
        /// A dependant is a node that will be invalidated when the main node is invalidated
        /// </summary>
        /// <param name="uuidDependant"></param>
        /// <param name="uuidMain"></param>
        public void RegisterDependant(ICacheMember mainNode, ICacheMember dependantNode)
        {
            List<ICacheMember> dependants;
            if(!dependancies.TryGetValue(mainNode, out dependants))
            {
                dependants = new List<ICacheMember>();
                dependancies.TryAdd(mainNode, dependants);
            }
            dependants.Add(dependantNode);
        }

        public void UnregisterDependant(ICacheMember mainNode, ICacheMember dependantNode)
        {
            if(dependancies.TryGetValue(mainNode, out var dependants))
            {
                lock (dependants)
                {
                    dependants.Remove(dependantNode);
                }
                if(dependants.Count == 0)
                {
                    dependancies.TryRemove(mainNode, out _);
                }
            }
        }

    }
}
