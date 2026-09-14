using System;
using System.Collections.Generic;
using GameFramework.Localization;
using GameFramework.Resource;
using Lokas;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameEntry = Lokas.GameEntry;
/// <summary>
/// TMP 字体 GF 组件：切换语言时广播 Profile 变化，并缓存运行时动态生成的材质。
/// 挂在 GameFramework GameObject 上，通过 GameEntry.TMPFont 访问。
/// </summary>
public class TMPFontComponent : GameFrameworkComponent
{
    public static event Action<TMPFontProfile> OnFontProfileChanged;

    [SerializeField] private string _DefaultFontName = "Main";
    [SerializeField] private TMPLanguageFontConfig _config;

    [SerializeField] private TMPFontProfile _currentProfile;
    [SerializeField] private TMP_FontAsset _currentFontAsset;
    [SerializeField] private TMP_FontAsset _baseFontAsset;
    [SerializeField] private int _loadVersion;
    private readonly Dictionary<string, TMP_FontAsset> _fontAssetCache = new();
    private readonly Dictionary<string, List<(Action<TMP_FontAsset> onSuccess, Action onFailure)>> _fontAssetLoadRequests = new();
    private readonly Dictionary<(TMP_FontAsset, FontStylePreset), Material> _cache = new();


    public void SetLanguageConfig(TMPLanguageFontConfig config, Action onComplete = null)
    {
        _config = config;
        SwitchLanguage(GameEntry.Localization.Language, onComplete);
    }


    public List<LanguageEntry> GetAvailableLanguages()
    {
        return _config != null && _config.languageProfiles != null
            ? new List<LanguageEntry>(_config.languageProfiles)
            : new List<LanguageEntry>();
    }

    protected override void Awake()
    {
        base.Awake();
    }

    void OnDestroy()
    {
        foreach (var mat in _cache.Values)
        {
            if (mat != null) Destroy(mat);
        }
        _cache.Clear();
    }

    public void SwitchLanguage(Language language, Action onComplete = null)
    {
        if (_config == null)
        {
            Log.Error("[TMPFontComponent] TMPLanguageFontConfig is not assigned.");
            onComplete?.Invoke();
            return;
        }
        LanguageEntry entry = _config.GetEntry(language) ?? _config.GetEntry(Language.English);
        if (entry == null || string.IsNullOrEmpty(entry.fontAssetName))
        {
            Log.Error("[TMPFontComponent] No font asset is configured for language {0}.", language);
            onComplete?.Invoke();
            return;
        }

        TMPFontProfile profile = entry.fontProfile != null ? entry.fontProfile : _config.defaultProfile;
        int version = ++_loadVersion;
        string assetName = entry.fontAssetName;

        LoadFontAsset(_DefaultFontName, version, mainFont =>
        {
            _baseFontAsset = mainFont;
            if (assetName == _DefaultFontName)
            {
                _currentFontAsset = mainFont;
                ApplyProfile(profile);
                onComplete?.Invoke();
                return;
            }

            LoadFontAsset(assetName, version, fontAsset =>
            {
                AddFallback(fontAsset, _baseFontAsset);
                _currentFontAsset = fontAsset;
                ApplyProfile(profile);
                onComplete?.Invoke();
            }, onComplete);
        }, onComplete);
    }

    private void LoadFontAsset(string assetName, int version, Action<TMP_FontAsset> onSuccess, Action onFailure = null)
    {
        LoadFontAsset(assetName, fontAsset =>
        {
            if (version != _loadVersion)
                return;

            onSuccess?.Invoke(fontAsset);
        }, onFailure);
    }

