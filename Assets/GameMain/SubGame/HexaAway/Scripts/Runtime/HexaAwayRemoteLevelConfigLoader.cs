using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Lokas
{
    [Serializable]
    public sealed class HexaAwayRemoteLevelConfigManifest
    {
        public int version = 1;
        public HexaAwayRemoteLevelConfigItem[] configs;

        public HexaAwayRemoteLevelConfigItem[] Configs => configs ?? new HexaAwayRemoteLevelConfigItem[0];
    }

    [Serializable]
    public sealed class HexaAwayRemoteLevelConfigItem
    {
        public string id;
        public string name;
        public string file;
        public int levelCount;
        public long byteSize;
        public string updatedAt;

        public string Id => id;
        public string Name => name;
        public string File => file;
        public int LevelCount => levelCount;
        public long ByteSize => byteSize;
        public string UpdatedAt => updatedAt;
    }

    public static class HexaAwayRemoteLevelConfigLoader
    {
#if UNITY_EDITOR
        public const string DefaultBaseUrl = "http://localhost:5889";

#else
        public const string DefaultBaseUrl = "http://192.168.31.191:5889";

#endif
        public static UniTask<HexaAwayRemoteLevelConfigManifest> LoadManifestAsync(string baseUrl = DefaultBaseUrl, CancellationToken cancellationToken = default)
        {
            return GetJsonAsync<HexaAwayRemoteLevelConfigManifest>($"{NormalizeBaseUrl(baseUrl)}/level-configs/manifest.json", cancellationToken);
        }

        public static UniTask<CompactLevelConfig> LoadConfigAsync(string baseUrl, HexaAwayRemoteLevelConfigItem item, CancellationToken cancellationToken = default)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (string.IsNullOrWhiteSpace(item.file))
            {
                throw new ArgumentException("Remote level config file is empty.", nameof(item));
            }

            return LoadConfigAsync(baseUrl, item.file, cancellationToken);
        }

        public static UniTask<CompactLevelConfig> LoadConfigAsync(string baseUrl, string fileName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Remote level config file is empty.", nameof(fileName));
            }

            return GetJsonAsync<CompactLevelConfig>($"{NormalizeBaseUrl(baseUrl)}/level-configs/{UnityWebRequest.EscapeURL(fileName)}", cancellationToken);
        }

        private static async UniTask<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Cache-Control", "no-store");
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"Request remote HexaAway level config failed. Url='{url}', Error='{request.error}'");
            }

            T result = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
            if (result == null)
            {
                throw new JsonException($"Remote HexaAway level config parse result is null. Url='{url}'");
            }

            return result;
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return DefaultBaseUrl;
            }

            return baseUrl.Trim().TrimEnd('/');
        }
    }
}
