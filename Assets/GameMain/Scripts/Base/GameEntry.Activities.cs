

namespace Lokas
{
    public partial class GameEntry
    {
        /// <summary>活动业务宿主。它由 GameEntry 上的 <see cref="ActivityComponent"/> 持有。</summary>
        public static ActivityModuleHost Activities => Activity != null ? Activity.Host : null;

        private static void InitActivities()
        {
            if (Activity == null)
            {
                UnityGameFramework.Runtime.Log.Error("[GameEntry] ActivityComponent is missing. Add it to the GameEntry object.");
                return;
            }

            Activity.Initialize();
        }

        internal static void ClearActivityComponent(ActivityComponent component)
        {
            if (object.ReferenceEquals(Activity, component)) Activity = null;
        }
    }
}
