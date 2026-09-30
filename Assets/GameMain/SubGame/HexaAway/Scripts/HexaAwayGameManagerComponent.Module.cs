namespace Lokas
{
    public sealed partial class HexaAwayGameManagerComponent
    {
        private HexaAwayGameData m_ModuleData;
        public override string SceneConfigKey => "Scene.HexaAway";
        public override System.Type GameProcedureType => typeof(ProcedureGameHexaAway);
        public override int CurrentLevel => m_ModuleData?.CurrentLevel ?? 0;
        public override int DisplayLevel => CurrentLevel + 1;
        public override void InitializeModule(SaveDataStore store)
        {
            var data = store.Get<HexaAwayGameData>();
            if (data == null) { data = new HexaAwayGameData(); store.Register(data); }
            if (ReferenceEquals(data, m_ModuleData)) return;
            if (m_ModuleData != null) m_ModuleData.OnCurrentLevelChanged -= NotifyProgressChanged;
            m_ModuleData = data;
            m_ModuleData.OnCurrentLevelChanged += NotifyProgressChanged;
        }
        public override bool CanGrantProp(PropType type) =>
            type == PropType.Hint || type == PropType.Shuffle || type == PropType.AddTime;
        public override bool TryGrantProp(PropType type, int count)
        {
            if (m_ModuleData == null || count <= 0 || !CanGrantProp(type)) return false;
            var target = type == PropType.Hint ? HexaAwayPropType.AddMove :
                type == PropType.Shuffle ? HexaAwayPropType.Drill : HexaAwayPropType.Hammer;
            m_ModuleData.AddPropCount(target, count);
            return true;
        }
    }

    // 既有玩法内快捷入口随包持有，公共框架不依赖此成员。
    public partial class GameEntry
    {
        public static HexaAwayGameManagerComponent HexaAway => SubGames?.Get<HexaAwayGameManagerComponent>(GameMode.HexaAway);
    }
}
