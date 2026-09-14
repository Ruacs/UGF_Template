using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class ItemUI : MonoBehaviour
    {

        public Action<ItemUI, ItemUIData> onClick;

        public ItemUIData m_ItemUIData;

        [ReadOnly] public int watchedCount = 0;    //广告观看次数


        [SerializeField] private Transform m_cacheTransform;


        [SerializeField] private bool m_isOwned;
        [SerializeField] private bool m_isUnlocked;
        [SerializeField] private bool m_isEquiped;


        // 未解锁且非活动皮肤：显示锁定标记
        private Button _ItemUIBtn; // 缓存Button组件，避免重复获取

        public bool IsOwned { get => m_isOwned; protected set => m_isOwned = value; }
        public bool IsUnlocked { get => m_isUnlocked; protected set => m_isUnlocked = value; }
        public bool IsEquiped { get => m_isEquiped; protected set => m_isEquiped = value; }

        private void Awake()
        {
            // 缓存Button组件并校验
            _ItemUIBtn = transform.GetComponent<Button>();
            if (_ItemUIBtn != null)
            {
                _ItemUIBtn.onClick.RemoveAllListeners(); // 清空原有监听，避免重复绑定
                _ItemUIBtn.onClick.AddListener(OnSkinItemClick);
            }
            else
            {
                Debug.LogError("Button component not found on SkinItemUI!");
            }

            m_cacheTransform = transform.parent;
            SetTextFont();
        }


        public void SetTextFont()
        {
            TMP_FontAsset mainFont = UGuiForm.TMP_MainFont;
            if (mainFont != null)
            {
            
            }
        }

     


        public void UpdateUI()
        {

        }

  
        private void OnSkinItemClick()
        {
            onClick?.Invoke(this,m_ItemUIData);
        }

      
       

        // 对象池复用时，重置状态
        public void Recycle()
        {
      
        }


        public void PlayAnimation(float duration = 0.3f, float delay = 0)
        {
            transform?.DOKill();
            transform.DOScale(Vector3.one, duration).SetEase(Ease.InSine).SetDelay(delay);
        }

    }

}