public static class NumberFormatUtil
{
    private static readonly string[] Units =
    {
        "",     // 10^0
        "K",    // 10^3
        "M",    // 10^6
        "B",    // 10^9
        "T",    // 10^12
        "P",    // 10^15
        "E"     // 10^18
    };

    /// <summary>
    /// 货币单位格式化
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string FormatToUnit(int value)
    {
        if (value < 1000)
            return value.ToString();

        double num = value;
        int unitIndex = 0;

        while (num >= 1000 && unitIndex < Units.Length - 1)
        {
            num /= 1000;
            unitIndex++;
        }

        return num.ToString("0.0") + Units[unitIndex];
    }

    /// <summary>
    /// 序数词后缀格式化  1 -> 1st  2 -> 2nd  3 -> 3rd  4 -> 4th
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string FormatToOrdinal(int value)
    {
        if (value <= 0)
            return value.ToString();

        int mod100 = value % 100;
        if (mod100 >= 11 && mod100 <= 13)
            return value + "th";

        switch (value % 10)
        {
            case 1: return value + "st";
            case 2: return value + "nd";
            case 3: return value + "rd";
            default: return value + "th";
        }
    }

    /// <summary>
    /// 百分号格式化      0.1 -> 10%
    /// </summary>
    /// <param name="value"></param>
    /// <param name="decimals">保留几位小数</param>
    /// <returns></returns>
    public static string FormatToPercent(float value, int decimals = 0)
    {
        return (value * 100f).ToString($"0.{new string('0', decimals)}") + "%";
    }

    /// <summary>
    /// 百分号格式化    10 -> 10%
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string FormatToPercent(int value)
    {
        return value + "%";
    }

    /// <summary>
    /// 时间格式化   3661 -> 01:01:01   61 -> 01:01
    /// </summary>
    /// <param name="totalSeconds"></param>
    /// <returns></returns>
    public static string FormatToTime(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        if (hours > 0)
        {
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
        else
        {
            return $"{minutes:D2}:{seconds:D2}";
        }
    }

    /// <summary>
    /// 时间格式化（小时+分钟）  44400 -> 12H20M
    /// </summary>
    /// <param name="totalSeconds"></param>
    /// <returns></returns>
    public static string FormatToHM(int totalSeconds, bool isCN = false)
    {
        if (totalSeconds < 0) totalSeconds = 0;

        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;

        if (isCN)
        {
            return $"{hours}时{minutes:D2}分";
        }
        return $"{hours}H{minutes:D2}M";
    }





    /// <summary>
    /// 时间格式化（小时+分钟+秒）  44411 -> 12H20M11S
    /// </summary>
    /// <param name="totalSeconds"></param>
    /// <returns></returns>
    public static string FormatToHMS(int totalSeconds, bool isCN = false)
    {
        if (totalSeconds < 0) totalSeconds = 0;

        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        if (isCN)
        {
            return $"{hours}时{minutes:D2}分{seconds:D2}秒";
        }

        return $"{hours}H{minutes:D2}M{seconds:D2}S";
    }

}
