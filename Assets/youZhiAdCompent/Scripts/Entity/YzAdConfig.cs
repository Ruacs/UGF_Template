namespace YzAdComponent
{

    public class YzAdConfig
    {
        /// <summary>
        /// 位置
        /// </summary>
        public int location = -1;


        /// <summary>
        /// 是否显示Banner广告 false：不显示 true： 显示
        /// </summary>
        public bool show_banner_ad = true;

        /// <summary>
        /// 是否显示插屏广告 false：不显示 true： 显示
        /// </summary>
        public bool show_intersitial_ad = true;


        public override string ToString()
        {
            return "#location=" + location + " #show_banner_ad=" + show_banner_ad + " #show_intersitial_ad=" + show_intersitial_ad;
        }
    }

}