using System.Collections.Generic;

namespace Motio.Undoing
{
    public class UndoStack
    {
        private static UndoStack _instance;
        public static UndoStack Instance => _instance ?? (_instance = new UndoStack());

        public static void Clear() => Instance._Clear();
        public static void Push(IUndoCommand command) => Instance._Push(command);
        public static void Undo() => Instance._Undo();
        public static void Redo() => Instance._Redo();
        public static bool CanRedo() => Instance._CanRedo();
        public static bool CanUndo() => Instance._CanUndo();



        private Stack<IUndoCommand> undoStack = new Stack<IUndoCommand>();
        private Stack<IUndoCommand> redoStack = new Stack<IUndoCommand>();

        public void _Push(IUndoCommand command)
        {
            undoStack.Push(command);
            //new action = redo stack is invalid
            redoStack.Clear();
        }

        public void _Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        public void _Undo()
        {
            if (undoStack.Count == 0)
                return;
            IUndoCommand command = undoStack.Pop();
            if (command == null)
                return;
            command.Undo();
            redoStack.Push(command);
        }

        public void _Redo()
        {
            if (redoStack.Count == 0)
                return;
            IUndoCommand command = redoStack.Pop();
            if (command == null)
                return;
            command.Redo();
            undoStack.Push(command);
        }

        public bool _CanUndo()
        {
            return undoStack.Count != 0;
        }

        public bool _CanRedo()
        {
            return redoStack.Count != 0;
        }
    }
}
