#if UNITY_WEBGL && DOUYIN_MINI_GAME
using YooAsset;

internal class DouYinFileSystemInitializeOperation : FSInitializeFileSystemOperation
{
    private readonly DouYinFileSystem _fileSystem;

    public DouYinFileSystemInitializeOperation(DouYinFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    internal override void InternalOnStart()
    {
        Status = EOperationStatus.Succeed;
    }

    internal override void InternalOnUpdate()
    {
    }
}
#endif