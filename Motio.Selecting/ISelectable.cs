using System.Windows.Input;

namespace Motio.Selecting
{
    public interface ISelectable
    {
        bool CanBeSelected { get; }
        /// <summary>
        /// selection group this selectable will be added to by the selection rect
        /// </summary>
        string DefaultSelectionGroup { get; }
        void OnSelect();
        void OnUnselect();
        bool Delete();
        void KeyPressed(KeyEventArgs e);
    }
}
