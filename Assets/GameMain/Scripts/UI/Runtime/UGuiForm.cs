using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameFramework;
using GameFramework.ObjectPool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public abstract class UGuiForm : UIFormLogic
    {
        public const int DepthFactor = 100;
        protected const float FadeTime = 0.3f;

        private static Font s_MainFont = null;
        private static TMP_FontAsset s_TMPMainFont = null;
        private Canvas m_CachedCanvas = null;
        private CanvasGroup m_CanvasGroup = null;
        private List<Canvas> m_CachedCanvasContainer = new List<Canvas>();

        [SerializeField] protected DOTweenSequence m_openAnimation;
        [SerializeField] protected DOTweenSequence m_closeAnimation;
        [SerializeField] bool m_ReverseOpenAnimAsClose = true;

        IList<IObjectPool<UIItemObject>> m_ItemPools = null;
        public static TMP_FontAsset TMP_MainFont => s_TMPMainFont;

        public int OriginalDepth
        {
            get;
            private set;
        }

        public int Depth
        {
            get
            {
                return m_CachedCanvas.sortingOrder;
            }
        }

        public static TMP_FontAsset S_TMPMainFont { get => s_TMPMainFont; set => s_TMPMainFont = value; }

        /// <summary>
        /// 关闭自身
        /// </summary>
        public void Close(bool isFade = true)
        {
            if (isFade)
            {
                _closeTween?.Kill();
                Tween closeTween = OnCloseUIAnimation();
                if (closeTween != null)
                {
                    _closeTween = DOTween.Sequence()
                        .Append(closeTween)
                        .AppendCallback(() => GameEntry.UI.CloseUIForm(this));
                }
                else
                {
                    // 没有自定义关闭动画，使用默认淡出
                    _closeTween = m_CanvasGroup.DOFade(0f, FadeTime).SetUpdate(true)
                        .OnComplete(() => GameEntry.UI.CloseUIForm(this));
                }
            }
            else
            {
                GameEntry.UI.CloseUIForm(this);
            }
        }

        private Tween _closeTween;
        private Tween _openTween;


        public void PlayUISound(int uiSoundId)
        {
            //UI点击音效
            GameEntry.Sound.PlayUISound(uiSoundId);
        }

        /// <summary>
        /// 设置字体
        /// </summary>
        /// <param name="mainFont"></param>
        public static void SetMainFont(Font mainFont)
        {
            if (mainFont == null)
            {
                Log.Error("Main font is invalid.");
                return;
            }

            s_MainFont = mainFont;
        }

        public static void SetMainFont(TMP_FontAsset mainFont)
        {
            if (mainFont == null)
            {
                Log.Error("Main font is invalid.");
                return;
            }

            s_TMPMainFont = mainFont;
        }



        /// <summary>
        /// 动画初始化
        /// </summary>
        protected virtual void OnInitUIAnimation()
        {

        }

        /// <summary>
        /// 播放进入动画，子类可重写返回自定义DOTween动画序列
        /// </summary>
        protected virtual Tween OnOpenUIAnimation()
        {
            if (m_openAnimation == null)
            {
                m_CanvasGroup.alpha = 0f;
                return m_CanvasGroup.DOFade(1f, FadeTime).SetUpdate(true);
            }
            return m_openAnimation.DOPlay();
        }

        /// <summary>
        /// 播放关闭动画，子类可重写返回自定义DOTween动画序列
        /// </summary>
        protected virtual Tween OnCloseUIAnimation()
        {
            if (m_closeAnimation == null)
            {
                if (m_openAnimation != null && m_ReverseOpenAnimAsClose)
                {
                    m_openAnimation?.DORewind();
                }
                return m_CanvasGroup.DOFade(0, FadeTime).SetUpdate(true);

            }
            return m_closeAnimation?.DOPlay();
        }


        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            m_CachedCanvas = gameObject.GetOrAddComponent<Canvas>();
            m_CachedCanvas.overrideSorting = true;
            OriginalDepth = m_CachedCanvas.sortingOrder;

            m_CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();

            RectTransform transform = GetComponent<RectTransform>();
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = Vector2.zero;

            gameObject.GetOrAddComponent<GraphicRaycaster>();

            OnInitUIAnimation();
            InitLocalization();
        }


        /// <summary>
        /// 更新界面中静态文本的多语言文字
        /// </summary>
        public virtual void InitLocalization()
        {
            UIStringKey[] texts = GetComponentsInChildren<UIStringKey>(true);
            foreach (var t in texts)
            {
                if (t.TryGetComponent<TMPro.TextMeshProUGUI>(out var textMeshCom))
                {
                    textMeshCom.text = GameEntry.Localization.GetString(t.Key);

                }
                else if (t.TryGetComponent<Text>(out var textCom))
                {
                    textCom.text = GameEntry.Localization.GetString(t.Key);
                }
            }

            TMP_Text[] tmp_texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmp_texts)
            {
                if (s_TMPMainFont == null)
                {
                    // Log.Warning("Main TMP Font is invalid.");
                }
                else
                {
                    t.font = s_TMPMainFont;
                    t.fontStyle = FontStyles.Normal;
                }
            }

            Text[] texts2 = GetComponentsInChildren<Text>(true);
            foreach (var t in texts2)
            {
                if (s_MainFont == null)
                {
                    //Log.Warning("Main Font is invalid.");
                }
                else
                {
                    t.font = s_MainFont;
                }
            }

            UILocalizedImageKey[] images = GetComponentsInChildren<UILocalizedImageKey>(true);
            foreach (var image in images)
            {
                image.ApplyLocalization();
            }
        }

        /// <summary>
        /// 界面回收时调用
        /// </summary>
        protected override void OnRecycle()
        {
            base.OnRecycle();
        }


        /// <summary>
        /// 每次打开界面时调用，相对于start
        /// </summary>
        /// <param name="userData"></param>
        /// 
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            SubscribeEvents();
            _openTween?.Kill();
            _openTween = OnOpenUIAnimation();
        }




        /// <summary>
        /// 每次关闭时调用
        /// </summary>
        /// <param name="isShutdown"></param>
        /// <param name="userData"></param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            UnspawnAllItemObjects();
            UnsubscribeEvents();
            base.OnClose(isShutdown, userData);
        }

        /// <summary>
        /// 每次暂停时调用
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
        }

        /// <summary>
        /// 每次暂停恢复时调用
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            return;
            m_CanvasGroup.alpha = 0f;
            StopAllCoroutines();
            StartCoroutine(m_CanvasGroup.FadeToAlpha(1f, FadeTime));
        }
        /// <summary>
        /// 每帧调用
        /// </summary>
        /// <param name="elapseSeconds"></param>
        /// <param name="realElapseSeconds"></param>
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
        }
        /// <summary>
        /// 界面遮挡时调用
        /// </summary>
        protected override void OnCover()

        {
            base.OnCover();
        }


        protected override void OnReveal()
        {
            base.OnReveal();
        }

        protected override void OnRefocus(object userData)
        {
            base.OnRefocus(userData);
        }



        protected override void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)

        {
            int oldDepth = Depth;
            base.OnDepthChanged(uiGroupDepth, depthInUIGroup);
            int deltaDepth = UGuiGroupHelper.DepthFactor * uiGroupDepth + DepthFactor * depthInUIGroup - oldDepth + OriginalDepth;
            GetComponentsInChildren(true, m_CachedCanvasContainer);
            for (int i = 0; i < m_CachedCanvasContainer.Count; i++)
            {
                m_CachedCanvasContainer[i].sortingOrder += deltaDepth;
            }

            m_CachedCanvasContainer.Clear();
        }

        public void SetBlocksRaycasts(bool enabled)
        {
            m_CanvasGroup.blocksRaycasts = enabled;
        }


        #region SubscribeEvents
        /// <summary>
        /// 订阅
        /// </summary>
        protected virtual void SubscribeEvents()
        {
        }
        /// <summary>
        /// 取消订阅
        /// </summary>
        protected virtual void UnsubscribeEvents()
        {
        }

        #endregion


        #region 对象池


        /// <summary>
        /// 从对象池获取一个Item (界面关闭时会自动Unspawn)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemTemple">Item实例化模板</param>
        /// <param name="instanceRoot">Item实例化到根节点</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象过期时间(过期后自动销毁)</param>
        /// <returns></returns>
        protected T SpawnItem<T>(GameObject itemTemple, Transform instanceRoot, float autoReleaseInterval = 5f, int capacity = 50, float expireTime = 50) where T : UIItemObject, new()
        {
            var itemTempleId = GetItemPoolId(itemTemple);
            GameFramework.ObjectPool.IObjectPool<T> pool;
            if (GameEntry.ObjectPool.HasObjectPool<T>(itemTempleId))
            {
                pool = GameEntry.ObjectPool.GetObjectPool<T>(itemTempleId);
                pool.AutoReleaseInterval = autoReleaseInterval;
                pool.Capacity = capacity;
                pool.ExpireTime = expireTime;
            }
            else
            {
                pool = GameEntry.ObjectPool.CreateSingleSpawnObjectPool<T>(itemTempleId, autoReleaseInterval, capacity, expireTime, 0);
                if (m_ItemPools == null) m_ItemPools = new List<IObjectPool<UIItemObject>>();
                m_ItemPools.Add((IObjectPool<UIItemObject>)(object)pool);
            }

            var spawn = pool.Spawn();
            if (spawn == null)
            {
                var itemInstance = Instantiate(itemTemple, instanceRoot);
                spawn = UIItemObject.Create<T>(itemInstance);
                pool.Register(spawn, true);
            }
            return spawn;
        }
        private void UnspawnAllItemObjects()
        {
            if (m_ItemPools == null) return;
            foreach (var item in m_ItemPools)
            {
                item.ReleaseAllUnused();

                item.UnspawnAll();
            }
        }
        private void DestroyAllItemPool()
        {
            if (m_ItemPools == null) return;

            for (int i = 0; i < m_ItemPools.Count; i++)
            {
                var item = m_ItemPools[i];
                GameEntry.ObjectPool.DestroyObjectPool(item);
            }
            m_ItemPools.Clear();
        }


        string GetItemPoolId(GameObject itemTemple)
        {
            return Utility.Text.Format("{0}.{1}", gameObject.GetInstanceID(), itemTemple.GetInstanceID());
        }


        /// <summary>
        /// 从对象池回收Item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemTemple">Item实例化模板</param>
        /// <param name="itemObject">要回收的Item实例</param>
        protected void UnspawnItem<T>(GameObject itemTemple, T itemObject) where T : UIItemObject, new()
        {
            UnspawnItem<T>(itemTemple, itemObject.gameObject);
        }
        /// <summary>
        /// 从对象池回收Item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemTemple">Item实例化模板</param>
        /// <param name="itemInstance">要回收的Item实例</param>
        protected void UnspawnItem<T>(GameObject itemTemple, GameObject itemInstance) where T : UIItemObject, new()
        {
            var itemTempleId = GetItemPoolId(itemTemple);
            if (!GameEntry.ObjectPool.HasObjectPool<T>(itemTempleId)) return;

            var pool = GameEntry.ObjectPool.GetObjectPool<T>(itemTempleId);
            pool.Unspawn(itemInstance);
        }
        /// <summary>
        /// 回收所有item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemTemple"></param>
        // protected void UnspawnAllItem<T>(GameObject itemTemple) where T : UIItemObject, new()
        // {
        //     var itemTempleId = GetItemPoolId(itemTemple);
        //     if (!GameEntry.ObjectPool.HasObjectPool<T>(itemTempleId)) return;

        //     var pool = GameEntry.ObjectPool.GetObjectPool<T>(itemTempleId);
        //     pool.ReleaseAllUnused();
        //     pool.UnspawnAll();
        // }
        #endregion


    }
}
