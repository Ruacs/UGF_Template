using System;

namespace Lokas
{
    /// <summary>
    /// 鍟嗗簵濂栧姳閰嶇疆銆備竴浠跺晢鍝佸彲浠ラ厤缃鏉″鍔憋紝鐢ㄤ簬缁勫悎鍙戞斁閬撳叿銆侀噾甯佸拰鍘诲箍鍛婃潈鐩娿€?    /// </summary>
    [Serializable]
    public class ShopRewardData
    {
        /// <summary>
        /// 濂栧姳鍙戞斁鐩爣銆傞€夋嫨 NoAds 鏃朵細蹇界暐 propType 鍜?count銆?        /// </summary>
        public ShopRewardTarget target = ShopRewardTarget.Gold;

        /// <summary>
        /// Prop type, used by game prop rewards.
        /// </summary>
        public PropType propType = PropType.Hint;

        /// <summary>
        /// 濂栧姳鏁伴噺銆傞噾甯佽〃绀洪噾甯佹暟閲忥紝閬撳叿琛ㄧず閬撳叿鏁伴噺銆?        /// </summary>
        public int count = 1;

        /// <summary>
        /// 涓€娆℃€ф潈鐩婂鍔辨弿杩版枃鏈紝渚嬪鍘诲箍鍛婅鏄庛€備粎 NoAds 绛夋潈鐩婄被濂栧姳浣跨敤銆?        /// </summary>
        public string descriptionText;
    }
}


