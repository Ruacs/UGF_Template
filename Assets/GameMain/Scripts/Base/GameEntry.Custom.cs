using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public partial class GameEntry : MonoBehaviour
    {
        public static CustomConfigComponent CustomConfig { get; private set; }
        public static SaveDataComponent SaveData { get; private set; }
        public static FlyItemUIComponent FlyItem { get; private set; }
        public static FloatingTextComponent FloatingText { get; private set; }
        public static TaskComponent Task { get; private set; }
        public static TMPFontComponent TMPFont { get; private set; }
        public static RedDotComponent RedDot { get; private set; }
        public static ShopComponent Shop { get; private set; }
        public static RankComponent Rank { get; private set; }
        public static TestModeComponent TestMode { get; private set; }

        public static DefaultGameManagerComponent DefaultGameManager { get; private set; }

        public static SubGameRuntimeRegistry SubGames => GameManager != null ? GameManager.SubGames : null;

        private static void InitCustomComponents()
        {
            CustomConfig = UnityGameFramework.Runtime.GameEntry.GetComponent<CustomConfigComponent>();
            SaveData = UnityGameFramework.Runtime.GameEntry.GetComponent<SaveDataComponent>();
            if (SaveData == null && Base != null)
            {
                SaveData = Base.gameObject.AddComponent<SaveDataComponent>();
                Log.Info("[GameEntry] SaveDataComponent was missing and has been added at runtime.");
            }

            if (SaveData != null)
            {
                GameManager.InitializeSubGames(SaveData.Store);
                SaveData.Initialize();
            }
            else
            {
                Log.Warning("[GameEntry] SaveDataComponent is missing. Please add it to the Game Framework entry object.");
            }

            FlyItem = UnityGameFramework.Runtime.GameEntry.GetComponent<FlyItemUIComponent>();
            FloatingText = UnityGameFramework.Runtime.GameEntry.GetComponent<FloatingTextComponent>();
            Task = UnityGameFramework.Runtime.GameEntry.GetComponent<TaskComponent>();
            TMPFont = UnityGameFramework.Runtime.GameEntry.GetComponent<TMPFontComponent>();
            RedDot = UnityGameFramework.Runtime.GameEntry.GetComponent<RedDotComponent>();
            Shop = UnityGameFramework.Runtime.GameEntry.GetComponent<ShopComponent>();
            Rank = UnityGameFramework.Runtime.GameEntry.GetComponent<RankComponent>();
            TestMode = UnityGameFramework.Runtime.GameEntry.GetComponent<TestModeComponent>();

            DefaultGameManager = UnityGameFramework.Runtime.GameEntry.GetComponent<DefaultGameManagerComponent>();
        }
    }
}

