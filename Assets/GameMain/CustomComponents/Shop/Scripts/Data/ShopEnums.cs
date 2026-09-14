namespace Lokas
{
    /// <summary>
    /// 鍟嗗簵鍟嗗搧鐨勪环鏍肩被鍨嬨€?    /// </summary>
    public enum ShopPriceType
    {
        /// <summary>
        /// 鍏嶈垂棰嗗彇銆?        /// </summary>
        Free = 0,

        /// <summary>
        /// 娑堣€楅噾甯佽喘涔般€?        /// </summary>
        Gold = 1,

        /// <summary>
        /// 瑙傜湅骞垮憡鑾峰緱銆?        /// </summary>
        AD = 2,

        /// <summary>
        /// 鍐呰喘鍟嗗搧銆?        /// </summary>
        IAP = 3,
    }

    /// <summary>
    /// 鍟嗗簵濂栧姳鍙戞斁鐩爣銆?    /// </summary>
    public enum ShopRewardTarget
    {
        /// <summary>
        /// 閲戝竵濂栧姳銆?        /// </summary>
        Gold = 0,

        /// <summary>
        /// Game prop reward. The concrete prop is defined by ShopRewardData.propType.
        /// </summary>
        GameProp = 1,

        /// <summary>
        /// Common prop reward.
        /// </summary>
        CommonProp = 3,

        /// <summary>
        /// 鍘诲箍鍛婃潈鐩婏紝璐拱鍚庡叧闂彃灞忓拰 Banner 绛夐潪婵€鍔卞箍鍛娿€?        /// </summary>
        NoAds = 4,
    }

    /// <summary>
    /// 鍟嗗簵璐拱缁撴灉銆?    /// </summary>
    public enum ShopBuyResult
    {
        /// <summary>
        /// 璐拱鎴愬姛銆?        /// </summary>
        Success = 0,

        /// <summary>
        /// 鍟嗗搧閰嶇疆鏃犳晥銆?        /// </summary>
        InvalidItem = 1,

        /// <summary>
        /// 鍟嗗搧褰撳墠涓嶅彲瑙佹垨涓嶅彲璐拱銆?        /// </summary>
        NotVisible = 2,

        /// <summary>
        /// 閲戝竵涓嶈冻銆?        /// </summary>
        NotEnoughGold = 3,

        /// <summary>
        /// 鏀粯娴佺▼涓嶅彲鐢紝渚嬪骞垮憡鎴栧唴璐皻鏈帴鍏ャ€?        /// </summary>
        PaymentUnavailable = 4,

        /// <summary>
        /// 鏀粯娴佺▼宸插彂璧凤紝绛夊緟寮傛鍥炶皟銆?        /// </summary>
        Pending = 5,
    }
    public enum PropType
    {
        Hint = 0,
        Shuffle = 1,
        AddTime = 2,
    }
}
