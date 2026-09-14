using TMPro;
using UnityEngine;

/// <summary>
/// 文本样式参数预设：存储 Face/描边/阴影参数，不持有材质。
/// 可在多个语言的 FontProfile 中复用同一份预设，Manager 会按 (FontAsset + Preset) 缓存生成的材质。
/// </summary>
[CreateAssetMenu(menuName = "TMP/FontStylePreset", fileName = "FontStylePreset")]
public class FontStylePreset : ScriptableObject
{
    [Header("Face")]
    public Color faceColor = Color.white;
    [Range(-1f, 1f)] public float faceDilate = 0f;
    [Range(0f, 1f)]  public float faceSoftness = 0f;

    [Header("描边 (Outline) — Width > 0 时生效")]
    [Range(0f, 1f)] public float outlineWidth = 0f;
    public Color outlineColor = Color.black;
    [Range(0f, 1f)] public float outlineSoftness = 0f;

    [Header("阴影 (Underlay)")]
    public bool enableUnderlay = false;
    public Color underlayColor = new Color(0f, 0f, 0f, 0.5f);
    public Vector2 underlayOffset = new Vector2(1f, -1f);
    [Range(-1f, 1f)] public float underlayDilate = 0f;
    [Range(0f, 1f)]  public float underlaySoftness = 0f;

    private static readonly int s_FaceSoftness    = Shader.PropertyToID("_FaceSoftness");
    private static readonly int s_OutlineSoftness  = Shader.PropertyToID("_OutlineSoftness");
    private static readonly int s_UnderlaySoftness = Shader.PropertyToID("_UnderlaySoftness");

    /// <summary>
    /// 将本预设的参数写入已有材质（材质应以 fontAsset.material 为模板创建）。
    /// </summary>
    public void ApplyTo(Material mat)
    {
        // Face
        SetColor(mat, ShaderUtilities.ID_FaceColor, faceColor);
        SetFloat(mat, ShaderUtilities.ID_FaceDilate, faceDilate);
        SetFloat(mat, s_FaceSoftness, faceSoftness);

        // Outline
        if (outlineWidth > 0f)
        {
            mat.EnableKeyword("OUTLINE_ON");
            SetFloat(mat, ShaderUtilities.ID_OutlineWidth, outlineWidth);
            SetColor(mat, ShaderUtilities.ID_OutlineColor, outlineColor);
            SetFloat(mat, s_OutlineSoftness, outlineSoftness);
        }
        else
        {
            mat.DisableKeyword("OUTLINE_ON");
            SetFloat(mat, ShaderUtilities.ID_OutlineWidth, 0f);
        }

        // Underlay
        if (enableUnderlay)
        {
            mat.EnableKeyword("UNDERLAY_ON");
            SetColor(mat, ShaderUtilities.ID_UnderlayColor, underlayColor);
            SetFloat(mat, ShaderUtilities.ID_UnderlayOffsetX, underlayOffset.x);
            SetFloat(mat, ShaderUtilities.ID_UnderlayOffsetY, underlayOffset.y);
            SetFloat(mat, ShaderUtilities.ID_UnderlayDilate, underlayDilate);
            SetFloat(mat, s_UnderlaySoftness, underlaySoftness);
        }
        else
        {
            mat.DisableKeyword("UNDERLAY_ON");
        }
    }

    static void SetFloat(Material mat, int id, float value)
    {
        if (mat.HasProperty(id)) mat.SetFloat(id, value);
    }

    static void SetColor(Material mat, int id, Color value)
    {
        if (mat.HasProperty(id)) mat.SetColor(id, value);
    }
}
