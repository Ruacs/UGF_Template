using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// PSB To UGUI 配置窗口
/// </summary>
public class PSBToUGUIConfigWindow : EditorWindow
{
    private UnityEngine.Object m_PsbFile;
    private string m_PanelName = "";
    private string m_PrefabDir = "Assets/GameMain/UI/UIPanel";
    private string m_ScriptDir = "Assets/GameMain/Scripts/UI/Panel";
    private bool m_GenerateScript = true;
    private bool m_RegisterGF = false;
    private string m_UIGroupName = "Default";
    private string m_SpritesDir = "";

    public static void Open(string psbPath)
    {
        var win = GetWindow<PSBToUGUIConfigWindow>("PSB To UGUI");
        win.minSize = new Vector2(450, 380);

        if (!string.IsNullOrEmpty(psbPath))
        {
            win.m_PsbFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(psbPath);
            win.OnPsbChanged();
        }

        win.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("PSB To UGUI Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // ── PSB 文件 ──
        EditorGUI.BeginChangeCheck();
        m_PsbFile = EditorGUILayout.ObjectField("PSB 文件", m_PsbFile, typeof(UnityEngine.Object), false);
        if (EditorGUI.EndChangeCheck())
            OnPsbChanged();

        if (m_PsbFile == null)
        {
            EditorGUILayout.HelpBox("请拖入一个 .psb 文件", MessageType.Info);
            return;
        }

        string psbPath = AssetDatabase.GetAssetPath(m_PsbFile);
        if (!psbPath.EndsWith(".psb", System.StringComparison.OrdinalIgnoreCase))
        {
            EditorGUILayout.HelpBox("请选择 .psb 格式文件", MessageType.Warning);
            return;
        }

        // ── 切图目录状态 ──
        if (!string.IsNullOrEmpty(m_SpritesDir) && Directory.Exists(m_SpritesDir))
        {
            int count = Directory.GetFiles(m_SpritesDir, "*.png", SearchOption.AllDirectories).Length;
            EditorGUILayout.HelpBox($"切图目录: {m_SpritesDir}\n找到 {count} 张 PNG", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"未找到切图目录，请创建:\n{Path.GetDirectoryName(psbPath)}/{Path.GetFileNameWithoutExtension(psbPath)}_sprites/", MessageType.Warning);
        }

        EditorGUILayout.Space(10);

        // ── 面板名称 ──
        m_PanelName = EditorGUILayout.TextField("面板名称", m_PanelName);

        EditorGUILayout.Space(5);

        // ── 输出路径 ──
        m_PrefabDir = EditorGUILayout.TextField("预制体输出目录", m_PrefabDir);
        m_ScriptDir = EditorGUILayout.TextField("脚本输出目录", m_ScriptDir);

        EditorGUILayout.Space(5);

        // ── 选项 ──
        m_GenerateScript = EditorGUILayout.Toggle("生成 C# 脚本", m_GenerateScript);
        m_RegisterGF = EditorGUILayout.Toggle("注册到 GameFramework", m_RegisterGF);
        if (m_RegisterGF)
        {
            EditorGUI.indentLevel++;
            m_UIGroupName = EditorGUILayout.TextField("UI Group", m_UIGroupName);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(15);

        // ── 预览输出 ──
        EditorGUILayout.LabelField("输出预览", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("预制体", $"{m_PrefabDir}/{m_PanelName}.prefab");
        if (m_GenerateScript)
            EditorGUILayout.LabelField("脚本", $"{m_ScriptDir}/{m_PanelName}.cs");
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(15);

        // ── 转换按钮 ──
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("Convert", GUILayout.Height(35)))
        {
            DoConvert(psbPath);
        }
        GUI.backgroundColor = Color.white;
    }

    void OnPsbChanged()
    {
        if (m_PsbFile == null) return;
        string path = AssetDatabase.GetAssetPath(m_PsbFile);
        m_PanelName = PSBToUGUIConverter.SanitizePanelName(Path.GetFileName(path));

        // 查找切图目录
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string[] candidates = { $"{dir}/{name}_sprites", $"{dir}/sprites", $"{dir}/{name}" };
        m_SpritesDir = "";
        foreach (var c in candidates)
            if (Directory.Exists(c)) { m_SpritesDir = c; break; }
    }

    void DoConvert(string psbPath)
    {
        var cfg = new PSBToUGUIConverter.Config
        {
            psbPath = psbPath,
            panelName = m_PanelName,
            prefabOutputDir = m_PrefabDir,
            scriptOutputDir = m_ScriptDir,
            generateScript = m_GenerateScript,
            registerInGF = m_RegisterGF,
            uiGroupName = m_UIGroupName
        };

        PSBToUGUIConverter.Convert(cfg);
    }
}
