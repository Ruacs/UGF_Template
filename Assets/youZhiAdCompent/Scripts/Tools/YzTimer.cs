using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;


namespace YzAdComponent
{
    public class YzTimer : MonoBehaviour
    {
        protected static List<TimerComponent> _components = new List<TimerComponent>();
        protected static int _tagCount = 1000;
        private bool isBackground = false;//是否可以后台运行

        private static YzTimer instance;

        //Update更新之前
        private void Start()
        {
            if (instance == null)
            {
                DontDestroyOnLoad(gameObject);
                StartCoroutine(UpdateFrame());
            }
        }

        //程序切换到后台
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                if (!isBackground)
                    StopAllCoroutines();
            }
            else
            {
                if (!isBackground)
                    StartCoroutine(UpdateFrame());
            }
        }

        //每一帧都更新
        private IEnumerator UpdateFrame()
        {

            while (true)
            {
                if (_components.Count > 0)
                {
                    float dt = Time.deltaTime;
                    TimerComponent t = null;
                    for (int i = 0; i < _components.Count; ++i)
                    {
                        t = _components[i];
                        t.tm += Time.deltaTime;
                        if (t.tm >= t.life)
                        {
                            t.tm -= t.life;
                            if (t.count == 1)
                            {
                                _components.RemoveAt(i--);
                                if (lateChannel.Contains(t.func))
                                {
                                    lateChannel.Remove(t.func);
                                }
                            }
                            --t.count;
                            t.func();
                        }
                    }
                }
                yield return 0;
            }
        }

        private static HashSet<Action> lateChannel = new HashSet<Action>();
        /// <summary>
        /// 多次调用 只执行一次
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="func"></param>
        public static void CallerLate(float delay, Action func)
        {
            if (!lateChannel.Contains(func))
            {
                lateChannel.Add(func);
                SetTimeout(delay, func);
            }
        }

        /// <summary>
        /// 延时回调一次
        /// </summary>
        /// <param name="delay">等待时间 单位秒</param>
        /// <param name="func">回调方法</param>
        /// <returns>Timer ID</returns>
        public static int SetTimeout(float delay, Action func)
        {
            return SetInterval(delay, func);
        }


        /// <summary>
        /// 注册一个定时器
        /// </summary>
        /// <param name="interval">间隔 单位秒</param>
        /// <param name="func">调度方法</param>
        /// <param name="times">循环次数 times{ 0 | < 1 }:一直循环 </param>
        /// <returns>每个调度器的标签值</returns>
        public static int SetInterval(float interval, Action func, int times = 1)
        {
            var scheduler = new TimerComponent();
            scheduler.tm = 0;
            scheduler.life = interval;
            scheduler.func = func;
            scheduler.count = times;
            scheduler.tag = ++_tagCount;
            _components.Add(scheduler);
            if (!_inited) Init();
            return _tagCount;
        }

        /// <summary>
        /// 通过Tag获取定时器对象
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static TimerComponent GetTimer(int tag)
        {

            foreach (var scheduler in _components)
            {
                if (tag == scheduler.tag)
                {
                    return scheduler;
                }
            }

            return null;
        }

        /// <summary>
        /// 清理定时器
        /// </summary>
        /// <param name="tag">定时器标签</param>
        /// <returns></returns>
        public static void ClearTimer(int tag)
        {
            int index = _components.FindIndex((TimerComponent t) =>
            {
                return t.tag == tag;
            });
            _components.RemoveAt(index);
        }

        /// <summary>
        /// 清理所有定时器
        /// </summary>
        public static void ClearTimers()
        {
            _components.Clear();
        }

        //初始化
        protected static bool _inited = false;
        protected static void Init()
        {
            var inst = new GameObject("TimerNode");
            inst.AddComponent<YzTimer>();
            _inited = true;
        }


        //定时器数据类
        public class TimerComponent
        {
            public int tag;
            public float tm;
            public float life;
            public int count;
            public Action func;
        }
    }
}