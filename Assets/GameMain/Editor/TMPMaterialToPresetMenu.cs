using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Project 窗口右键菜单：从已有 TMP 材质读取 Face/描边/阴影参数，生成 FontStylePreset 资产。
/// 支持多选，批量提取。
/// </summary>
public static class TMPMaterialToPresetMenu
{
    [MenuItem("Assets/TMP/从材质创建 FontStylePreset")]
    static void CreatePresetFromMaterial()
    {
        var materials = GetSelectedTMPMaterials();
        if (materials.Length == 0) return;

        int created = 0;
        Object lastPreset = null;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var mat in materials)
            {
                var preset = BuildPreset(mat);
                string dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(mat)).Replace('\\', '/');
                string presetPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{mat.name}_Preset.asset");
                AssetDatabase.CreateAsset(preset, presetPath);
                lastPreset = preset;
                created++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        if (created == 1 && lastPreset != null)
        {
            Selection.activeObject = lastPreset;
            EditorGUIUtility.PingObject(lastPreset);
        }
        else
        {
            Debug.Log($"[TMPMaterialToPreset] 已创建 {created} 个 FontStylePreset");
        }
    }

    [MenuItem("Assets/TMP/从材质创建 FontStylePreset", validate = true)]
    static bool Validate() => GetSelectedTMPMaterials().Length > 0;

    // ── 工具方法 ────────────────────────────────────────────

    static Material[] GetSelectedTMPMaterials()
    {
        var result = new System.Collections.Generic.List<Material>();
        foreach (var obj in Selection.objects)
        {
            if (obj is Material mat && mat.shader != null && mat.shader.name.Contains("TextMeshPro"))
                result.Add(mat);
        }
        return result.ToArray();
    }

    static readonly int s_FaceSoftness    = Shader.PropertyToID("_FaceSoftness");
    static readonly int s_OutlineSoftness = Shader.PropertyToID("_OutlineSoftness");
    static readonly int s_UnderlaySoftness = Shader.PropertyToID("_UnderlaySoftness");

    static FontStylePreset BuildPreset(Material mat)
    {
        var preset = ScriptableObject.CreateInstance<FontStylePreset>();

        // Face
        preset.faceColor    = GetColor(mat, ShaderUtilities.ID_FaceColor, Color.white);
        preset.faceDilate   = GetFloat(mat, ShaderUtilities.ID_FaceDilate);
        preset.faceSoftness = GetFloat(mat, s_FaceSoftness);

        // Outline — TMP 基础 shader 不用 OUTLINE_ON keyword，直接读值
        preset.outlineWidth    = GetFloat(mat, ShaderUtilities.ID_OutlineWidth);
        preset.outlineColor    = GetColor(mat, ShaderUtilities.ID_OutlineColor, Color.black);
        preset.outlineSoftness = GetFloat(mat, s_OutlineSoftness);

        // Underlay — UNDERLAY_ON keyword 或 color.a > 0 均视为启用
        var underlayColor = GetColor(mat, ShaderUtilities.ID_UnderlayColor, default);
        preset.enableUnderlay = mat.IsKeywordEnabled("UNDERLAY_ON") || underlayColor.a > 0f;
        if (preset.enableUnderlay)
        {
            preset.underlayColor    = underlayColor;
            preset.underlayOffset   = new Vector2(
                GetFloat(mat, ShaderUtilities.ID_UnderlayOffsetX),
                GetFloat(mat, ShaderUtilities.ID_UnderlayOffsetY));
            preset.underlayDilate   = GetFloat(mat, ShaderUtilities.ID_UnderlayDilate);
            preset.underlaySoftness = GetFloat(mat, s_UnderlaySoftness);
        }

        return preset;
    }

    static float GetFloat(Material mat, int id, float fallback = 0f) =>
        mat.HasProperty(id) ? mat.GetFloat(id) : fallback;

    static Color GetColor(Material mat, int id, Color fallback) =>
        mat.HasProperty(id) ? mat.GetColor(id) : fallback;
}
