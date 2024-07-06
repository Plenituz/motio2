namespace Motio.Undoing
{
    public interface IUndoCommand
    {
        void Undo();
        void Redo();
    }
}
