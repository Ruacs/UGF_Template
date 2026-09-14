using System;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public static class SubGameResourceLoader
    {
        public static UniTask<T> LoadConfigSOAsync<T>(string configSOName) where T : ScriptableObject
        {
            var completionSource = new UniTaskCompletionSource<T>();
            string assetName = AssetUtility.GetScriptableObjectAsset(configSOName);

            GameEntry.Resource.LoadAsset(assetName, typeof(T), new LoadAssetCallbacks(
                (loadedAssetName, asset, duration, userData) =>
                {
                    if (asset is T config)
                    {
                        completionSource.TrySetResult(config);
                        Log.Info("Load sub game ConfigSO '{0}' OK.", configSOName);
                        return;
                    }

                    completionSource.TrySetException(new InvalidCastException(
                        Utility.Text.Format("Sub game ConfigSO '{0}' from '{1}' is not '{2}'.", configSOName, loadedAssetName, typeof(T).Name)));
                },
                (loadedAssetName, status, errorMessage, userData) =>
                {
                    completionSource.TrySetException(new InvalidOperationException(
                        Utility.Text.Format("Can not load sub game ConfigSO '{0}' from '{1}' with error message '{2}'.", configSOName, loadedAssetName, errorMessage)));
                }));

            return completionSource.Task;
        }

        public static UniTask<T> LoadJsonConfigAsync<T>(string configName, bool fromBytes)
        {
            var completionSource = new UniTaskCompletionSource<T>();

            GameEntry.CustomConfig.LoadJsonConfig<T>(
                configName,
                fromBytes,
                config => completionSource.TrySetResult(config),
                errorMessage => completionSource.TrySetException(new InvalidOperationException(errorMessage)));

            return completionSource.Task;
        }
    }
}
