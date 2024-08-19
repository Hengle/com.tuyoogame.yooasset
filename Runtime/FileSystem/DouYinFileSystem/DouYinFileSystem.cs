#if UNITY_WEBGL && DOUYIN_MINI_GAME
using System;
using System.Collections.Generic;
using StarkSDKSpace;
using UnityEngine;
using YooAsset;

public static class DouYinFileSystemCreater
{
    public static FileSystemParameters CreateDouYinFileSystemParameters(IRemoteServices remoteServices)
    {
        var fileSystemClass = typeof(DouYinFileSystem).FullName;
        var fileSystemParams = new FileSystemParameters(fileSystemClass, null);
        fileSystemParams.AddParameter("REMOTE_SERVICES", remoteServices);
        return fileSystemParams;
    }
}

/// <summary>
///     微信小游戏文件系统
///     参考：https://wechat-miniprogram.github.io/minigame-unity-webgl-transform/Design/UsingAssetBundle.html
/// </summary>
internal class DouYinFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _douYinFilePaths = new(10000);
    private StarkFileSystemManager _douYinFileSystemMgr;


    #region 自定义参数

    /// <summary>
    ///     自定义参数：远程服务接口
    /// </summary>
    public IRemoteServices RemoteServices { private set; get; }

    #endregion

    /// <summary>
    ///     包裹名称
    /// </summary>
    public string PackageName { private set; get; }

    /// <summary>
    ///     文件根目录
    /// </summary>
    public string FileRoot { get; private set; } = string.Empty;

    /// <summary>
    ///     文件数量
    /// </summary>
    public int FileCount
    {
        get { return 0; }
    }

    public virtual FSInitializeFileSystemOperation InitializeFileSystemAsync()
    {
        var operation = new DouYinFileSystemInitializeOperation(this);
        OperationSystem.StartOperation(PackageName, operation);
        return operation;
    }

    public virtual FSLoadPackageManifestOperation LoadPackageManifestAsync(string packageVersion, int timeout)
    {
        var operation = new DouYinFileSystemLoadPackageManifestOperation(this, packageVersion, timeout);
        OperationSystem.StartOperation(PackageName, operation);
        return operation;
    }

    public virtual FSRequestPackageVersionOperation RequestPackageVersionAsync(bool appendTimeTicks, int timeout)
    {
        var operation = new DouYinFileSystemRequestPackageVersionOperation(this, timeout);
        OperationSystem.StartOperation(PackageName, operation);
        return operation;
    }

    public virtual FSClearAllBundleFilesOperation ClearAllBundleFilesAsync()
    {
        var operation = new FSClearAllBundleFilesCompleteOperation();
        OperationSystem.StartOperation(PackageName, operation);
        return operation;
    }

    public virtual FSClearUnusedBundleFilesOperation ClearUnusedBundleFilesAsync(PackageManifest manifest)
    {
        var operation = new FSClearUnusedBundleFilesCompleteOperation();
        OperationSystem.StartOperation(PackageName, operation);
        return operation;
    }

    public virtual FSDownloadFileOperation DownloadFileAsync(PackageBundle bundle, DownloadParam param)
    {
        param.MainURL = RemoteServices.GetRemoteMainURL(bundle.FileName);
        param.FallbackURL = RemoteServices.GetRemoteFallbackURL(bundle.FileName);
        var operation = new DouYinFileSystemDownloadFileOperation(this, bundle, param);
        OperationSystem.StartOperation(PackageName, operation);
        return operation;
    }

    public virtual FSLoadBundleOperation LoadBundleFile(PackageBundle bundle)
    {
        var operation = new DouYinFileSystemLoadBundleOperation(this, bundle);
        OperationSystem.StartOperation(PackageName, operation);
        return operation;
    }

    public virtual void UnloadBundleFile(PackageBundle bundle, object result)
    {
        var assetBundle = result as AssetBundle;
        if (assetBundle != null)
        {
            assetBundle.Unload(true);
        }
    }

    public virtual void SetParameter(string name, object value)
    {
        if (name == "REMOTE_SERVICES")
        {
            RemoteServices = (IRemoteServices)value;
        }
        else
        {
            YooLogger.Warning($"Invalid parameter : {name}");
        }
    }

    public virtual void OnCreate(string packageName, string rootDirectory)
    {
        PackageName = packageName;

        // 注意：CDN服务未启用的情况下，使用微信WEB服务器
        if (RemoteServices == null)
        {
            var webRoot = PathUtility.Combine(Application.streamingAssetsPath, YooAssetSettingsData.Setting.DefaultYooFolderName, packageName);
            RemoteServices = new WebRemoteServices(webRoot);
        }

        _douYinFileSystemMgr = StarkSDK.API.GetStarkFileSystemManager();
        FileRoot = Application.persistentDataPath + "/" + YooAssetSettingsData.Setting.DefaultYooFolderName + "/"; //注意：如果有子目录，请修改此处！
    }

    public virtual void OnUpdate()
    {
    }

    public virtual bool Belong(PackageBundle bundle)
    {
        return true;
    }

    public virtual bool Exists(PackageBundle bundle)
    {
        var filePath = GetDouYinFileLoadPath(bundle);
        var result = _douYinFileSystemMgr.AccessSync(filePath);
        return result;
    }

    public virtual bool NeedDownload(PackageBundle bundle)
    {
        if (Belong(bundle) == false)
        {
            return false;
        }

        return Exists(bundle) == false;
    }

    public virtual bool NeedUnpack(PackageBundle bundle)
    {
        return false;
    }

    public virtual bool NeedImport(PackageBundle bundle)
    {
        return false;
    }

    public virtual byte[] ReadFileData(PackageBundle bundle)
    {
        throw new NotImplementedException();
    }

    public virtual string ReadFileText(PackageBundle bundle)
    {
        throw new NotImplementedException();
    }

    #region 内部方法

    private string GetDouYinFileLoadPath(PackageBundle bundle)
    {
        if (_douYinFilePaths.TryGetValue(bundle.BundleGUID, out var filePath) == false)
        {
            filePath = PathUtility.Combine(FileRoot, bundle.FileName);
            _douYinFilePaths.Add(bundle.BundleGUID, filePath);
        }

        return filePath;
    }

    #endregion

    private class WebRemoteServices : IRemoteServices
    {
        protected readonly Dictionary<string, string> _mapping = new(10000);
        private readonly string _webPackageRoot;

        public WebRemoteServices(string buildinPackRoot)
        {
            _webPackageRoot = buildinPackRoot;
        }

        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return GetFileLoadURL(fileName);
        }

        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return GetFileLoadURL(fileName);
        }

        private string GetFileLoadURL(string fileName)
        {
            if (_mapping.TryGetValue(fileName, out var url) == false)
            {
                var filePath = PathUtility.Combine(_webPackageRoot, fileName);
                url = DownloadSystemHelper.ConvertToWWWPath(filePath);
                _mapping.Add(fileName, url);
            }

            return url;
        }
    }
}
#endif