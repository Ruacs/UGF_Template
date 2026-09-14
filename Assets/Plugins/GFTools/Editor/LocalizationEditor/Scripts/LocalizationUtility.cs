using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace GFTools.LocalizationEditor
{
    public static class LocalizationUtility
    {
        /// <summary>
        /// 从XML文件中读取本地话数据
        /// </summary>
        /// <param name="filePath">XML文件路径</param>
        /// <returns>键值对字典</returns>
        public static List<LocalizationEntry> ReadXml(string filePath)
        {
            List<LocalizationEntry> localizationEntryList = new List<LocalizationEntry>();

            if (!File.Exists(filePath))
            {
                Debug.LogError($"Localization XML file not found: {filePath}");
                return localizationEntryList;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            XmlNodeList xmlNodeDictionaryList = xmlDoc.SelectNodes("Dictionaries/Dictionary/String");
            foreach (XmlNode node in xmlNodeDictionaryList)
            {
                var key = node.Attributes["Key"]?.Value;
                var value = node.Attributes["Value"]?.Value;
                if(!string.IsNullOrEmpty(key))
                {
                    localizationEntryList.Add(new LocalizationEntry { Key = key, Value = value });
                }
            }
            return localizationEntryList;
        }

        /// <summary>
        /// 将 key-value 本地化数据写入 XML 文件
        /// </summary>
        /// <param name="LanguageType">语言类型</param>
        /// <param name="filePath">XML文件路径</param>
        /// <param name="localizationDict">要写入的本地化数据</param>
        public static void WriteXml(string LanguageType, string filePath, List<LocalizationEntry> localizationEntryList)
        {
            XmlDocument xmlDoc = new XmlDocument();
            //创建XML声明
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
            xmlDoc.AppendChild(xmlDeclaration);

            //创建根节点
            XmlElement root = xmlDoc.CreateElement("Dictionaries");
            xmlDoc.AppendChild(root);


            XmlElement language = xmlDoc.CreateElement("Dictionary");
            language.SetAttribute("Language", LanguageType);
            root.AppendChild(language);

            foreach(var entry in localizationEntryList)
            {
                XmlElement stringElement = xmlDoc.CreateElement("String");
                stringElement.SetAttribute("Key", entry.Key);
                stringElement.SetAttribute("Value", entry.Value);
                language.AppendChild(stringElement);
            }

            // 创建文件夹（如果不存在）
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // 保存文件（UTF-8 编码，带 BOM）
            using (var writer = new XmlTextWriter(filePath, new UTF8Encoding(true)))
            {
                writer.Formatting = System.Xml.Formatting.Indented;
                xmlDoc.Save(writer);
            }

            Debug.Log($"Localization XML saved:<color=#BCFF3B> {filePath}</color>");
        }


        /// <summary>
        /// Json读取
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<LocalizationEntry> ReadJson(string filePath)
        {
            List<LocalizationEntry> localizationEntryList = new List<LocalizationEntry>();
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Localization Json file not found: {filePath}");
                return localizationEntryList;
            }
            string stringDictionary = File.ReadAllText(filePath);
            localizationEntryList = JsonConvert.DeserializeObject<List<LocalizationEntry>>(stringDictionary);

            return localizationEntryList;
        }

        /// <summary>
        /// Json写入
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="localizationEntryList">要写入的本地化词条数据</param>
        public static void WriteJson(string filePath, List<LocalizationEntry> localizationEntryList)
        {
            // 空值校验
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("JSON写入失败：文件路径为空");
            }
            if (localizationEntryList == null || localizationEntryList.Count == 0)
            {
                Debug.LogWarning("JSON写入警告：本地化字典为空");
            }

            try
            {
                // 1. 创建目录（自动处理多级目录）
                string dirPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                // 2. 序列化JSON（格式化输出 + 兼容中文）
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented, // 格式化输出（易读）
                    StringEscapeHandling = StringEscapeHandling.Default // 避免中文被转义为\uXXXX
                };
                string jsonContent = JsonConvert.SerializeObject(localizationEntryList, settings);

                // 3. 写入文件（UTF-8编码，避免中文乱码）
                File.WriteAllText(filePath, jsonContent, System.Text.Encoding.UTF8);

                Debug.Log($"JSON文件写入成功：{filePath}\n内容：{jsonContent}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"JSON写入失败：{exception.Message}\n{exception.StackTrace}");
            }
        }


        public static void SaveLanguageFileByXml(Dictionary<string,string> dict)
        {

        }
       

        /// <summary>
        /// 获取指定路径下的子文件夹名称列表
        /// </summary>
        /// <param name="folderPath">Unity 项目内的路径，如 "Assets/GameMain/Localization"</param>
        /// <returns>子文件夹名称列表（不含完整路径）</returns>
        public static List<string> GetFileNames(string folderPath)
        {
            List<string> subfolders = new List<string>();
            HashSet<string> languages = new HashSet<string>();
            if (Directory.Exists(folderPath))
            {
                string[] dirs = Directory.GetFiles(folderPath);
     
                foreach (string dir in dirs)
                {
                    string fileName = Path.GetFileName(dir);
                    string[] strArr = fileName.Split(".");
                    if (!string.IsNullOrEmpty(strArr[0]) && !languages.Contains(strArr[0]))
                    {
                        languages.Add(strArr[0]);
                        subfolders.Add(strArr[0]);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"路径不存在：{folderPath}");
            }

            return subfolders;
        }

        /// <summary>
        /// 生成纯色背景图
        /// </summary>
        /// <param name="width">宽</param>
        /// <param name="height">高</param>
        /// <param name="col">颜色</param>
        /// <returns></returns>
        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }



        public static string GetIconPath(EditorWindow window)
        {
            string iconLocalPath = "Textures/Icon_Localization.png";
            MonoScript script = MonoScript.FromScriptableObject(window);
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string folderPath = Path.GetDirectoryName(scriptPath);
           
            if (folderPath.EndsWith("Scripts"))
            {
                folderPath = folderPath.Substring(0, folderPath.Length - "Scripts".Length);
            }
            string iconPath = folderPath.Replace("\\","/") + iconLocalPath;

            return iconPath;
        }

        public static string GetSuffixByFileType(FileType fileType)
        {
            return "." + fileType.ToString().ToLowerInvariant();
        }

    }
}
