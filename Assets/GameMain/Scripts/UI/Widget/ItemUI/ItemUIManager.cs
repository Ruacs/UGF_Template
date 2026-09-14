
using GameFramework.ObjectPool;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public class ItemUIManager : UnitySingleton<ItemUIManager>
    {

        public ItemUI skinItemUIPrefab;

        [SerializeField] private Transform m_InstanceRoot;
        [SerializeField] private List<ItemUI> m_ActiveSkinItemUIList;
        [SerializeField] private int m_InstancePoolCapacity = 13;
        [SerializeField] private int m_ExpireTime = 60;
        private IObjectPool<ItemUIObject> m_SkinItemObjectPool;


        public List<ItemUI> ActiveSkinItemUIList => m_ActiveSkinItemUIList;

        public int ActiveCount => m_ActiveSkinItemUIList.Count;

        private int s_SerialId;
        private void Start()
        {
            s_SerialId = 0;
            m_ActiveSkinItemUIList = new List<ItemUI>();
            m_SkinItemObjectPool = GameEntry.ObjectPool.CreateSingleSpawnObjectPool<ItemUIObject>("SkinItemPool");
            m_SkinItemObjectPool.ExpireTime = m_ExpireTime;
            m_SkinItemObjectPool.Capacity = m_InstancePoolCapacity;
        }


        #region SkinItem对象池

        public ItemUI ShowSkinItemUI(Transform transform)
        {
            ItemUI skinItemUI = CreateSkinItemUI();
            skinItemUI.name = "SkinItemUI_" + s_SerialId;
            m_ActiveSkinItemUIList.Add(skinItemUI);
            skinItemUI.transform.SetParent(transform);

            s_SerialId++;

            return skinItemUI;
        }

        public void HideSkinItemUI(ItemUI skinItem)
        {
            skinItem.Recycle();
            m_ActiveSkinItemUIList.Remove(skinItem);
            m_SkinItemObjectPool.Unspawn(skinItem);
        }

        public void HideAll()
        {
            HideRange(0, ActiveCount-1);
        }

        public void HideRange(int start, int end)
        {
           for (int i = end; i >= start; i--)
           {
               HideSkinItemUI(m_ActiveSkinItemUIList[i]);
           }
           
        }


        private ItemUI CreateSkinItemUI()
        {
            ItemUIObject skinItemObject = m_SkinItemObjectPool.Spawn();
            ItemUI skinItemUI;
            if (skinItemObject != null)
            {
                skinItemUI = (ItemUI)skinItemObject.Target;
            }
            else
            {
                skinItemUI = Instantiate(skinItemUIPrefab, m_InstanceRoot);
                skinItemUI.transform.localScale = Vector3.one;
                m_SkinItemObjectPool.Register(ItemUIObject.Create(skinItemUI), true);
            }

            return skinItemUI;
        }


        


        #endregion

    }

}