using Motio.ClickLogic;
using Motio.UICommon;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Motio.Renderers.BezierRendering
{
    /// <summary>
    /// class responsible for rendering a <see cref="IBezierPoint"/> and the curve to the 
    /// next item (and potentially previous)
    /// </summary>
    public class VisualBezierPoint<VisualPointType, VisualTangentType> 
        where VisualPointType : BezierVisualItem, new()
        where VisualTangentType : BezierVisualItem, new()
    {
        private readonly IBezierPoint point;
        private readonly Predicate<IBezierPoint> IsRendered;
        private readonly CoordinateTransformer ToCanvasSpace;
        private readonly CoordinateTransformer ToWorldSpace;
        private readonly PointDeleter Deleter;
        private readonly PointAdder Adder;
        private readonly SelectionAwareClickHandler<VisualPointType> pointClickHandler;
        private readonly SelectionAwareClickHandler<VisualTangentType> tangentClickHandler;
        private VisualPointType visualPoint;
        private VisualTangentType rightTangent;
        private VisualTangentType leftTangent;
        private Line lineToRight;
        private Line lineToLeft;
        private BezierCurve _curveToNext;
        /// <summary>
        /// curve to the next item, always rendered with this item (if there is a next item)
        /// </summary>
        private BezierCurve CurveToNext
        {
            get
            {
                IBezierPoint next = point.NextPoint;
                if (_curveToNext != null && _curveToNext.endPoint != next)
                {
                    if (addedToCanvas != null)
                        _curveToNext.RemoveFromCanvas(addedToCanvas);
                    _curveToNext = null;
                }

                if (_curveToNext != null)
                    return _curveToNext;
                if(next != null)
                {
                    _curveToNext = new BezierCurve(point, next, ToCanvasSpace);
                    _curveToNext.Click += CurveToNext_Click;
                    if (addedToCanvas != null)
                        _curveToNext.AddToCanvas(addedToCanvas);
                }

                return _curveToNext;
            }
        }

        private BezierCurve _curveToPrev;
        /// <summary>
        /// curve to previous item, only rendered is the previous item is not rendered
        /// </summary>
        private BezierCurve CurveToPrev
        {
            get
            {
                IBezierPoint prev = point.PreviousPoint;
                if (_curveToPrev != null && _curveToPrev.startPoint != prev)
                {
                    if (addedToCanvas != null)
                        _curveToPrev.RemoveFromCanvas(addedToCanvas);
                    _curveToPrev = null;
                }

                if (_curveToPrev != null)
                    return _curveToPrev;
                if (!shouldCurveToPrev)
                    return null;
                if (prev != null)
                {
                    _curveToPrev = new BezierCurve(prev, point, ToCanvasSpace);
                    if (addedToCanvas != null)
                        _curveToPrev.AddToCanvas(addedToCanvas);
                }

                return _curveToPrev;
            }
        }
        private bool shouldCurveToPrev = false;
        private IBezierContainer addedToCanvas = null;

        public event Action<IBezierPoint> Click;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point">the point to render</param>
        /// <param name="IsRendered">a function that tells weither or not the given point is rendered</param>
        /// <param name="ToCanvasSpace">transformation function from the bezier point space to canvas space</param>
        /// <param name="Deleter">function that delete the given point from the actual structure, not just the render</param>
        public VisualBezierPoint(
            IBezierPoint point, 
            Predicate<IBezierPoint> IsRendered,
            CoordinateTransformer ToCanvasSpace,
            CoordinateTransformer ToWorldSpace,
            PointDeleter Deleter,
            PointAdder Adder,
            string pointSelectionGroup,
            string tangentSelectionGroup)
        {
            this.point = point;
            this.IsRendered = IsRendered;
            this.ToCanvasSpace = ToCanvasSpace;
            this.ToWorldSpace = ToWorldSpace;
            this.Deleter = Deleter;
            this.Adder = Adder;
            pointClickHandler = new SelectionAwareClickHandler<VisualPointType>(null, pointSelectionGroup);
            tangentClickHandler = new SelectionAwareClickHandler<VisualTangentType>(null, tangentSelectionGroup);

            CreateVisualElements();
        }

        private void CreateVisualElements()
        {
            pointClickHandler.UpdatePosition += PointClickHandler_UpdatePosition;
            pointClickHandler.Click += PointClickHandler_Click;
            tangentClickHandler.UpdatePosition += TangentClickHandler_UpdatePosition;

            visualPoint = (VisualPointType)Activator.CreateInstance(typeof(VisualPointType), point);
            visualPoint.OnUnselect();//Set the default color 
            
            rightTangent = (VisualTangentType)Activator.CreateInstance(typeof(VisualTangentType), point);
            rightTangent.OnUnselect();
            rightTangent.left = false;

            leftTangent = (VisualTangentType)Activator.CreateInstance(typeof(VisualTangentType), point);
            leftTangent.OnUnselect();
            leftTangent.left = true;

            visualPoint.DoDelete += VisualPoint_DoDelete;
            rightTangent.DoDelete += VisualPoint_DoDelete;
            leftTangent.DoDelete += VisualPoint_DoDelete;

            lineToRight = new Line()
            {
                Stroke = Brushes.Black
            };
            lineToLeft = new Line()
            {
                Stroke = Brushes.Black
            };
        }

        private void CurveToNext_Click(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition((IInputElement)addedToCanvas);

            (float xWorld, float yWorld) = ToWorldSpace((float)position.X, (float)position.Y);
            //TODO give the tangents so the curve stays the same

            Adder?.Invoke(point, point.NextPoint, xWorld, yWorld);
        }

        private void PointClickHandler_Click(object sender, MouseEventArgs e)
        {
            //PointEllipse visualPoint = (PointEllipse)sender;
            Click?.Invoke(point);
        }

        private void PointClickHandler_UpdatePosition(VisualPointType selectable, Point position)
        {
            //the given selectable might not be the one from this instance of VisualBezierPoint,
            //it could be one of the same type (PointEllipse) that was selected
            (float xWorld, float yWorld) = ToWorldSpace((float)position.X, (float)position.Y);

            if(ToolBox.IsAltHeld())
            {
                //if alt is held we are creating new handles
                selectable.point.RightHandleX = xWorld - selectable.point.PointX;
                selectable.point.RightHandleY = yWorld - selectable.point.PointY;
                selectable.point.LeftHandleX = -selectable.point.RightHandleX;
                selectable.point.LeftHandleY = -selectable.point.RightHandleY;
            }
            else
            {
                //if alt is not held we are just moving the point
                selectable.point.PointX = xWorld;
                selectable.point.PointY = yWorld;
            }
        }

        private void TangentClickHandler_UpdatePosition(VisualTangentType selectable, Point position)
        {
            //the given selectable might not be the one from this instance of VisualBezierPoint,
            //it could be one of the same type (TangentEllipse) that was selected

            (float xWorld, float yWorld) = ToWorldSpace((float)position.X, (float)position.Y);
            //the given position is the absolute position of the handle
            //we need to put it back to position relative to the center point
            xWorld -= selectable.point.PointX;
            yWorld -= selectable.point.PointY; 

            if (selectable.left)
            {
                if (!ToolBox.IsCtrlHeld())
                {
                    float offsetX = xWorld - selectable.point.LeftHandleX;
                    float offsetY = yWorld - selectable.point.LeftHandleY;
                    selectable.point.RightHandleX -= offsetX;
                    selectable.point.RightHandleY -= offsetY;
                }
                selectable.point.LeftHandleX = xWorld;
                selectable.point.LeftHandleY = yWorld;
            }
            else
            {
                if (!ToolBox.IsCtrlHeld())
                {
                    float offsetX = xWorld - selectable.point.RightHandleX;
                    float offsetY = yWorld - selectable.point.RightHandleY;
                    selectable.point.LeftHandleX -= offsetX;
                    selectable.point.LeftHandleY -= offsetY;
                }
                selectable.point.RightHandleX = xWorld;
                selectable.point.RightHandleY = yWorld;
            }
        }

        public void SetPointSize(double size)
        {
            void SetSize(CenteredEllipse obj)
            {
                obj.Width = size;
                obj.Height = size;
            }
            SetSize(visualPoint);
            SetSize(rightTangent);
            SetSize(leftTangent);
        }

        public void SetCurveThickness(double thickness)
        {
            CurveToNext?.SetCurveThickness(thickness);
            CurveToPrev?.SetCurveThickness(thickness);
            lineToRight.StrokeThickness = thickness;
            lineToLeft.StrokeThickness = thickness;
        }

        public void UpdateRender(float pointVisualX, float pointVisualY)
        {
            void SetPos(CenteredEllipse obj, float x, float y)
            {
                Canvas.SetLeft(obj, x);
                Canvas.SetTop(obj, y);
            }

            void SetVisibility(CenteredEllipse obj, float x, float y)
            {
                if (x == 0 && y == 0)
                {
                    if (obj.Visibility != Visibility.Collapsed)
                        obj.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (obj.Visibility != Visibility.Visible)
                        obj.Visibility = Visibility.Visible;
                }
            }

            void SetLine(Line line, float x2, float y2)
            {
                line.X1 = pointVisualX;
                line.Y1 = pointVisualY;
                line.X2 = x2;
                line.Y2 = y2;
            }

            shouldCurveToPrev = !IsRendered(point.PreviousPoint);

            float pointWorldX = point.PointX;
            float pointWorldY = point.PointY;
            float rightWorldX = point.RightHandleX;
            float rightWorldY = point.RightHandleY;
            float leftWorldX = point.LeftHandleX;
            float leftWorldY = point.LeftHandleY;

            (float rightVisualX, float rightVisualY) = ToCanvasSpace(
                xWorld: pointWorldX + rightWorldX,
                yWorld: pointWorldY + rightWorldY);
            (float leftVisualX, float leftVisualY) = ToCanvasSpace(
                xWorld: pointWorldX + leftWorldX,
                yWorld: pointWorldY + leftWorldY);

            SetPos(visualPoint, pointVisualX, pointVisualY);
            SetPos(rightTangent, rightVisualX, rightVisualY);
            SetPos(leftTangent, leftVisualX, leftVisualY);
            SetVisibility(rightTangent, rightWorldX, rightWorldY);
            SetVisibility(leftTangent, leftWorldX, leftWorldY);

            SetLine(lineToRight, rightVisualX, rightVisualY);
            SetLine(lineToLeft, leftVisualX, leftVisualY);

            CurveToNext?.UpdateCurvePoints(
                p1X: pointVisualX, p1Y: pointVisualY,
                p2X: rightVisualX, p2Y: rightVisualY);

            if (shouldCurveToPrev)
            {
                CurveToPrev?.UpdateCurvePoints(
                    p3X: leftVisualX, p3Y: leftVisualY,
                    p4X: pointVisualX, p4Y: pointVisualY);
            }
            else if (!shouldCurveToPrev && CurveToPrev != null)
            {
                CurveToPrev.RemoveFromCanvas(addedToCanvas);
                _curveToPrev = null;
            }
        }

        public void AddToCanvas(IBezierContainer canvas)
        {
            addedToCanvas = canvas;
            //change the click handler container to the given canvas to make sure
            //the canvas didn't change in between 
            pointClickHandler.container = (IInputElement)canvas;
            tangentClickHandler.container = (IInputElement)canvas;
            //hook to click handler
            pointClickHandler.Hook(visualPoint);
            tangentClickHandler.Hook(leftTangent);
            tangentClickHandler.Hook(rightTangent);

            //lines
            canvas.AddToRender(lineToLeft);
            canvas.AddToRender(lineToRight);
            //tangent circles
            canvas.AddToRender(leftTangent);
            canvas.AddToRender(rightTangent);
            //central circle
            canvas.AddToRender(visualPoint);

            //curves if not null
            CurveToNext?.AddToCanvas(canvas);
            if(shouldCurveToPrev)
                CurveToPrev?.AddToCanvas(canvas);
        }

        public void RemoveFromCanvas(IBezierContainer canvas)
        {
            addedToCanvas = null;
            //unhook from handler, but no need to set the container since 
            //we unhooked
            pointClickHandler.UnHook(visualPoint);
            tangentClickHandler.UnHook(leftTangent);
            tangentClickHandler.UnHook(rightTangent);

            canvas.RemoveFromRender(lineToLeft);
            canvas.RemoveFromRender(lineToRight);

            canvas.RemoveFromRender(leftTangent);
            canvas.RemoveFromRender(rightTangent);

            canvas.RemoveFromRender(visualPoint);

            //using the manual _curveTo* here because 
            //when a point is deleted the point.NextPoint is null and the curve would 
            //not be deleted from canvas
            if (_curveToNext != null)
                _curveToNext.RemoveFromCanvas(canvas);
            if (_curveToPrev != null)
                _curveToPrev.RemoveFromCanvas(canvas);
        }

        private void VisualPoint_DoDelete(BezierVisualItem obj)
        {
            if (obj == visualPoint)
                Deleter(point);
            if (obj == rightTangent)
            {
                point.RightHandleX = 0;
                point.RightHandleY = 0;
            }
            if (obj == leftTangent)
            {
                point.LeftHandleX = 0;
                point.LeftHandleY = 0;
            }
        }
    }
}
