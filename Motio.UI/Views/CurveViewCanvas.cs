using Motio.Renderers.BezierRendering;
using Motio.Selecting;
using Motio.UI.Renderers.KeyframeCurveRendering;
using Motio.UI.Renderers.KeyframeRendering;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views
{
    /// <summary>
    /// CurveViewCanvas is just a canvas that implements ISelectionClearer for the Global selection system
    /// </summary>
    public class CurveViewCanvas : Canvas, ISelectionClearer, IBezierContainer
    {
        public string[] HyperGroupsToClear => new[]
        {
            Selection.KEYFRAME_CURVES
        };

        public void AddToRender(FrameworkElement element)
        {
            Children.Add(element);
        }

        public void RemoveFromRender(FrameworkElement element)
        {
            Children.Remove(element);
        }
    }
}
