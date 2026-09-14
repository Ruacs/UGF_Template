using System;
using UnityEngine;
using System.Globalization;


namespace YzAdComponent
{
    public class YZLocalStorage
    {

        public static bool HasKey(string key)
        {
            return UnityEngine.PlayerPrefs.HasKey(key);
        }

        public static int getIntItem(string key, int default_value)
        {
            return UnityEngine.PlayerPrefs.GetInt(key, default_value);
        }

        public static float getFloatItem(string key, float default_value)
        {
            return UnityEngine.PlayerPrefs.GetFloat(key, default_value);
        }

        public static long getLongItem(string key, long default_value)
        {
            var cacheValue = UnityEngine.PlayerPrefs.GetString(key, "");
            if (cacheValue != null && cacheValue != "") return long.Parse(cacheValue, CultureInfo.InvariantCulture);
            return default_value;
        }

        public static string getStringItem(string key, string default_value)
        {
            return UnityEngine.PlayerPrefs.GetString(key, default_value);
        }

        public static void setIntItem(string key, int value)
        {
            UnityEngine.PlayerPrefs.SetInt(key, value);
            UnityEngine.PlayerPrefs.Save();
        }

        public static void setFloatItem(string key, float value)
        {
            UnityEngine.PlayerPrefs.SetFloat(key, value);
            UnityEngine.PlayerPrefs.Save();
        }

        public static void setLongItem(string key, long value)
        {
            UnityEngine.PlayerPrefs.SetString(key, value.ToString());
            UnityEngine.PlayerPrefs.Save();
        }

        public static void setStringItem(string key, string value)
        {
            UnityEngine.PlayerPrefs.SetString(key, value);
            UnityEngine.PlayerPrefs.Save();
        }

        public static void DeleteKey(string key)
        {
            UnityEngine.PlayerPrefs.DeleteKey(key);
            UnityEngine.PlayerPrefs.Save();
        }

        public static void DeleteAll()
        {
            UnityEngine.PlayerPrefs.DeleteAll();
            UnityEngine.PlayerPrefs.Save();
        }

    }

}
