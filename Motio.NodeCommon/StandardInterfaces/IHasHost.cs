namespace Motio.NodeCommon.StandardInterfaces
{
    public interface IHasHost
    {
        object Host { get; set; }
    }

    public static class IHasHostExtensions
    {
        public static object FindRoot(this IHasHost on)
        {
            object host = on.Host;
            if (host is IHasHost nHost)
                return FindRoot(nHost);
            else
                return host;
        }
    }
}
