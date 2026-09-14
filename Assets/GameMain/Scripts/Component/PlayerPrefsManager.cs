using UnityEngine;

namespace Lokas
{
    public class PlayerPrefsManager
    {

        public static int GetInt(string key, int value)
        {

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE

            
            return  GameEntry.Setting.GetInt(key, value); 
#endif

        }


        public static bool GetBool(string key, bool value = false)
        {

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE

            return GameEntry.Setting.GetBool(key, value); 
#endif

        }

        public static float GetFloat(string key, float value)
        {

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE          
            return GameEntry.Setting.GetFloat(key, value); 
#endif

        }

        public static string GetString(string key, string value)
        {

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
            return GameEntry.Setting.GetString(key, value); 
#endif

        }



        public static void SetInt(string key, int value)
        {

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
            GameEntry.Setting.SetInt(key, value);
 
#endif

        }

        public static void SetBool(string key, bool value)
        {

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
            GameEntry.Setting.SetBool(key, value);
 
#endif

        }

        public static void SetFloat(string key, float value)
        {

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
            GameEntry.Setting.SetFloat(key, value);
 
#endif

        }

        public static void SetString(string key, string value)
        {

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
             GameEntry.Setting.SetString(key, value); 
#endif

        }


        public static void Save()
        {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE
            GameEntry.Setting.Save();
#endif
        }

    }
}
