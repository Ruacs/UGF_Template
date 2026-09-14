//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 路径
    /// </summary>
    public static class AssetUtility
    {
        /// <summary>
        /// 配置表
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="fromBytes"></param>
        /// <returns></returns>
        public static string GetConfigAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format("Assets/GameMain/Configs/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }
        /// <summary>
        /// 资源信息表
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="fromBytes"></param>
        /// <returns></returns>
        public static string GetDataTableAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format("Assets/GameMain/DataTables/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }


        public static string GetCustomConfigAsset(string assetName, bool fromBytes, string suffix = "txt")
        {
            if (TryGetSubGameRelativeAssetName(assetName, out string gameName, out string relativeAssetName))
            {
                return GetSubGameAsset(gameName, "Config", relativeAssetName, fromBytes ? "bytes" : suffix);
            }

            return Utility.Text.Format("Assets/GameMain/CustomConfigs/{0}.{1}", assetName, fromBytes ? "bytes" : suffix);
        }
        /// <summary>
        /// 语言配置自定义配置文件
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="fromBytes"></param>
        /// <returns></returns>
        public static string GetDictionaryAsset()
        {
            string suffix;
            switch (GameEntry.Localization.Type)
            {
                case UnityGameFramework.Runtime.LocalizationComponent.DictionaryType.Xml:
                    suffix = "xml";
                    break;
                case UnityGameFramework.Runtime.LocalizationComponent.DictionaryType.Json:
                    suffix = "json";
                    break;
                case UnityGameFramework.Runtime.LocalizationComponent.DictionaryType.Bytes:
                    suffix = "bytes";
                    break;
                default:
                    suffix = "xml";
                    break;
            }
            return Utility.Text.Format("Assets/GameMain/Localization/{0}/{1}.{2}", GameEntry.Localization.Language, GameEntry.Localization.Language, suffix);
        }

        /// <summary>
        /// 字体
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetFontAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Fonts/{0}.ttf", assetName);
        }

        public static string GetTMPFontAsset(string assetName, bool isCommon = false)
        {
            if (isCommon)
            {
                return Utility.Text.Format("Assets/GameMain/Fonts/{0}.asset", assetName);
            }
            string language = GameEntry.Localization.Language == GameFramework.Localization.Language.ChineseSimplified ? "CN" : "EN";
            return Utility.Text.Format("Assets/GameMain/Fonts/{0}_{1}.asset", assetName, language);
        }

        /// <summary>
        /// 场景
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetSceneAsset(string assetName)
        {
            if (SubGameAssetRegistry.TryGetSceneGameName(assetName, out string gameName))
            {
                return GetSubGameSceneAsset(gameName, assetName);
            }

            return Utility.Text.Format("Assets/GameMain/Scenes/{0}.unity", assetName);
        }
        /// <summary>
        /// 背景音乐
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetMusicAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Audio/Music/{0}.ogg", assetName);
        }
        /// <summary>
        /// 声音
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetSoundAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Audio/Sound/{0}.ogg", assetName);
        }


        /// <summary>
        /// 实体路径
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetEntityAsset(string assetName)
        {
            if (TryGetSubGameRelativeAssetName(assetName, out string gameName, out string relativeAssetName))
            {
                return GetSubGameAsset(gameName, "Entity", relativeAssetName, "prefab");
            }

            return Utility.Text.Format("Assets/GameMain/Entities/{0}.prefab", assetName);
        }

        /// <summary>
        /// UI预制体
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetUIFormAsset(string assetName)
        {
            if (SubGameAssetRegistry.TryGetUIFormGameName(assetName, out string gameName))
            {
                return GetSubGameUIFormAsset(gameName, assetName);
            }

            return Utility.Text.Format("Assets/GameMain/UI/UIPanel/{0}.prefab", assetName);
        }

        /// <summary>
        /// 本地化图片
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetLocalizationImage(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Localization/{0}/Images/{1}.png", GameEntry.Localization.Language, assetName);
        }

   
        public static string GetMaterialsAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Materials/SnakeSkinMat/{0}.mat", assetName);
        }

        public static string GetScriptableObjectAsset(string assetName)
        {
            if (SubGameAssetRegistry.TryGetScriptableObjectLocation(assetName, out string gameName, out string category))
            {
                return GetSubGameScriptableObjectAsset(gameName, category, assetName);
            }

            return Utility.Text.Format("Assets/GameMain/ScriptableObjects/{0}.asset", assetName);
        }

        /// <summary>
        /// UI点击音效
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetUISoundAsset(string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Audio/Sound/{0}.ogg", assetName);
        }

        public static string GetSubGameAsset(string gameName, string category, string assetName, string extension)
        {
            return Utility.Text.Format("Assets/GameMain/SubGame/{0}/{1}/{2}.{3}", gameName, category, assetName, extension);
        }

        public static string GetSubGameScriptableObjectAsset(string gameName, string category, string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/SubGame/{0}/ScriptableObjects/{1}/{2}.asset", gameName, category, assetName);
        }

        public static string GetSubGameUIFormAsset(string gameName, string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/SubGame/{0}/UI/{1}.prefab", gameName, assetName);
        }

        public static string GetSubGameSceneAsset(string gameName, string assetName)
        {
            return Utility.Text.Format("Assets/GameMain/Scenes/SubGame/{0}/{1}.unity", gameName, assetName);
        }

        private static bool TryGetSubGameRelativeAssetName(string assetName, out string gameName, out string relativeAssetName)
        {
            gameName = null;
            relativeAssetName = null;

            if (string.IsNullOrEmpty(assetName))
            {
                return false;
            }

            return SubGameAssetRegistry.TryParseScopedAssetName(assetName, out gameName, out relativeAssetName);
        }

    }
}
