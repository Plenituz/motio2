using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Motio.Renderers.BezierRendering
{
    /// <summary>
    /// class to render a Bezier Curve (given as a List of <see cref="IBezierPoint"/>). The rendering is automatically
    /// updated uppon modification of <see cref="RenderedPoints"/>. you can also change the rendered part of the curve 
    /// with <see cref="BoundedItemRenderer{DataItem, VisualItem}.SetBounds(double, double, double, double)"/>
    /// </summary>
    public class BezierCurveRenderer<VisualPointType, VisualTangentType> : BoundedItemRenderer<IBezierPoint, VisualBezierPoint<VisualPointType, VisualTangentType>>
        where VisualPointType : BezierVisualItem, new()
        where VisualTangentType : BezierVisualItem, new()
    {
        private readonly CoordinateTransformer ToCanvasSpace;
        private readonly CoordinateTransformer ToWorldSpace;
        private readonly PointDeleter Deleter;
        private readonly PointAdder Adder;
        private readonly string pointSelectionGroup;
        private readonly string tangentSelectionGroup;
        private readonly IBezierContainer canvas;
        private double pointSize = 0;
        private double curveThickness = 0;

        /// <summary>
        /// TODO make this an ObservableHashSet because Contains is used quite a bit PERF
        /// </summary>
        public ObservableCollection<IBezierPoint> RenderedPoints { get; } = new ObservableCollection<IBezierPoint>();
        /// <summary>
        /// if the given <see cref="IBezierPoint"/> items rendered implements 
        /// <see cref="INotifyPropertyChanged"/> then the property name in this list
        /// will trigger an update.
        /// </summary>
        public HashSet<string> propertiesToListenToOnPoints;
        public event Action<IBezierPoint> Click;
        protected override IEnumerable<IBezierPoint> AllDataItems => RenderedPoints;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="Transformer">transform from IBezier data space to canvas space</param>
        /// <param name="Deleter">try to delete the given point from the actual bezier curve data and return wether the removal was sucessful</param>
        public BezierCurveRenderer(IBezierContainer canvas, 
            CoordinateTransformer ToCanvasSpace, 
            CoordinateTransformer ToWorldSpace, 
            PointDeleter Deleter,
            PointAdder Adder,
            string pointSelectionGroup,
            string tangentSelectionGroup)
        {
            this.canvas = canvas;
            this.ToCanvasSpace = ToCanvasSpace;
            this.ToWorldSpace = ToWorldSpace;
            this.Deleter = Deleter;
            this.Adder = Adder;
            this.pointSelectionGroup = pointSelectionGroup;
            this.tangentSelectionGroup = tangentSelectionGroup;
            //auto update render when collection changes
            RenderedPoints.CollectionChanged += RenderedPoints_CollectionChanged;
        }

        private void RenderedPoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Move:
                    UpdateVisibleItems();
                    break;
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Add:
                    UpdateRender();
                    SetPointSize(pointSize);
                    SetCurveThickness(curveThickness);
                    break;
            }
        }

        public void SetCurveThickness(double thickness)
        {
            curveThickness = thickness;
            foreach (VisualBezierPoint<VisualPointType, VisualTangentType> point in VisualItems.Values)
            {
                point.SetCurveThickness(thickness);
            }
        }

        public void SetPointSize(double size)
        {
            pointSize = size;
            foreach(VisualBezierPoint<VisualPointType, VisualTangentType> point in VisualItems.Values)
            {
                point.SetPointSize(size);
            }
        }

        public void ForceRedraw(IBezierPoint point)
        {
            var visual = VisualItems[point];
            visual.RemoveFromCanvas(canvas);
            visual.AddToCanvas(canvas);
        }

        public void Delete()
        {
            RenderedPoints.CollectionChanged -= RenderedPoints_CollectionChanged;
        }

        protected override VisualBezierPoint<VisualPointType, VisualTangentType> CreateItem(IBezierPoint dataItem)
        {
            VisualBezierPoint<VisualPointType, VisualTangentType> visualItem
                = new VisualBezierPoint<VisualPointType, VisualTangentType>(
                point: dataItem,
                IsRendered: RenderedPoints.Contains,
                ToCanvasSpace: ToCanvasSpace,
                ToWorldSpace: ToWorldSpace,
                Deleter: Deleter,
                Adder: Adder,
                pointSelectionGroup: pointSelectionGroup,
                tangentSelectionGroup: tangentSelectionGroup);
            UpdateItem(dataItem, visualItem);
            visualItem.AddToCanvas(canvas);
            visualItem.Click += VisualItem_Click;

            if (dataItem is INotifyPropertyChanged notifier)
            {
                notifier.PropertyChanged += DataItem_PropertyChanged;
            }
            return visualItem;
        }

        private void VisualItem_Click(IBezierPoint obj)
        {
            Click?.Invoke(obj);
        }

        private void DataItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(propertiesToListenToOnPoints != null && propertiesToListenToOnPoints.Contains(e.PropertyName))
            {
                IBezierPoint point = (IBezierPoint)sender;
                UpdateItem(point);

                //since curves are drawn from point to next point, we need to update the previous
                //point if there is one. That way the curve from the previous point to the current point is updated
                IBezierPoint previous = point.PreviousPoint;
                if (previous != null)
                    UpdateItem(previous);
            }
        }

        protected override Point DataItemToPoint(IBezierPoint dataItem)
        {
            return new Point(dataItem.PointX, dataItem.PointY);
        }

        protected override Point DataSpaceToVisualSpace(IBezierPoint dataItem)
        {
            (float x, float y) = ToCanvasSpace(dataItem.PointX, dataItem.PointY);
            return new Point(x, y);
        }

        protected override void RemoveItem(IBezierPoint dataItem, VisualBezierPoint<VisualPointType, VisualTangentType> visualItem)
        {
            visualItem.RemoveFromCanvas(canvas);
            visualItem.Click -= VisualItem_Click;
            if (dataItem is INotifyPropertyChanged notifier)
            {
                notifier.PropertyChanged -= DataItem_PropertyChanged;
            }
        }

        protected override void UpdateItem(IBezierPoint dataItem, VisualBezierPoint<VisualPointType, VisualTangentType> visualItem, Point visualSpacePos)
        {
            visualItem.SetPointSize(pointSize);
            visualItem.SetCurveThickness(curveThickness);
            visualItem.UpdateRender((float)visualSpacePos.X, (float)visualSpacePos.Y);
        }
    }
}
