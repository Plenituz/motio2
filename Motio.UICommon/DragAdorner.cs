﻿using System.Windows.Documents;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;

namespace Motio.UICommon
{
    public class DragAdorner : Adorner
    {
        private Rectangle child = null;
        private double offsetLeft = 0;
        private double offsetTop = 0;
     
        /// <summary>
        /// Initializes a new instance of DragVisualAdorner.
        /// </summary>
        /// <param name="adornedElement">The element being adorned.</param>
        /// <param name="size">The size of the adorner.</param>
        /// <param name="brush">A brush to with which to paint the adorner.</param>
        public DragAdorner( UIElement adornedElement, Size size, Brush brush )
            : base( adornedElement )
        {
            Rectangle rect = new Rectangle();
            rect.Fill = brush;
            rect.Width = size.Width;
            rect.Height = size.Height;
            rect.IsHitTestVisible = false;
            this.child = rect;
        }
  
        public override GeneralTransform GetDesiredTransform( GeneralTransform transform )
        {
            GeneralTransformGroup result = new GeneralTransformGroup();
            result.Children.Add( base.GetDesiredTransform( transform ) );
            result.Children.Add( new TranslateTransform( this.offsetLeft, this.offsetTop ) );
            return result;
        }
      
  
        /// <summary>
        /// Gets/sets the horizontal offset of the adorner.
        /// </summary>
        public double OffsetLeft
        {
            get { return this.offsetLeft; }
            set
            {
                this.offsetLeft = value;
                UpdateLocation();
            }
        }
  
   
        /// <summary>
        /// Updates the location of the adorner.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public void SetOffsets( double left, double top )
        {
            this.offsetLeft = left;
            this.offsetTop = top;
            this.UpdateLocation();
        }
   
  
        /// <summary>
        /// Gets/sets the vertical offset of the adorner.
        /// </summary>
        public double OffsetTop
        {
            get { return this.offsetTop; }
            set
            {
                this.offsetTop = value;
                UpdateLocation();
            }
        }
  
        /// <summary>
        /// Override.
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride( Size constraint )
        {
            this.child.Measure( constraint );
            return this.child.DesiredSize;
        }
  
        /// <summary>
        /// Override.
        /// </summary>
        /// <param name="finalSize"></param>
        /// <returns></returns>
        protected override Size ArrangeOverride( Size finalSize )
        {
            this.child.Arrange( new Rect( finalSize ) );
            return finalSize;
        }
  
        /// <summary>
        /// Override.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected override Visual GetVisualChild( int index )
        {
            return this.child;
        }
  
        /// <summary>
        /// Override.  Always returns 1.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }
  
 
        private void UpdateLocation()
        {
            AdornerLayer adornerLayer = this.Parent as AdornerLayer;
            if( adornerLayer != null )
                adornerLayer.Update( this.AdornedElement );
        }
    }
}
