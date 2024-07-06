using Motio.Undoing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Motio.Pathing.Undo
{
    public class RemoveFromPathCommand : IUndoCommand
    {
        private readonly Path path;
        private readonly PathPoint point;
        private readonly int index;

        public RemoveFromPathCommand(PathPoint point, int index)
        {
            this.point = point;
            this.path = point.Host;
            this.index = index;
        }

        public void Redo()
        {
            path.noUndo = true;
            path.Points.RemoveAt(index);
            path.noUndo = false;
        }

        public void Undo()
        {
            path.noUndo = true;
            path.Points.Insert(index, point);
            path.noUndo = false;
        }
    }
}
