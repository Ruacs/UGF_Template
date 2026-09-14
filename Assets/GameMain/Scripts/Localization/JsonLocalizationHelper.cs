using GameFramework.Localization;
using LitJson;
using System;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class JsonLocalizationHelper : DefaultLocalizationHelper
    {
        public override bool ParseData(ILocalizationManager localizationManager, string dictionaryString, object userData)
        {
            try
            {
                JsonData jsonData = JsonMapper.ToObject(dictionaryString);
                if (jsonData == null || !jsonData.IsArray)
                {
                    Log.Warning("Can not parse dictionary data: root is not an array.");
                    return false;
                }

                for (int i = 0; i < jsonData.Count; i++)
                {
                    JsonData entry = jsonData[i];
                    string key = (string)entry["Key"];
                    string value = (string)entry["Value"];
                    if (!localizationManager.AddRawString(key, value))
                    {
                        Log.Warning("Can not add raw string with key '{0}' which may be invalid or duplicate.", key);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("Can not parse dictionary data with exception '{0}'.", exception.ToString());
                return false;
            }
        }
    }
}
