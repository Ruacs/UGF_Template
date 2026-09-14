using System;
using UnityEngine;

namespace YzAdComponent
{

    public class AndroidJavaClassHelper
    {

        private static AndroidJavaClass _instance;


        public static void CallStatic(string methodName, params object[] args)
        {

            try
            {
                if (_instance == null)
                {
                    _instance = new AndroidJavaClass(YzConstant.JniClassName);
                }
                _instance.CallStatic(methodName, args);
            }
            catch (Exception)
            {
                Debug.LogError("JNI Call Error");
            }
        }

        public static ReturnType CallStatic<ReturnType>(string methodName, params object[] args)
        {
            try
            {
                if (_instance == null)
                {
                    _instance = new AndroidJavaClass(YzConstant.JniClassName);
                }
                return _instance.CallStatic<ReturnType>(methodName, args);
            }
            catch (Exception)
            {
                Debug.LogError("JNI Call Error");
            }

            return default(ReturnType);
        }
        public static AndroidJavaClass Instance
        {
            get
            {

                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
    }

}