using System.Collections;
using Lokas;
using TMPro;
using UnityEngine;

/// <summary>
/// 挂在每个 TMP Text 上，声明该文本使用哪种样式 key。
/// 语言切换时由 TMPFontManager 广播，自动同步 fontAsset 和动态生成的材质。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TMPStyleApplier : MonoBehaviour
{
    [Tooltip("对应 TMPFontProfile 中配置的样式 key，留空则使用 fontAsset 默认材质")]
    [SerializeField] private string _styleKey = string.Empty;

    private TMP_Text _text;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }
    
    public void SetStyleKey(string styleKey)
    {
        SetStyleKey(styleKey,true);
    }

    public void SetStyleKey(string styleKey, bool applyImmediately = true)
    {
        _styleKey = styleKey ?? string.Empty;

        if (!applyImmediately || GameEntry.TMPFont == null)
            return;

        if (_text == null)
            _text = GetComponent<TMP_Text>();

        Apply(GameEntry.TMPFont.GetCurrentProfile());
    }

    void OnEnable()
    {
        TMPFontComponent.OnFontProfileChanged += Apply;
        if (GameEntry.TMPFont != null)
            StartCoroutine(ApplyNextFrame(GameEntry.TMPFont.GetCurrentProfile()));
    }

    void OnDisable()
    {
        TMPFontComponent.OnFontProfileChanged -= Apply;
    }

    private IEnumerator ApplyNextFrame(TMPFontProfile profile)
    {
        yield return null;
        Apply(profile);
    }

    private void Apply(TMPFontProfile profile)
    {
        if (profile == null || _text == null) return;

        var fontAsset = GameEntry.TMPFont.GetCurrentFontAsset();
        if (fontAsset != null)
            _text.font = fontAsset;

        var preset = profile.GetPreset(_styleKey);
        var mat = GameEntry.TMPFont.GetOrCreateMaterial(fontAsset, preset);
        if (mat != null)
            _text.fontSharedMaterial = mat;

        //  _text.fontStyle = FontStyles.Normal;
        _text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
    }



    
}
