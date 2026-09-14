using GameFramework.Resource;
using Lokas;
using System;
using Log = UnityGameFramework.Runtime.Log;

/// <summary>
/// 预下载管理器使用说明：
/// 比如有100个关卡，分成5个SubPack，每个SubPack20个关卡
/// 那么5个SubPack中，除了每个包中需要添加20个关卡预制体外，还需要添加一个flag标签资源（每个SubPack一个）
/// 如：
/// SubPack1 包含： Level_1.prefab ~ Level_20.prefab + SubPack1_Flag.prefab
/// 
/// 使用预下载管理器可参考 DowmLoadTest() 方法
/// </summary>


public class PreDownLoadManager : UnitySingleton<PreDownLoadManager>
{
    /// <summary>
    /// 手动预下载 AssetBundle（通过 每个SubPack中的flag资源 触发）
    /// </summary>
    public void DownLoad(string subPackFlagName,PreDownloadCallback callback = null)
    {
        if(string.IsNullOrEmpty(subPackFlagName))
        {
            Log.Error("[PreDownload] 下载失败：subPackFlagName 为空");
            callback?.OnFail?.Invoke("subPackFlagName is null or empty");
            return;
        }

        GameEntry.Resource.LoadAsset(subPackFlagName, typeof(object),
           new LoadAssetCallbacks(
               (name, asset, duration, data) =>
               {
                  Log.Info($"[PreDownload] 下载完成：{name},Load Time: {duration}s");
                   callback?.OnSuccess?.Invoke();
               },
               (name, status, errorMessage, data) =>
               {
                   Log.Error($"[PreDownload] 下载失败：{name}, error={errorMessage}");
                   callback?.OnFail?.Invoke(errorMessage);
               }
           )
       );
    }

    void DowmLoadTest()
    {
        PreDownloadCallback callback = new PreDownloadCallback(
            onSuccess: () =>
            {
                Log.Info("[PreDownloadTest] 下载成功");
            },
            onFail: (error) =>
            {
                Log.Info("[PreDownloadTest] 下载失败回调，error=" + error);
            }
        );


        DownLoad(AssetUtility.GetEntityAsset("SubPack1_Flag"), callback);
    }

}


public class PreDownloadCallback
{
    /// <summary>
    /// 下载完成
    /// </summary>
    public Action OnSuccess;

    /// <summary>
    /// 下载失败（带错误信息）
    /// </summary>
    public Action<string> OnFail;

    public PreDownloadCallback(Action onSuccess = null, Action<string> onFail = null)
    {
        OnSuccess = onSuccess;
        OnFail = onFail;
    }
}