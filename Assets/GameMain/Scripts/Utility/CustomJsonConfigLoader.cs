using System;
using System.Text;
using GameFramework;
using GameFramework.Resource;
using Newtonsoft.Json;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public static class CustomJsonConfigLoader
    {
        public static void Load<T>(string configName, bool fromBytes, Action<T> onSuccess, Action<string> onFailure = null)
        {
            string assetName = AssetUtility.GetCustomConfigAsset(configName, fromBytes, "json");
            GameEntry.Resource.LoadAsset(assetName, typeof(TextAsset), new LoadAssetCallbacks(
                (loadedAssetName, asset, duration, userData) =>
                {
                    try
                    {
                        T config = Parse<T>((TextAsset)asset, fromBytes);
                        onSuccess?.Invoke(config);
                        Log.Info("Load custom json config '{0}' OK.", configName);
                    }
                    catch (Exception exception)
                    {
                        string errorMessage = Utility.Text.Format("Parse custom json config '{0}' from '{1}' failed: {2}", configName, loadedAssetName, exception.Message);
                        Log.Error(errorMessage);
                        onFailure?.Invoke(errorMessage);
                    }
                },
                (loadedAssetName, status, errorMessage, userData) =>
                {
                    string message = Utility.Text.Format("Can not load custom json config '{0}' from '{1}' with error message '{2}'.", configName, loadedAssetName, errorMessage);
                    Log.Error(message);
                    onFailure?.Invoke(message);
                }));
        }

        public static T Parse<T>(TextAsset textAsset, bool fromBytes)
        {
            if (textAsset == null)
                throw new ArgumentNullException(nameof(textAsset));

            byte[] bytes = textAsset.bytes;
            if (fromBytes)
            {
                bytes = JsonConfigObfuscator.Deobfuscate(bytes);
            }

            string json = Encoding.UTF8.GetString(bytes);
            T config = JsonConvert.DeserializeObject<T>(json);
            if (config == null)
                throw new JsonException("Custom json config parse result is null.");

            return config;
        }
    }
}
