#if UNITY_WEBGL && DOUYIN_MINI_GAME
using YooAsset;

internal sealed class RequestDouYinPackageVersionOperation : AsyncOperationBase
{
    private readonly DouYinFileSystem _fileSystem;
    private readonly int _timeout;
    private int _requestCount;
    private ESteps _steps = ESteps.None;
    private UnityWebTextRequestOperation _webTextRequestOp;


    public RequestDouYinPackageVersionOperation(DouYinFileSystem fileSystem, int timeout)
    {
        _fileSystem = fileSystem;
        _timeout = timeout;
    }

    /// <summary>
    ///     包裹版本
    /// </summary>
    public string PackageVersion { private set; get; }

    internal override void InternalOnStart()
    {
        _requestCount = WebRequestCounter.GetRequestFailedCount(_fileSystem.PackageName, nameof(RequestDouYinPackageVersionOperation));
        _steps = ESteps.RequestPackageVersion;
    }

    internal override void InternalOnUpdate()
    {
        if (_steps == ESteps.None || _steps == ESteps.Done)
        {
            return;
        }

        if (_steps == ESteps.RequestPackageVersion)
        {
            if (_webTextRequestOp == null)
            {
                var fileName = YooAssetSettingsData.GetPackageVersionFileName(_fileSystem.PackageName);
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
                PackageVersion = _webTextRequestOp.Result;
                if (string.IsNullOrEmpty(PackageVersion))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Wechat package version file content is empty !";
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
                WebRequestCounter.RecordRequestFailed(_fileSystem.PackageName, nameof(RequestDouYinPackageVersionOperation));
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
        RequestPackageVersion,
        Done,
    }
}
#endif