using Motio.UICommon;
using System.Collections.Generic;
using System.Windows;

namespace Motio.Renderers
{
    public abstract class BoundedItemRenderer<DataItem, VisualItem> : BaseItemsRenderer<DataItem, VisualItem>
    {
        public SimpleRect Bounds { get; private set; } = new SimpleRect();

        private HashSet<DataItem> visibleDataItems = new HashSet<DataItem>();
        protected override IEnumerable<DataItem> DataItems => visibleDataItems;
        protected abstract IEnumerable<DataItem> AllDataItems { get; }

        /// <summary>
        /// Change the bounds, note that the bounds are in data space
        /// this will not trigger an UpdateRender
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        public void SetBounds(double left, double right, double top, double bottom)
        {
            Bounds.Left = left;
            Bounds.Right = right;
            Bounds.Top = top;
            Bounds.Bottom = bottom;
        }

        public override void UpdateRender()
        {
            //faut trouver les element qui sont dans visibleDataItem et pas dans AllDataItems
            HashSet<DataItem> items = new HashSet<DataItem>(AllDataItems);
            visibleDataItems.RemoveWhere((dataItem) => !items.Contains(dataItem));

            foreach(DataItem dataItem in items)
            {
                Point dataPoint = DataItemToPoint(dataItem);
                if (Bounds.IsInside(dataPoint))
                {
                    if (!visibleDataItems.Contains(dataItem))
                        visibleDataItems.Add(dataItem);
                }
                else
                {
                    if (visibleDataItems.Contains(dataItem))
                        visibleDataItems.Remove(dataItem);
                }
            }
            base.UpdateRender();
        }

        public override void UpdateItem(DataItem dataItem, VisualItem visualItem)
        {
            Point visualSpacePos = DataSpaceToVisualSpace(dataItem);
            UpdateItem(dataItem, visualItem, visualSpacePos);
        }

        /// <summary>
        /// transform the <paramref name="dataItem"/> into a Point in data space
        /// </summary>
        /// <param name="dataItem"></param>
        /// <returns></returns>
        protected abstract Point DataItemToPoint(DataItem dataItem);
        /// <summary>
        /// transform the given <paramref name="dataItem"/> into a point in visual space (ready to be added to a canvas for example)
        /// </summary>
        /// <param name="dataItem"></param>
        /// <returns></returns>
        protected abstract Point DataSpaceToVisualSpace(DataItem dataItem);
        /// <summary>
        /// update the item with the given <paramref name="visualSpacePos"/> which is the position of the dataItem transformed
        /// by the bounds to be displayed
        /// </summary>
        /// <param name="dataItem"></param>
        /// <param name="visualItem"></param>
        /// <param name="visualSpacePos"></param>
        protected abstract void UpdateItem(DataItem dataItem, VisualItem visualItem, Point visualSpacePos);
    }
}
