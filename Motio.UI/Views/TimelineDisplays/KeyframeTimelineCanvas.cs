using Motio.Selecting;
using System.Windows.Controls;

namespace Motio.UI.Views.TimelineDisplays
{
    class KeyframeTimelineCanvas : Canvas, ISelectionClearer
    {
        public string[] HyperGroupsToClear => new[]
        {
            Selection.KEYFRAMES_TIMELINE
        };
    }
}
