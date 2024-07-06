using System.Windows.Input;

namespace Motio.UI
{
    public class CustomCommands
    {
        public static readonly RoutedUICommand Undo = new RoutedUICommand(
            "Undo", "Undo", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.Z, ModifierKeys.Control) });

        public static readonly RoutedUICommand Redo = new RoutedUICommand(
            "Redo", "Redo", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.Y, ModifierKeys.Control), new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand PlayPause = new RoutedUICommand(
            "Play/Pause", "Play/Pause", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.Space) });

        public static readonly RoutedUICommand SwitchKeyframeClip = new RoutedUICommand(
            "Switch keyframe/clip view", "Switch keyframe/clip view", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.Tab) });

        public static readonly RoutedUICommand Save = new RoutedUICommand(
            "Save", "Save", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control) });

        public static readonly RoutedUICommand Open = new RoutedUICommand(
            "Open", "Open", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.O, ModifierKeys.Control) });

        public static readonly RoutedUICommand New = new RoutedUICommand(
            "New", "New", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.N, ModifierKeys.Control) });

        public static readonly RoutedUICommand OpenConsole = new RoutedUICommand(
            "Open Console", "Open Console", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand SaveSelection = new RoutedUICommand(
            "Save Selection", "Save Selection", typeof(CustomCommands));

        public static readonly RoutedUICommand ExportToImageSequence = new RoutedUICommand(
            "Export to Image Sequence", "Export to Image Sequence", typeof(CustomCommands));

        public static readonly RoutedUICommand NextFrame = new RoutedUICommand(
            "Next Frame", "Next Frame", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.Right) });

        public static readonly RoutedUICommand PrevFrame = new RoutedUICommand(
            "Prev Frame", "Prev Frame", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.Left) });

        public static readonly RoutedUICommand NewGraphicsNode = new RoutedUICommand(
            "New Graphics Node", "New Graphics Node", typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift) });
    }
}
