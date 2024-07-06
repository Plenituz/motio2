namespace Motio.NodeCommon.Utils
{
    public interface IChainCache
    {
        int Count { get; }
        void Add(ICacheMember member);
        void AddAt(ICacheMember member, int index);
        void Remove(int index);
        void Move(int fromIndex, int toIndex);
        void RemoveAll();

        void StartSingleFrame(int frame, int nodeIndex, int endNode = -1);
        void StartBatchCalculate(int startFrame, int endFrame, int nodeIndex);
        void StopCalculating();

        bool IsFullyCached(int frame);
        DataFeed GetCache(int nodeIndex, int frame);
        bool TryGetCache(int nodeIndex, int frame, out DataFeed dataFeed);

        void ClearCacheAfter(int nodeIndex, int frame);
        void ClearAllFramesAfter(int nodeIndex);
    }
}
