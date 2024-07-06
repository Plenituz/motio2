using Motio.Undoing;

namespace Motio.Pathing.Undo
{
    public class AddToPathCommand : IUndoCommand
    {
        private readonly Path path;
        private readonly PathPoint point;
        private readonly int index;

        public AddToPathCommand(PathPoint point, int index)
        {
            this.point = point;
            this.path = point.Host;
            this.index = index;
        }

        public void Redo()
        {
            path.noUndo = true;
            path.Points.Insert(index, point);
            path.noUndo = false;
        }

        public void Undo()
        {
            path.noUndo = true;
            path.Points.RemoveAt(index);
            path.noUndo = false;
        }
    }
}
