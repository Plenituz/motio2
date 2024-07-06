namespace Motio.UI.Utils
{
    public interface IProxy<OriginalType>
    {
        OriginalType Original { get; }
    }
}