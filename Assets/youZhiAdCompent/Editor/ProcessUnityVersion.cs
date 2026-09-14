using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ProcessUnityVersion
{
    public static Dictionary<string, string> replaceDict = new Dictionary<string, string>()
    {
        {"m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}", "m_Script: {fileID: 708705254, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 3245ec927659c4140ac4f8d17403cc18, type: 3}", "m_Script: {fileID: 1741964061, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}", "m_Script: {fileID: -765806418, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 0cd44c1031e13a943bb63640046fad76, type: 3}", "m_Script: {fileID: 1980459831, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: dc42784cf147c0c48a680349fa168899, type: 3}", "m_Script: {fileID: 1301386320, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 31a19414c41e5ae4aae2af33fee712f6, type: 3}", "m_Script: {fileID: -1200242548, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 1392445389, guid: f70555f144d8491a825f0804e09c671c, type: 3}", "m_Script: {fileID: 11500000, guid: a65f502c9af7751458beffa2a6c253c3, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 8a8695521f0d02e499659fee002a26c2, type: 3}", "m_Script: {fileID: -2095666955, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 1344c3c82d62a2a41a3576d8abb8e3ea, type: 3}", "m_Script: {fileID: -98529514, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}", "m_Script: {fileID: 1392445389, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: e19747de3f5aca642ab2be37e372fb86, type: 3}","m_Script: {fileID: -900027084, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 306cc8c2b49d7114eaa3623786fc2126, type: 3}","m_Script: {fileID: 1679637790, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {" m_Script: {fileID: 11500000, guid: 1aa08ab6e0800fa44ae55d278d1423e3, type: 3}","m_Script: {fileID: 1367256648, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 30649d3a9faa99c48a7b1166b86bf2a0, type: 3}","m_Script: {fileID: -405508275, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 86710e43de46f6f4bac7c8e50813a599, type: 3}","m_Script: {fileID: -1254083943, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 67db9e8f0e2ae9c40bc1e2b64352a6b4, type: 3}","m_Script: {fileID: -113659843, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 59f8146938fff824cb5fd77236b75775, type: 3}","m_Script: {fileID: 1297475563, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: d199490a83bb2b844b9695cbf13b01ef, type: 3}","m_Script: {fileID: 575553740, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: 9085046f02f69544eb97fd06b6048fe2, type: 3}","m_Script: {fileID: 2109663825, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
        {"m_Script: {fileID: 11500000, guid: d0b148fe25e99eb48b9724523833bab1, type: 3}","m_Script: {fileID: -1862395651, guid: f70555f144d8491a825f0804e09c671c, type: 3}"},
    };

    [MenuItem("Tool/替换低版本引用")]
    public static void Process()
    {
        ProcessAllPrefabs();
    }
    private static void ProcessAllPrefabs()
    {
        List<GameObject> prefabs = new List<GameObject>();
        var resourcesPath = Application.dataPath+"/Scripts/Common/";
        var absolutePaths = System.IO.Directory.GetFiles(resourcesPath, "*.prefab", System.IO.SearchOption.AllDirectories);
        for (int i = 0; i < absolutePaths.Length; i++)
        {
            Debug.Log("prefab name: " + absolutePaths[i]);
            foreach (var VARIABLE in replaceDict)
            {
                ReplaceValue(absolutePaths[i], VARIABLE.Key, VARIABLE.Value);
            }
            EditorUtility.DisplayProgressBar("处理预制体……", "处理预制体中……", (float)i / absolutePaths.Length);
        }
        EditorUtility.ClearProgressBar();
    }
    /// <summary>
    /// 替换值
    /// </summary>
    /// <param name="strFilePath">txt等文件的路径</param>
    private static void ReplaceValue(string strFilePath, string oldLine, string newLine)
    {
        if (File.Exists(strFilePath))
        {
            string[] lines = System.IO.File.ReadAllLines(strFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Replace(oldLine, newLine);
            }
            File.WriteAllLines(strFilePath, lines);
        }
    }
}