#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Lokas;

public class JsonEncryptTool : EditorWindow
{
    private enum OutputMode
    {
        AesBytes,
        ObfuscatedBytes
    }

    private const string DefaultKey = "YourSecretKey1234567890123456ABC"; // 32 bytes
    private const string DefaultIv = "YourIV1234567890";                  // 16 bytes

    private const string InputDirPrefsKey = "JsonEncryptTool.InputDir";
    private const string OutputDirPrefsKey = "JsonEncryptTool.OutputDir";
    private const string KeyPrefsKey = "JsonEncryptTool.Key";
    private const string IvPrefsKey = "JsonEncryptTool.Iv";
    private const string CompressJsonPrefsKey = "JsonEncryptTool.CompressJson";
    private const string OutputModePrefsKey = "JsonEncryptTool.OutputMode";

    private DefaultAsset m_InputFolder;
    private DefaultAsset m_OutputFolder;
    private string m_Key = DefaultKey;
    private string m_Iv = DefaultIv;
    private bool m_CompressJson = true;
    private OutputMode m_OutputMode = OutputMode.ObfuscatedBytes;

    [MenuItem("Tools/JSON Config Encryptor")]
    public static void Open()
    {
        GetWindow<JsonEncryptTool>("JSON Encryptor");
    }

    private void OnEnable()
    {
        m_Key = EditorPrefs.GetString(KeyPrefsKey, DefaultKey);
        m_Iv = EditorPrefs.GetString(IvPrefsKey, DefaultIv);
        m_CompressJson = EditorPrefs.GetBool(CompressJsonPrefsKey, true);
        m_OutputMode = (OutputMode)EditorPrefs.GetInt(OutputModePrefsKey, (int)OutputMode.ObfuscatedBytes);

        m_InputFolder = LoadFolder(EditorPrefs.GetString(InputDirPrefsKey, "Assets/GameMain/SubGame/HexaAway/Config"));
        m_OutputFolder = LoadFolder(EditorPrefs.GetString(OutputDirPrefsKey, "Assets/GameMain/SubGame/HexaAway/Config"));
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("JSON Config Encryptor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        m_InputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Input Folder", m_InputFolder, typeof(DefaultAsset), false);
        m_OutputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", m_OutputFolder, typeof(DefaultAsset), false);
        m_OutputMode = (OutputMode)EditorGUILayout.EnumPopup("Output Mode", m_OutputMode);

        using (new EditorGUI.DisabledScope(m_OutputMode != OutputMode.AesBytes))
        {
            EditorGUILayout.BeginHorizontal();
            m_Key = EditorGUILayout.TextField("Key", m_Key);
            if (GUILayout.Button("Gen 16", GUILayout.Width(64f)))
                m_Key = GenerateAsciiSecret(16);
            if (GUILayout.Button("Gen 24", GUILayout.Width(64f)))
                m_Key = GenerateAsciiSecret(24);
            if (GUILayout.Button("Gen 32", GUILayout.Width(64f)))
                m_Key = GenerateAsciiSecret(32);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            m_Iv = EditorGUILayout.TextField("IV", m_Iv);
            if (GUILayout.Button("Generate 16", GUILayout.Width(96f)))
                m_Iv = GenerateAsciiSecret(16);
            EditorGUILayout.EndHorizontal();
        }

        m_CompressJson = EditorGUILayout.ToggleLeft("Compress JSON To One Line", m_CompressJson);

        if (EditorGUI.EndChangeCheck())
            SavePrefs();

        string inputDir = GetFolderPath(m_InputFolder);
        string outputDir = GetFolderPath(m_OutputFolder);
        int keyBytes = Encoding.UTF8.GetByteCount(m_Key ?? string.Empty);
        int ivBytes = Encoding.UTF8.GetByteCount(m_Iv ?? string.Empty);

        EditorGUILayout.Space();
        if (m_OutputMode == OutputMode.AesBytes)
        {
            EditorGUILayout.HelpBox(
                $"Key bytes: {keyBytes} (must be 16, 24, or 32)\nIV bytes: {ivBytes} (must be 16)",
                IsValidKey(m_Key) && IsValidIv(m_Iv) ? MessageType.Info : MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "ObfuscatedBytes writes a lightweight bytes wrapper: N00? header, swapped body, random padding, boly tail.",
                MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(!CanEncrypt(inputDir, outputDir, m_Key, m_Iv, m_OutputMode)))
        {
            if (GUILayout.Button("Encrypt JSON Configs"))
                EncryptAllConfigs(inputDir, outputDir, m_Key, m_Iv, m_CompressJson, m_OutputMode);
        }
    }

    private static void EncryptAllConfigs(string inputDir, string outputDir, string key, string iv, bool compressJson, OutputMode outputMode)
    {
        if (!Directory.Exists(inputDir))
        {
            Debug.LogError($"Input config directory does not exist: {inputDir}");
            return;
        }

        if (outputMode == OutputMode.AesBytes && (!IsValidKey(key) || !IsValidIv(iv)))
        {
            Debug.LogError("Invalid AES key or IV length. Key must be 16/24/32 UTF-8 bytes, IV must be 16 UTF-8 bytes.");
            return;
        }

        Directory.CreateDirectory(outputDir);

        int count = 0;
        int failedCount = 0;
        foreach (var file in Directory.GetFiles(inputDir, "*.json"))
        {
            string json = File.ReadAllText(file);
            if (compressJson && !TryCompressJson(json, file, out json))
            {
                failedCount++;
                continue;
            }

            byte[] encrypted = outputMode == OutputMode.AesBytes
                ? AesEncrypt(json, key, iv)
                : JsonConfigObfuscator.Obfuscate(Encoding.UTF8.GetBytes(json));
            string outPath = Path.Combine(
                outputDir,
                Path.GetFileNameWithoutExtension(file) + ".bytes");

            File.WriteAllBytes(outPath, encrypted);
            Debug.Log($"Encrypted: {Path.GetFileName(file)} -> {outPath}");
            count++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Encrypted {count} JSON config(s). Failed: {failedCount}.");
    }

    private static byte[] AesEncrypt(string plainText, string key, string iv)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream();
        using var encStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        byte[] data = Encoding.UTF8.GetBytes(plainText);
        encStream.Write(data, 0, data.Length);
        encStream.FlushFinalBlock();
        return ms.ToArray();
    }

    private static DefaultAsset LoadFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            return null;

        return AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
    }

