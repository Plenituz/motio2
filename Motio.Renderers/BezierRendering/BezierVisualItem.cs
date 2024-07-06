using Motio.Selecting;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace Motio.Renderers.BezierRendering
{
    public abstract class BezierVisualItem : CenteredEllipse, ISelectable
    {
        public readonly IBezierPoint point;
        public event Action<BezierVisualItem> DoDelete;
        public bool left = false;

        protected abstract Brush Color { get; }
        protected abstract Brush SelectedColor { get; }

        public BezierVisualItem(IBezierPoint point)
        {
            this.point = point;
        }

        public bool Delete()
        {
            DoDelete?.Invoke(this);
            return true;
        }
        public bool CanBeSelected => true;
        public abstract string DefaultSelectionGroup { get; }
        public abstract void KeyPressed(KeyEventArgs e);
        public void OnSelect() => Fill = SelectedColor;
        public void OnUnselect() => Fill = Color;
    }
}