    public void LoadFontAsset(string assetName, Action<TMP_FontAsset> onSuccess, Action onFailure = null)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            onFailure?.Invoke();
            return;
        }

        if (_fontAssetCache.TryGetValue(assetName, out TMP_FontAsset cachedFont) && cachedFont != null)
        {
            onSuccess?.Invoke(cachedFont);
            return;
        }

        if (_fontAssetLoadRequests.TryGetValue(assetName, out var requests))
        {
            requests.Add((onSuccess, onFailure));
            return;
        }

        requests = new List<(Action<TMP_FontAsset> onSuccess, Action onFailure)> { (onSuccess, onFailure) };
        _fontAssetLoadRequests.Add(assetName, requests);

        string assetPath = AssetUtility.GetTMPFontAsset(assetName, true);
        GameEntry.Resource.LoadAsset(assetPath, typeof(TMP_FontAsset), new LoadAssetCallbacks(
            (loadedAssetName, asset, duration, userData) =>
            {
                TMP_FontAsset fontAsset = asset as TMP_FontAsset;
                if (fontAsset == null)
                {
                    Log.Error("[TMPFontComponent] Loaded resource {0} is not a TMP_FontAsset.", loadedAssetName);
                    InvokeFontAssetLoadFailure(assetName);
                    return;
                }

                _fontAssetCache[assetName] = fontAsset;
                if (!_fontAssetLoadRequests.TryGetValue(assetName, out var callbacks))
                    return;

                _fontAssetLoadRequests.Remove(assetName);
                foreach (var callback in callbacks)
                {
                    callback.onSuccess?.Invoke(fontAsset);
                }
            },
            (loadedAssetName, status, errorMessage, userData) =>
            {
                Log.Error("[TMPFontComponent] Failed to load font: {0}, {1}", loadedAssetName, errorMessage);
                InvokeFontAssetLoadFailure(assetName);
            }));
    }

    private void InvokeFontAssetLoadFailure(string assetName)
    {
        if (!_fontAssetLoadRequests.TryGetValue(assetName, out var callbacks))
            return;

        _fontAssetLoadRequests.Remove(assetName);
        foreach (var callback in callbacks)
        {
            callback.onFailure?.Invoke();
        }
    }

    private static void AddFallback(TMP_FontAsset fontAsset, TMP_FontAsset fallback)
    {
        if (fontAsset == null || fallback == null || fontAsset == fallback)
            return;

        if (fontAsset.fallbackFontAssetTable == null)
            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();

        if (!fontAsset.fallbackFontAssetTable.Contains(fallback))
            fontAsset.fallbackFontAssetTable.Add(fallback);
    }

    public void ApplyProfile(TMPFontProfile profile)
    {
        if (profile == null) return;
        _currentProfile = profile;
        UGuiForm.SetMainFont(_currentFontAsset);
        OnFontProfileChanged?.Invoke(_currentProfile);
    }

    public TMPFontProfile GetCurrentProfile() => _currentProfile;

    public TMP_FontAsset GetCurrentFontAsset() => _currentFontAsset;
    /// <summary>
    /// 通过 styleKey 返回当前 Profile 下对应的材质；styleKey 找不到时降级为 fontAsset 默认材质。
    /// </summary>
    public Material GetMaterialByStyleKey(string styleKey)
    {
        if (_currentProfile == null) return null;
        var preset = _currentProfile.GetPreset(styleKey);
        return GetOrCreateMaterial(_currentFontAsset, preset);
    }

    /// <summary>
    /// 返回 (fontAsset + preset) 对应的材质，首次调用时动态创建并缓存。
    /// preset 为 null 时直接返回 fontAsset 的原始材质（不创建副本）。
    /// </summary>
    public Material GetOrCreateMaterial(TMP_FontAsset fontAsset, FontStylePreset preset)
    {
        if (fontAsset == null) return null;
        if (preset == null) return fontAsset.material;

        var key = (fontAsset, preset);
        if (_cache.TryGetValue(key, out var cached) && cached != null)
        {
            cached.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.material.GetTexture(ShaderUtilities.ID_MainTex));
            return cached;
        }

        var mat = new Material(fontAsset.material) { name = $"{fontAsset.name}_{preset.name}" };
        preset.ApplyTo(mat);
        _cache[key] = mat;
        return mat;
    }
    /// <summary>
    /// Preset 参数被修改后调用，使对应缓存失效并重刷所有 Applier。
    /// </summary>
    public void InvalidateCache(FontStylePreset preset)
    {
        var toRemove = new List<(TMP_FontAsset, FontStylePreset)>();
        foreach (var key in _cache.Keys)
        {
            if (key.Item2 == preset) toRemove.Add(key);
        }
        foreach (var key in toRemove)
        {
            if (_cache[key] != null) Destroy(_cache[key]);
            _cache.Remove(key);
        }
        if (_currentProfile != null)
            OnFontProfileChanged?.Invoke(_currentProfile);
    }
}

