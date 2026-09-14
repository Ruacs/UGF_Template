using GameFramework;
using UnityGameFramework.Runtime;
namespace Lokas
{
    public static class LocalizationExtension
    {
        public static string Get(this LocalizationComponent localization, string key, params object[] args)
        {
            var str = GameEntry.Localization.GetString(key);

            if (args == null || args.Length == 0)
            {
                return str;
            }

            return Utility.Text.Format(str, args);
        }


        public static async void LoadLanguage(this LocalizationComponent com, object userData)
        {
             com.ReadData(AssetUtility.GetDictionaryAsset(), userData);
        }
    }
}