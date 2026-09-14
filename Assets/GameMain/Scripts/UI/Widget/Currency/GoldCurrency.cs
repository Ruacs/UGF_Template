using Lokas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lokas
{
    public class GoldCurrency : CurrencyBase
    {
        public override int Amount => GameEntry.SaveData.Money;
        public override string CurrencyName { get; protected set; } = "Gold";
        private int lastAmount;

        protected override void Awake()
        {
            base.Awake();
            OnAmountChange += GoldCurrency_OnAmountChange;
        }
        public override void Refresh()
        {
            lastAmount = Amount;
            tmp_Amount.text = Amount.ToString();

        }



        /// <summary>
        /// 更新金币显示
        /// </summary>
        private void GoldCurrency_OnAmountChange(object sender, ICurrency.OnAmountChangeEvent e)
        {
           
            int from = lastAmount;
            int to = e.Amount;

            if (from == to) return; // 无变化直接返回

            StopAllCoroutines(); // 停止之前的动画
            StartCoroutine(tmp_Amount.ValueTween(from, to));
            lastAmount = to;
        }


        public override void Add(int value)
        {
            GameEntry.SaveData.Money += value;
            NotifyAmountChange();
        }

        public override bool Spend(int value)
        {
            if (GameEntry.SaveData.Money < value) return false;

            GameEntry.SaveData.Money -= value;
            NotifyAmountChange();
            return true;
        }

        /// <summary>
        /// 播放金币飞向 UI 的动画，并在动画结束后增加金币
        /// </summary>
        /// <param name="value">
        /// 实际增加的金币数量（动画完成后才会结算）
        /// </param>
        /// <param name="startPos">
        /// 金币动画起点（UI 本地坐标）
        /// 如果不传，则默认从 Vector2.zero 开始
        /// </param>
        public void AddWithItemTween(int value,Action onComplete = null, Vector2? startPos = null)
        {
            // 如果没有传起点，使用默认位置
            Vector2 realStartPos = startPos ?? Vector2.zero;

            // 构建金币飞行动画参数
            ItemInfo itemInfo = new ItemInfo
            {
                startPos = realStartPos,
                // 动画终点：金币 UI 图标位置（转成当前 UI 坐标系）
                endPos = rt_icon.WorldToLocalPointInRect(transform.parent.parent.GetComponent<RectTransform>(),
                    GameEntry.UI.UICamera
                ),
                sprite = Icon,
                onComplete = () =>
                {
                    Add(value);

                    GameEntry.Sound.PlayUISound(SoundId.UI_Coins);

                    onComplete?.Invoke();
                }
            };


            GameEntry.Sound.PlayUISound(SoundId.UI_Coins);

            // 播放金币飞行动画
            ItemsTweenUIPanel.ShowItemTween(itemInfo);
        }

        /// <summary>
        /// 加钱按钮
        /// </summary>
        public void OnClickAdd()
        {
            GameEntry.Sound.PlayUISound(SoundId.UI_Click);
            Debug.Log(CurrencyName + " OnClickAdd");
            AddWithItemTween(200);
        }




    }

}