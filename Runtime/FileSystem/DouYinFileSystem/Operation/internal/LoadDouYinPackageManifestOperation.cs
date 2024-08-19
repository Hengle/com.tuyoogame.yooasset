#if UNITY_WEBGL && DOUYIN_MINI_GAME
using YooAsset;

internal sealed class LoadDouYinPackageManifestOperation : AsyncOperationBase
{
    private readonly DouYinFileSystem _fileSystem;
    private readonly string _packageHash;
    private readonly string _packageVersion;
    private readonly int _timeout;
    private DeserializeManifestOperation _deserializer;
    private int _requestCount;
    private ESteps _steps = ESteps.None;
    private UnityWebDataRequestOperation _webDataRequestOp;

    internal LoadDouYinPackageManifestOperation(DouYinFileSystem fileSystem, string packageVersion, string packageHash, int timeout)
    {
        _fileSystem = fileSystem;
        _packageVersion = packageVersion;
        _packageHash = packageHash;
        _timeout = timeout;
    }

    /// <summary>
    ///     包裹清单
    /// </summary>
    public PackageManifest Manifest { private set; get; }

    internal override void InternalOnStart()
    {
        _requestCount = WebRequestCounter.GetRequestFailedCount(_fileSystem.PackageName, nameof(LoadDouYinPackageManifestOperation));
        _steps = ESteps.RequestFileData;
    }

    internal override void InternalOnUpdate()
    {
        if (_steps == ESteps.None || _steps == ESteps.Done)
        {
            return;
        }

        if (_steps == ESteps.RequestFileData)
        {
            if (_webDataRequestOp == null)
            {
                var fileName = YooAssetSettingsData.GetManifestBinaryFileName(_fileSystem.PackageName, _packageVersion);
                var url = GetRequestURL(fileName);
                _webDataRequestOp = new UnityWebDataRequestOperation(url, _timeout);
                OperationSystem.StartOperation(_fileSystem.PackageName, _webDataRequestOp);
            }

            Progress = _webDataRequestOp.Progress;
            if (_webDataRequestOp.IsDone == false)
            {
                return;
            }

            if (_webDataRequestOp.Status == EOperationStatus.Succeed)
            {
                _steps = ESteps.VerifyFileData;
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = _webDataRequestOp.Error;
                WebRequestCounter.RecordRequestFailed(_fileSystem.PackageName, nameof(LoadDouYinPackageManifestOperation));
            }
        }

        if (_steps == ESteps.VerifyFileData)
        {
            var fileHash = HashUtility.BytesMD5(_webDataRequestOp.Result);
            if (fileHash == _packageHash)
            {
                _steps = ESteps.LoadManifest;
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Failed to verify douyin package manifest file!";
            }
        }

        if (_steps == ESteps.LoadManifest)
        {
            if (_deserializer == null)
            {
                _deserializer = new DeserializeManifestOperation(_webDataRequestOp.Result);
                OperationSystem.StartOperation(_fileSystem.PackageName, _deserializer);
            }

            Progress = _deserializer.Progress;
            if (_deserializer.IsDone == false)
            {
                return;
            }

            if (_deserializer.Status == EOperationStatus.Succeed)
            {
                _steps = ESteps.Done;
                Manifest = _deserializer.Manifest;
                Status = EOperationStatus.Succeed;
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = _deserializer.Error;
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
        RequestFileData,
        VerifyFileData,
        LoadManifest,
        Done,
    }
}
#endif