using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lokas;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_RewardTipsBox : MonoBehaviour
{
    [SerializeField] private UI_ItemProp m_ItemPropPrefab;
    [SerializeField] private RectTransform m_ItemPropRoot;

    [SerializeField] private List<UI_ItemProp> m_ItemPropList;

    private bool m_isShow;
    private int m_ShowFrame = -1;

    void Awake()
    {
        transform.localScale = new Vector3(0, 1, 1);
        m_isShow = false;
    }

    void Update()
    {
        if (!m_isShow)
        {
            return;
        }

        // 忽略打开弹窗的这一帧，避免同一次点击把弹窗立刻关掉。
        if (Time.frameCount <= m_ShowFrame)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Hide();
            return;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Hide();
        }
    }


    public void Show(List<RewardData> rewardDatas)
    {
        if (m_isShow)
        {
            return;
        }

        GameEntry.Sound.PlaySound(SoundId.SFX_chest_tips);
        m_isShow = true;
        m_ShowFrame = Time.frameCount;
        transform.DOScaleX(1, 0.3f).SetEase(Ease.OutBack);

        CreateItemProp(rewardDatas);
    }


    public void Hide()
    {
        if (!m_isShow) return;
        m_isShow = false;
        transform.DOScaleX(0, 0.3f).SetEase(Ease.InBack);
    }



    private void CreateItemProp(List<RewardData> rewardDatas)
    {
        if (m_ItemPropList != null && m_ItemPropList.Count > 0) return;

        if (m_ItemPropList == null)
            m_ItemPropList = new List<UI_ItemProp>();

        foreach (var item in rewardDatas)
        {
            var itemPropUI = Instantiate(m_ItemPropPrefab, m_ItemPropRoot);
             if (GameEntry.CustomConfig.PropDataBaseSO.TryGetPropData(item.propType, out PropData propData))
             {
                 itemPropUI.RefreshUI(propData.sprite, "x" + item.Count);
             }
            m_ItemPropList.Add(itemPropUI);
        }
    }



}
