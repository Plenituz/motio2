using Motio.NodeCommon.StandardInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Motio.Selecting
{
    public delegate void SelectionItemHandler(string group, ISelectable item);
    public delegate void SelectionGroupCleared(string groupName);

    /// <summary>
    /// allow an item to be selected only once
    /// </summary>
    public class Selection
    {
        public const string GIZMOS = "gizmos";
        public const string KEYFRAMES_TIMELINE = "keyframes_timeline";
        public const string KEYFRAME_CURVES = "keyframes_curves";
        public const string KEYFRAME_CURVES_TANGENTS = KEYFRAME_CURVES + " tangents";
        public const string PATH_POINTS = "path points " + GIZMOS;
        public const string PATH_TANGENTS = "path tangents " + GIZMOS;
        public const string MESH_POINTS = "mesh points " + GIZMOS;


        private static Selection _instance;
        public static Selection Instance => _instance ?? (_instance = new Selection());
        private Selection() { }

        /// <summary>
        /// iterate of the whole selection. you can delete while iterating since this uses
        /// a copy of the selection
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, ISelectable>> All() => Instance._All();
        public static void Add(string groupName, ISelectable item) => Instance._Add(groupName, item);
        public static void Remove(string groupName, ISelectable item) => Instance._Remove(groupName, item);
        public static void ClearAll() => Instance._ClearAll();
        public static void ClearGroup(string groupName) => Instance._ClearGroup(groupName);
        /// <summary>
        /// list all selected item in <paramref name="groupName"/>. This returns a copy of the 
        /// group, it's therefore safe to iterate and edit the selection at the same time
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static IList<ISelectable> ListGroup(string groupName) => Instance._ListGroup(groupName);
        public static IEnumerable<KeyValuePair<string, ISelectable>> ListHyperGroup(string hyperGroupName) => Instance._ListHyperGroup(hyperGroupName);
        public static bool Contains(ISelectable item) => Instance._Contains(item);
        public static int GroupCount(string groupName) => Instance._GroupCount(groupName);
        public static bool GroupContains(string groupName, ISelectable item) => Instance._GroupContains(groupName, item);
        public static void ClearHyperGroup(string hyperGroupName) => Instance._ClearHyperGroup(hyperGroupName);
        public static SelectionGroup GetGroup(string groupName) => Instance._GetGroup(groupName);

        private OrderedDictionary<string, SelectionGroup> groups = new OrderedDictionary<string, SelectionGroup>();

        public event SelectionItemHandler ItemSelected;
        public event SelectionItemHandler ItemUnSelected;
        public event SelectionGroupCleared GroupCleared;

        public IEnumerable<KeyValuePair<string, ISelectable>> _All()
        {
            foreach(var pair in groups)
            {
                //iterating using Members instead of Iter to allow user to deselect item as he iterate
                foreach(ISelectable selectable in pair.Value.Members)
                {
                    yield return new KeyValuePair<string, ISelectable>(pair.Key, selectable);
                }
            }
        }

        public void _Add(string groupName, ISelectable item)
        {
            if (!item.CanBeSelected && _Contains(item))
                return;
            if(!groups.TryGetValue(groupName, out SelectionGroup group))
            {
                group = new SelectionGroup();
                groups.Add(groupName, group);
            }
            group.Add(item);
            item.OnSelect();
            ItemSelected?.Invoke(groupName, item);
        }

        public void _Remove(string groupName, ISelectable item)
        {
            if (groups.TryGetValue(groupName, out SelectionGroup group))
            {
                if(group.Contains(item))
                {
                    group.Remove(item);
                    item.OnUnselect();
                    ItemUnSelected?.Invoke(groupName, item);
                }
            }
        }

        public void _ClearAll()
        {
            foreach(string groupName in groups.Keys)
            {
                _ClearGroup(groupName);
            }
        }

        public void _ClearGroup(string groupName)
        {
            if(groups.ContainsKey(groupName))
            {
                foreach(ISelectable selectable in groups[groupName].Iter)
                {
                    selectable.OnUnselect();
                }

                groups.Remove(groupName);
                GroupCleared?.Invoke(groupName);
            }
        }

        private void CallOnHyperGroup(string hyperGroupName, Action<string, SelectionGroup> action)
        {
            foreach (var pair in groups.ToList())
            {
                if (pair.Key.Contains(hyperGroupName))
                {
                    action?.Invoke(pair.Key, pair.Value);
                }
            }
        }

        public void _ClearHyperGroup(string hyperGroupName)
        {
            CallOnHyperGroup(hyperGroupName, (groupName, group) =>
            {
                _ClearGroup(groupName);
            });
        }

        public bool _GroupContains(string groupName, ISelectable item)
        {
            if (groups.TryGetValue(groupName, out var group))
                return group.Contains(item);
            else
                return false;
        }

        public IList<ISelectable> _ListGroup(string groupName)
        {
            if(groups.TryGetValue(groupName, out var group))
                return group.Members;
            else
                return new ISelectable[0];
        }

        public SelectionGroup _GetGroup(string groupName)
        {
            if (groups.TryGetValue(groupName, out var group))
                return group;
            else
                return new SelectionGroup();
        }

        /// <summary>
        /// a selectable is concidered part of a "Hyper group" if it's group name 
        /// contains the hyper group's name
        /// </summary>
        /// <param name="subGroupName"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, ISelectable>> _ListHyperGroup(string hyperGroupName)
        {
            foreach(var pair in groups)
            {
                if (pair.Key.Contains(hyperGroupName))
                {
                    foreach(ISelectable selectable in pair.Value.Members)
                    {
                        yield return new KeyValuePair<string, ISelectable>(pair.Key, selectable);
                    }
                }
            }
        }

        public int _GroupCount(string groupName)
        {
            if (groups.TryGetValue(groupName, out var group))
                return group.Count;
            else
                return 0;
        }

        /// <summary>
        /// is the item selected (in any group)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool _Contains(ISelectable item)
        {
            foreach(var pair in groups)
            {
                if (pair.Value.Contains(item))
                    return true;
            }
            return false;
        }
    }
}
