using Motio.Undoing;

namespace Motio.Pathing.Undo
{
    public class ClosePathCommand : IUndoCommand
    {
        private readonly Path path;
        private readonly bool revert;

        public ClosePathCommand(Path path, bool revert = false)
        {
            this.path = path;
            this.revert = revert;
        }

        public void Redo()
        {
            path.noUndo = true;
            path.Closed = revert;
            path.noUndo = false;
        }

        public void Undo()
        {
            path.noUndo = true;
            path.Closed = !revert;
            path.noUndo = false;
        }
    }
}
