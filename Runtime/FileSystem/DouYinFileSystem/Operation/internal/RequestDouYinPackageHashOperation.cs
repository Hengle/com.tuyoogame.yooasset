#if UNITY_WEBGL && DOUYIN_MINI_GAME
using YooAsset;

internal class RequestDouYinPackageHashOperation : AsyncOperationBase
{
    private readonly DouYinFileSystem _fileSystem;
    private readonly string _packageVersion;
    private readonly int _timeout;
    private int _requestCount;
    private ESteps _steps = ESteps.None;
    private UnityWebTextRequestOperation _webTextRequestOp;


    public RequestDouYinPackageHashOperation(DouYinFileSystem fileSystem, string packageVersion, int timeout)
    {
        _fileSystem = fileSystem;
        _packageVersion = packageVersion;
        _timeout = timeout;
    }

    /// <summary>
    ///     包裹哈希值
    /// </summary>
    public string PackageHash { private set; get; }

    internal override void InternalOnStart()
    {
        _requestCount = WebRequestCounter.GetRequestFailedCount(_fileSystem.PackageName, nameof(RequestDouYinPackageHashOperation));
        _steps = ESteps.RequestPackageHash;
    }

    internal override void InternalOnUpdate()
    {
        if (_steps == ESteps.None || _steps == ESteps.Done)
        {
            return;
        }

        if (_steps == ESteps.RequestPackageHash)
        {
            if (_webTextRequestOp == null)
            {
                var fileName = YooAssetSettingsData.GetPackageHashFileName(_fileSystem.PackageName, _packageVersion);
                var url = GetRequestURL(fileName);
                _webTextRequestOp = new UnityWebTextRequestOperation(url, _timeout);
                OperationSystem.StartOperation(_fileSystem.PackageName, _webTextRequestOp);
            }

            Progress = _webTextRequestOp.Progress;
            if (_webTextRequestOp.IsDone == false)
            {
                return;
            }

            if (_webTextRequestOp.Status == EOperationStatus.Succeed)
            {
                PackageHash = _webTextRequestOp.Result;
                if (string.IsNullOrEmpty(PackageHash))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Wechat package hash file content is empty !";
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = _webTextRequestOp.Error;
                WebRequestCounter.RecordRequestFailed(_fileSystem.PackageName, nameof(RequestDouYinPackageHashOperation));
            }
        }
    }

    private string GetRequestURL(string fileName)
    {
        // 轮流返回请求地址
        if (_requestCount % 2 == 0)
        {
            return _fileSystem.RemoteServices.GetRemoteMainURL(fileName);
        }

        return _fileSystem.RemoteServices.GetRemoteFallbackURL(fileName);
    }

    private enum ESteps
    {
        None,
        RequestPackageHash,
        Done,
    }
}
#endif