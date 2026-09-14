using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI; 

namespace Lokas
{
    public class ItemsRoot: MonoBehaviour
    {
        public List<Image> m_ItemIcons;

        public List<RectTransform> m_ItemRTs;
        private int m_ItemCount = 0;
        private RectTransform m_ItemEndPosRT;
        private Transform m_ItemsParent;
        private int curIndex = 0;
        public Action onComplete;

        private void Awake()
        {
            m_ItemsParent = transform.Find("ItemsParent");
            m_ItemEndPosRT = transform.Find("EndPos").GetComponent<RectTransform>();
           
        }

        public void ItemTween(ItemInfo itemInfo)
        {
            onComplete += () =>
            {
                Destroy(this.gameObject);
            };


            m_ItemCount = itemInfo.count > 0 ? itemInfo.count : 20;
            if (itemInfo.count > 50)
            {
                m_ItemCount = 30;
            }

            m_ItemIcons?.Clear();
            m_ItemIcons = new List<Image>(m_ItemCount);

            for (int i = 0; i < m_ItemCount; i++)
            {
                GameObject item = Instantiate(transform.GetChild(0).gameObject, m_ItemsParent);
                item.SetActive(true);
                Image img_Item = item.transform.Find("Icon").GetComponent<Image>();
                img_Item.sprite = itemInfo.sprite != null ? itemInfo.sprite : transform.GetChild(0).GetComponent<Image>().sprite;
                m_ItemIcons.Add(img_Item);
                item.name = "Item_" + i;
            }

            curIndex = 0;
            for (int i = 0; i < m_ItemCount; i++)
            {
                m_ItemIcons[i].rectTransform.localPosition = itemInfo.startPos;
                Vector2 offset = new Vector2(UnityEngine.Random.Range(-200f, 200f), UnityEngine.Random.Range(-200f, 200f));
                m_ItemIcons[i].rectTransform.DOLocalMove(itemInfo.startPos + offset, UnityEngine.Random.Range(0.5f, 1.0f)).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    curIndex += 1;
                    if (curIndex >= m_ItemCount)
                    {
                        int index = 0;
                        for (int i = 0; i < m_ItemCount; i++)
                        {
                            m_ItemIcons[i].rectTransform.DOLocalMove(itemInfo.endPos, UnityEngine.Random.Range(0.5f, 1.0f)).SetEase(Ease.InBack).OnComplete(() =>
                            {
                                index++;
                                if (index >= m_ItemCount)
                                {
                                    
                                    itemInfo.onComplete();
                                    onComplete.Invoke();
                                }
                            });
                        }
                    }
                });
            }


        }


        /// <summary>
        /// 一个接一个飞向目标位置，每个之间有延迟间隔
        /// </summary>
        public void ItemTweenOneByOne(ItemInfo itemInfo, float intervalDelay = 0.08f)
        {
            onComplete += () =>
            {
                Destroy(gameObject);
            };

            m_ItemCount = itemInfo.count > 0 ? itemInfo.count : 20;
            if (itemInfo.count > 50)
                m_ItemCount = 30;

            m_ItemRTs?.Clear();
            m_ItemRTs = new List<RectTransform>(m_ItemCount);

            for (int i = 0; i < m_ItemCount; i++)
            {
                RectTransform item = Instantiate(transform.GetChild(0), m_ItemsParent) as RectTransform;
                item.gameObject.SetActive(true);
                Image img_Item = item.transform.Find("Icon").GetComponent<Image>();
                img_Item.sprite = itemInfo.sprite != null ? itemInfo.sprite : transform.GetChild(0).GetComponent<Image>().sprite;
                img_Item.SetNativeSize();
                item.localPosition = itemInfo.startPos;
                item.localScale = Vector3.zero;
                m_ItemRTs.Add(item);
                item.name = "Item_" + i;
            }

            int completed = 0;
            for (int i = 0; i < m_ItemCount; i++)
            {
                var rt = m_ItemRTs[i];
                float delay = i * intervalDelay;

                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(delay);
                seq.Append(rt.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack));
                seq.Append(rt.DOLocalMove(itemInfo.endPos, 0.45f).SetEase(Ease.OutQuad));
                seq.OnComplete(() =>
                {
                    completed++;
                    if (completed >= m_ItemCount)
                    {
                        itemInfo.onComplete?.Invoke();
                        onComplete?.Invoke();
                    }
                });
            }
        }

    }



}