    private static string GetFolderPath(DefaultAsset folder)
    {
        if (folder == null)
            return string.Empty;

        string path = AssetDatabase.GetAssetPath(folder);
        return AssetDatabase.IsValidFolder(path) ? path : string.Empty;
    }

    private static bool CanEncrypt(string inputDir, string outputDir, string key, string iv, OutputMode outputMode)
    {
        return !string.IsNullOrEmpty(inputDir)
            && !string.IsNullOrEmpty(outputDir)
            && Directory.Exists(inputDir)
            && (outputMode != OutputMode.AesBytes || (IsValidKey(key) && IsValidIv(iv)));
    }

    private static bool IsValidKey(string key)
    {
        int byteCount = Encoding.UTF8.GetByteCount(key ?? string.Empty);
        return byteCount == 16 || byteCount == 24 || byteCount == 32;
    }

    private static bool IsValidIv(string iv)
    {
        return Encoding.UTF8.GetByteCount(iv ?? string.Empty) == 16;
    }

    private static bool TryCompressJson(string json, string filePath, out string compressedJson)
    {
        try
        {
            compressedJson = JToken.Parse(json).ToString(Formatting.None);
            return true;
        }
        catch (JsonException exception)
        {
            compressedJson = json;
            Debug.LogError($"Compress JSON failed: {filePath}\n{exception.Message}");
            return false;
        }
    }

    private static string GenerateAsciiSecret(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] result = new char[length];
        byte[] randomBytes = new byte[length];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        for (int i = 0; i < length; i++)
            result[i] = chars[randomBytes[i] % chars.Length];

        return new string(result);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(InputDirPrefsKey, GetFolderPath(m_InputFolder));
        EditorPrefs.SetString(OutputDirPrefsKey, GetFolderPath(m_OutputFolder));
        EditorPrefs.SetString(KeyPrefsKey, m_Key ?? string.Empty);
        EditorPrefs.SetString(IvPrefsKey, m_Iv ?? string.Empty);
        EditorPrefs.SetBool(CompressJsonPrefsKey, m_CompressJson);
        EditorPrefs.SetInt(OutputModePrefsKey, (int)m_OutputMode);
    }
}
#endif
