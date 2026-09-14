using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 排行榜单条数据。
    /// </summary>
    [Serializable]
    public class RankData
    {
        /// <summary>排名（从 1 开始，0 表示未排名）。</summary>
        public int Rank;

        /// <summary>玩家唯一 Id。</summary>
        public string PlayerId;

        /// <summary>玩家名字。</summary>
        public string PlayerName;

        /// <summary>头像资源 Id / 资源名。</summary>
        public string AvatarId;

        /// <summary>头像框资源 Id / 资源名。</summary>
        public string AvatarFrameId;

        /// <summary>积分（用于排序）。</summary>
        public int Score;

        /// <summary>物品槽显示的数量（例如金币、星星）。</summary>
        public int ItemCount;

        /// <summary>物品槽图标资源 Id。</summary>
        public string ItemIconId;

        /// <summary>是否为本地玩家（UI 中高亮）。</summary>
        public bool IsSelf;

        /// <summary>是否已领取本排名奖励。</summary>
        public bool RewardClaimed;

        /// <summary>该排名对应的奖励宝箱皮肤（Gold / Silver / Copper 等）。</summary>
        public RankData() { }

        public RankData(string playerId, string playerName, int score, bool isSelf = false)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Score = score;
            IsSelf = isSelf;
        }
    }
}
