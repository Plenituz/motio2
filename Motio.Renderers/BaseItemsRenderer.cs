using System.Collections.Generic;

namespace Motio.Renderers
{
    public abstract class BaseItemsRenderer<DataItem, VisualItem>
    {
        /// <summary>
        /// list of data to render, this can be readOnly
        /// </summary>
        protected abstract IEnumerable<DataItem> DataItems { get; }
        protected virtual IDictionary<DataItem, VisualItem> VisualItems { get; set; } = new Dictionary<DataItem, VisualItem>();

        /// <summary>
        /// this won't create new item, just update the one already displayed
        /// </summary>
        public virtual void UpdateVisibleItems()
        {
            foreach (KeyValuePair<DataItem, VisualItem> pair in VisualItems)
            {
                UpdateItem(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// completly update everything : create new items if necessary and remove ones that are no longer in DataItems
        /// </summary>
        public virtual void UpdateRender()
        {
            //check all the current dataitems and update/create a visualitem accordingly
            foreach(DataItem dataItem in DataItems)
            {
                if (VisualItems.ContainsKey(dataItem))
                {
                    UpdateItem(dataItem);
                }
                else
                {
                    VisualItem newVisual = CreateItem(dataItem);
                    VisualItems.Add(dataItem, newVisual);
                }
            }

            //now looking for elements that are still in VisualItems but are gone from DataItems
            HashSet<DataItem> dataCopy = new HashSet<DataItem>(DataItems);
            LinkedList<DataItem> toRemoveList = new LinkedList<DataItem>();
            foreach(DataItem dataItem in VisualItems.Keys)
            {
                if (!dataCopy.Contains(dataItem))
                {
                    toRemoveList.AddFirst(dataItem);
                }
            }

            //now dataItems should only contain the items that are in visualitems and not in dataitems
            foreach(DataItem toRemove in toRemoveList)
            {
                RemoveItem(toRemove, VisualItems[toRemove]);
                VisualItems.Remove(toRemove);
            }
        }

        /// <summary>
        /// Create a visual item and return it, you don't need to touch <see cref="VisualItems"/> yourself
        /// but you do need to add your visual item to whatever layout/canvas it needs to be visible
        /// </summary>
        /// <param name="dataItem"></param>
        /// <returns></returns>
        protected abstract VisualItem CreateItem(DataItem dataItem);
        /// <summary>
        /// remove the given <paramref name="visualItem"/> from it's layout so it's no longer rendered
        /// </summary>
        /// <param name="toRemove"></param>
        /// <param name="visualItem"></param>
        protected abstract void RemoveItem(DataItem dataItem, VisualItem visualItem);
        /// <summary>
        /// update the given <paramref name="visualItem"/> with the data of <paramref name="dataItem"/>
        /// </summary>
        /// <param name="dataItem"></param>
        /// <param name="visualItem"></param>
        public abstract void UpdateItem(DataItem dataItem, VisualItem visualItem);

        public void UpdateItem(DataItem dataItem)
        {
            //when click and dragging outside the bounds the item might no longer be in VisualItems
            bool got = VisualItems.TryGetValue(dataItem, out VisualItem item);
            if(got)
                UpdateItem(dataItem, item);
        }
    }
}
