using System.Collections.Generic;
using System.Linq;
using Motio.NodeCommon.Utils;

namespace Motio.Selecting
{
    public class SelectionGroup
    {
        private OrderedHashSet<ISelectable> items = new OrderedHashSet<ISelectable>();
        /// <summary>
        /// iternal member to iterate without copying 
        /// </summary>
        internal ICollection<ISelectable> Iter => items;

        public IList<ISelectable> Members => items.ToList();
        public int Count => items.Count;

        internal void Add(ISelectable item)
        {
            items.Add(item);
        }

        internal void Remove(ISelectable item)
        {
            items.Remove(item);
        }

        public bool Contains(ISelectable item)
        {
            return items.Contains(item);
        }
    }
}
