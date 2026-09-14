using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public static class SubGameAssetRegistry
    {
        private sealed class State
        {
            public readonly HashSet<string> Games = new(StringComparer.Ordinal);
            public readonly Dictionary<string, string> UIForms = new(StringComparer.Ordinal);
            public readonly Dictionary<string, string> Scenes = new(StringComparer.Ordinal);
            public readonly Dictionary<string, SubGameScriptableObjectAssetLocation> ScriptableObjects = new(StringComparer.Ordinal);
        }

        private static State s_State = new();
        public static void Initialize() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => s_State = new State();

        public static void RegisterConfig(SubGameAssetRegistryConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.HasLegacyEntries)
                throw new InvalidOperationException("Legacy subgame entries need migration in Tools/SubGame/Asset Registry Config.");
            Rebuild(config.Manifests);
        }

        // 先完整验证并构建，再替换运行索引；配置错误不会留下半份新映射。
        public static void Rebuild(IEnumerable<SubGameAssetManifest> manifests) => s_State = BuildState(manifests);
        public static void ValidateManifests(IEnumerable<SubGameAssetManifest> manifests) => BuildState(manifests);

        private static State BuildState(IEnumerable<SubGameAssetManifest> manifests)
        {
            if (manifests == null) throw new ArgumentNullException(nameof(manifests));
            var state = new State();
            int index = 0;
            foreach (var manifest in manifests)
            {
                if (manifest == null) throw new InvalidOperationException($"Missing subgame manifest at index {index}.");
                var entry = manifest.Entry;
                string game = entry?.GameName;
                ValidateName(game, "game");
                if (game.Contains("/")) throw new InvalidOperationException($"Game name cannot contain a path: '{game}'.");
                if (!state.Games.Add(game))
                    throw new InvalidOperationException($"Duplicate game '{game}' in manifest '{manifest.name}' (index {index}).");

                if (entry.UIForms == null || entry.Scenes == null || entry.ScriptableObjects == null)
                    throw new InvalidOperationException($"Missing list in manifest '{manifest.name}' for '{game}'.");
                foreach (string assetName in entry.UIForms) AddUnique(state.UIForms, assetName, game, "UI");
                foreach (string assetName in entry.Scenes) AddUnique(state.Scenes, assetName, game, "scene");
                foreach (var so in entry.ScriptableObjects)
                {
                    if (so == null) throw new InvalidOperationException($"Missing SO entry in '{game}'.");
                    ValidateName(so.AssetName, "SO");
                    ValidateName(so.Category, "SO category");
                    if (state.ScriptableObjects.TryGetValue(so.AssetName, out var previous))
                        throw new InvalidOperationException($"Duplicate SO '{so.AssetName}': '{previous.GameName}' and '{game}'.");
                    state.ScriptableObjects.Add(so.AssetName, new SubGameScriptableObjectAssetLocation(game, so.Category));
                }
                index++;
            }
            return state;
        }

        private static void ValidateName(string value, string kind)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Contains("\\") || value.Contains(":"))
                throw new InvalidOperationException($"Invalid {kind} name '{value}'.");
            foreach (string part in value.Split('/'))
                if (string.IsNullOrEmpty(part) || part == "." || part == "..")
                    throw new InvalidOperationException($"Invalid {kind} path '{value}'.");
        }

        private static void AddUnique(Dictionary<string, string> map, string assetName, string game, string kind)
        {
            ValidateName(assetName, kind);
            if (map.TryGetValue(assetName, out string previous))
                throw new InvalidOperationException($"Duplicate {kind} '{assetName}': '{previous}' and '{game}'.");
            map.Add(assetName, game);
        }

        // 保留已有显式注册入口；同值重复调用幂等，跨游戏/类别冲突不再静默覆盖。
        public static void RegisterSubGame(string gameName)
        {
            ValidateName(gameName, "game");
            if (gameName.Contains("/")) throw new InvalidOperationException("Game name must be a single path segment.");
            s_State.Games.Add(gameName);
        }

        public static void RegisterUIForm(string assetName, string gameName) =>
            RegisterMapping(s_State.UIForms, assetName, gameName, "UI");
        public static void RegisterScene(string assetName, string gameName) =>
            RegisterMapping(s_State.Scenes, assetName, gameName, "scene");

        private static void RegisterMapping(Dictionary<string, string> map, string assetName, string gameName, string kind)
        {
            ValidateName(assetName, kind);
            if (map.TryGetValue(assetName, out string previous) && previous != gameName)
                throw new InvalidOperationException($"Duplicate {kind} '{assetName}': '{previous}' and '{gameName}'.");
            RegisterSubGame(gameName);
            map[assetName] = gameName;
        }

        public static void RegisterScriptableObject(string assetName, string gameName, string category)
        {
            ValidateName(assetName, "SO");
            ValidateName(category, "SO category");
            if (s_State.ScriptableObjects.TryGetValue(assetName, out var previous) &&
                (previous.GameName != gameName || previous.Category != category))
                throw new InvalidOperationException($"Duplicate SO '{assetName}': '{previous.GameName}/{previous.Category}' and '{gameName}/{category}'.");
            RegisterSubGame(gameName);
            s_State.ScriptableObjects[assetName] = new SubGameScriptableObjectAssetLocation(gameName, category);
        }

        public static bool UnregisterSubGame(string gameName)
        {
            if (string.IsNullOrEmpty(gameName) || !s_State.Games.Contains(gameName)) return false;
            var next = new State();
            foreach (var game in s_State.Games) if (game != gameName) next.Games.Add(game);
            foreach (var item in s_State.UIForms) if (item.Value != gameName) next.UIForms.Add(item.Key, item.Value);
            foreach (var item in s_State.Scenes) if (item.Value != gameName) next.Scenes.Add(item.Key, item.Value);
            foreach (var item in s_State.ScriptableObjects)
                if (item.Value.GameName != gameName) next.ScriptableObjects.Add(item.Key, item.Value);
            s_State = next;
            return true;
        }

        public static bool TryGetUIFormGameName(string assetName, out string gameName)
        {
            gameName = null;
            return assetName != null && s_State.UIForms.TryGetValue(assetName, out gameName);
        }

        public static bool TryGetSceneGameName(string assetName, out string gameName)
        {
            gameName = null;
            return assetName != null && s_State.Scenes.TryGetValue(assetName, out gameName);
        }

        public static bool TryGetScriptableObjectLocation(string assetName, out string gameName, out string category)
        {
            gameName = null;
            category = null;
            if (assetName == null || !s_State.ScriptableObjects.TryGetValue(assetName, out var location)) return false;
            gameName = location.GameName;
            category = location.Category;
            return true;
        }

        public static bool TryParseScopedAssetName(string assetName, out string gameName, out string relativeAssetName)
        {
            gameName = null;
            relativeAssetName = null;
            if (string.IsNullOrEmpty(assetName)) return false;
            int separatorIndex = assetName.IndexOf('/');
            if (separatorIndex <= 0 || separatorIndex >= assetName.Length - 1) return false;
            string candidate = assetName.Substring(0, separatorIndex);
            if (!s_State.Games.Contains(candidate)) return false;
            gameName = candidate;
            relativeAssetName = assetName.Substring(separatorIndex + 1);
            return true;
        }

        private readonly struct SubGameScriptableObjectAssetLocation
        {
            public SubGameScriptableObjectAssetLocation(string gameName, string category)
            {
                GameName = gameName;
                Category = category;
            }
            public string GameName { get; }
            public string Category { get; }
        }
    }
}
