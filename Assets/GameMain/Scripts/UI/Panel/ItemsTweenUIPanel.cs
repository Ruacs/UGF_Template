using Cysharp.Threading.Tasks;
using GameFramework.ObjectPool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public struct ItemInfo
    {
        public int count;
        public Sprite sprite;
        public Vector2 startPos;
        public Vector2 endPos;
        public Action onComplete;
        public bool useOneByOne;
        public float oneByOneDelay;
    }

    public class ItemsTweenUIPanel : UGuiForm
    {
        public ItemsRoot itemsRootPrefab;
        [SerializeField]
        private List<ItemsRoot> m_itemsRoots;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            itemsRootPrefab = transform.Find("ItemsRootPrefab").GetComponent<ItemsRoot>();
            m_itemsRoots = new List<ItemsRoot>();
        }


        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            ItemsTween((ItemInfo)userData);

        }


        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);

        }

        private void ItemsTween(ItemInfo itemInfo)
        {
            ItemsRoot itemsRoot = Instantiate(itemsRootPrefab, transform);
            itemsRoot.gameObject.SetActive(true);
            m_itemsRoots.Add(itemsRoot);
            itemsRoot.onComplete = async () =>
            {
                m_itemsRoots.Remove(itemsRoot);
                if (m_itemsRoots.Count == 0)
                {
                    await UniTask.Delay(1000);
                    //Close();
                }
            };
            if (itemInfo.useOneByOne)
                itemsRoot.ItemTweenOneByOne(itemInfo, itemInfo.oneByOneDelay > 0 ? itemInfo.oneByOneDelay : 0.08f);
            else
                itemsRoot.ItemTween(itemInfo);

        }

        public static void ShowItemTween(ItemInfo itemInfo)
        {
            if (GameEntry.UI.HasUIForm(UIFormId.ItemsTweenUIPanel))
            {
                ItemsTweenUIPanel itemsTweenUIPanel = GameEntry.UI.GetUIForm(UIFormId.ItemsTweenUIPanel) as ItemsTweenUIPanel;
                if (itemsTweenUIPanel != null)
                {
                    itemsTweenUIPanel.ItemsTween(itemInfo);
                }
            }
            else
            {
                GameEntry.UI.OpenUIForm(UIFormId.ItemsTweenUIPanel, itemInfo);

            }


        }

    }
}
