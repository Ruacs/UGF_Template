using System;
using System.Collections.Generic;
using Ads;
using GameFramework.DataTable;
using UnityEngine;
using UnityEngine.Serialization;
using UnityGameFramework.Runtime;
using Random = UnityEngine.Random;
namespace Lokas
{
    [Serializable]
    internal class RankListSaveData
    {
        public List<RankData> Ranks = new();
    }

    [Serializable]
    public class RankCycleData
    {
        [FormerlySerializedAs("TargetSpecialTileId")]
        public int TargetItemId;
        public int DurationSeconds;
        public long EndUnixTimeSeconds;
        /// <summary>周期所属自然日（本地日期，格式 yyyy-MM-dd）。跨日时据此触发重置。</summary>
        public string DateKey;

        /// <summary>本周期内玩家是否已完成过一局（决定榜单规模与玩家名字）。</summary>
        public bool HasPlayed;

        public RankCycleData() { }

        public RankCycleData(int targetItemId, int durationSeconds, long endUnixTimeSeconds, string dateKey)
        {
            TargetItemId = targetItemId;
            DurationSeconds = durationSeconds;
            EndUnixTimeSeconds = endUnixTimeSeconds;
            DateKey = dateKey;
        }

        public RankCycleData Clone()
        {
            return new RankCycleData(TargetItemId, DurationSeconds, EndUnixTimeSeconds, DateKey)
            {
                HasPlayed = HasPlayed,
            };
        }
    }

    /// <summary>
    /// 排行榜组件：维护排名数据、负责排序/刷新与事件通知。
    /// 同时维护“排行周期”与倒计时，周期结束时自动清空排行榜。
    /// </summary>
    public class RankComponent : GameFrameworkComponent
    {
        /// <summary>排行榜周期固定时长（1 天 = 24 小时）。</summary>
        public const int DailyDurationSeconds = 24 * 60 * 60;

        /// <summary>排行榜解锁所需的最低关卡数（当前关卡 &gt; 该值才解锁）。</summary>
        public int UnlockLevelThreshold = 1;

        /// <summary>首次（玩家尚未完成一局）榜单中虚假玩家数量。</summary>
        public const int InitialFakeCount = 25;

        /// <summary>首次榜单最高分（偶数）。</summary>
        public const int InitialMaxScore = 60;

        /// <summary>玩家完成首局后虚假玩家数量，总条数 = FullFakeCount + 1 = 100。</summary>
        public const int FullFakeCount = 99;

        /// <summary>玩家完成首局后榜单最高分（偶数）。</summary>
        public const int FullMaxScore = 360;

        /// <summary>虚假玩家最低积分（偶数，可多个并列）。</summary>
        public const int FakeMinScore = 2;

        /// <summary>本地玩家在排行榜中的固定标识。</summary>
        private const string SelfPlayerId = "self";

        /// <summary>本地玩家默认名字。</summary>
        private const string SelfPlayerName = "Player";

        /// <summary>所有玩家统一使用的头像 Id。</summary>
        private const string DefaultAvatarId = "1";

        private const string DateKeyFormat = "yyyy-MM-dd";

        /// <summary>排行榜周期持久化存储 Key。</summary>
        private const string CycleSaveKey = "RankCycle";

        /// <summary>排行榜条目持久化存储 Key。</summary>
        private const string RanksSaveKey = "RankList";

        /// <summary>待领排行榜奖励名次持久化存储 Key（值为 1-6 表示有待领奖励，0 表示无）。</summary>
        private const string PendingRankRewardKey = "PendingRankReward";

        public const int MaxRankRewardRank = 6;

        private readonly List<int> m_TargetItemIds = new();
        [SerializeField] private RankTargetConfigSO m_TargetConfig;
        [SerializeField, FormerlySerializedAs("m_DefaultTargetSpecialTileIds")]
        private int[] m_DefaultTargetItemIds = { 201, 202, 203 };

        [SerializeField] private List<RankData> m_Ranks = new();
        [SerializeField] private RankCycleData m_CurrentCycle = new();
        [Header("Fake Rank Progress")]
        [SerializeField, Range(0f, 2f)] private float m_FakeSettlementTopNearScale = 1.12f;
        [SerializeField, Range(0f, 2f)] private float m_FakeSettlementTopFarScale = 0.35f;
        [SerializeField, Range(0f, 2f)] private float m_FakeSettlementBottomNearScale = 0.15f;
        [SerializeField, Range(0f, 2f)] private float m_FakeSettlementBottomFarScale = 0.04f;
        [SerializeField, Min(1)] private int m_FakeSettlementNearbyRankSpan = 10;
        [SerializeField, Min(0)] private int m_FakeSettlementMaxExtraDelta = 4;

        private int m_LastRemainingSeconds = -1;
        private long m_LastHandledCycleEndUnixTimeSeconds = -1;
        private bool m_Initialized;

        /// <summary>排名列表变化时触发，参数为排序后的只读列表。</summary>
        public event Action<IReadOnlyList<RankData>> OnRanksChanged;

        /// <summary>本地玩家排名变化时触发（oldRank, newRank），0 表示未上榜。</summary>
        public event Action<int, int> OnSelfRankChanged;

        /// <summary>排行榜周期发生变化时触发（目标牌/结束时间等）。</summary>
        public event Action<RankCycleData> OnCycleChanged;

        /// <summary>排行榜剩余秒数变化时触发（通常每秒一次）。</summary>
        public event Action<int> OnCycleCountdownChanged;

        /// <summary>排行榜周期结束时触发（会先清空排行榜）。</summary>
        public event Action<RankCycleData> OnCycleExpired;

        /// <summary>当前完整排名列表（只读）。</summary>
        public IReadOnlyList<RankData> Ranks => m_Ranks;

        /// <summary>本地玩家。</summary>
        public RankData SelfRank => m_Ranks.Find(r => r != null && r.IsSelf);

        /// <summary>榜单条数。</summary>
        public int Count => m_Ranks.Count;

        /// <summary>可选排行榜目标项 Id。</summary>
        public IReadOnlyList<int> TargetItemIds => GetTargetItemIds();

        /// <summary>当前排行榜周期数据。</summary>
        public RankCycleData CurrentCycle => m_CurrentCycle;

        /// <summary>当前排行榜目标项 Id。</summary>
        public int CurrentTargetItemId => m_CurrentCycle?.TargetItemId ?? 0;

        /// <summary>当前周期剩余秒数。</summary>
        public int CycleRemainingSeconds => GetRemainingSeconds();

        /// <summary>当前周期剩余时间（HH:mm:ss）。</summary>
        public string CycleRemainingTimeText => FormatRemainingTime(CycleRemainingSeconds);

        /// <summary>排行榜是否已解锁。</summary>
        public bool IsUnlocked => AdsServerConfig.TryGetPrimaryLevel(out int level)
            && level >= AdsServerConfig.Common.RankUnlockLevel;


        /// <summary>当前排行榜目标项对应的 Sprite（供 UI 显示）。</summary>
        public Sprite CurrentTargetItemSprite => m_TargetConfig != null ? m_TargetConfig.GetIcon(CurrentTargetItemId) : null;

        /// <summary>当前排行榜目标项标题本地化 Key。</summary>
        public string CurrentTargetTitleKey => m_TargetConfig != null ? m_TargetConfig.GetTitleKey(CurrentTargetItemId) : null;

        /// <summary>当前排行榜目标项描述本地化 Key。</summary>
        public string CurrentTargetDescriptionKey => m_TargetConfig != null ? m_TargetConfig.GetDescriptionKey(CurrentTargetItemId) : null;

        [Obsolete("Use CurrentTargetItemSprite instead.")]
        public Sprite CurrentTargetTileSprite => CurrentTargetItemSprite;


        public void SetRankTargetConfig(RankTargetConfigSO rankTargetConfigSO)
        {
            m_TargetConfig = rankTargetConfigSO;
        }

        /// <summary>
        /// 启动排行榜组件：由外部在 SaveData 等依赖就绪后调用。
        /// 组件自身 Awake 早于 SaveData 初始化，不能在 Awake 里访问存档，需通过本方法显式启动。
        /// </summary>
        public void StartUp()
        {
            if (m_Initialized) return;
            LoadCycle();
            TryLoadRanks();
            m_Initialized = true;
            EnsureDailyCycle();
            TickCycle();
        }



        private void Update()
        {
            if (!m_Initialized) return;
            EnsureDailyCycle();
            TickCycle();
        }

        /// <summary>从持久化存储加载 RankCycleData（如无则保持默认值，由 EnsureDailyCycle 生成）。</summary>
        private void LoadCycle()
        {
            var saved = GameEntry.SaveData.GetData<RankCycleData>(CycleSaveKey, null);
            if (saved != null && !string.IsNullOrEmpty(saved.DateKey))
            {
                m_CurrentCycle = saved;
                m_LastHandledCycleEndUnixTimeSeconds = -1;
                m_LastRemainingSeconds = -1;
            }
        }

        /// <summary>把当前 RankCycleData 写入持久化存储。</summary>
        private void SaveCycle()
        {
            if (m_CurrentCycle == null) return;
            GameEntry.SaveData.SetData(CycleSaveKey, m_CurrentCycle);
        }

        /// <summary>用一组数据重置排行榜并排序。</summary>
        public void SetRanks(IEnumerable<RankData> ranks)
        {
            int oldSelfRank = SelfRank?.Rank ?? 0;

            m_Ranks.Clear();
            if (ranks != null)
            {
                foreach (var r in ranks)
                {
                    if (r != null) m_Ranks.Add(r);
                }
            }

            Resort();
            NotifyChanged(oldSelfRank);
        }

        /// <summary>清空排行榜。</summary>
        public void Clear()
        {
            if (m_Ranks.Count == 0) return;

            int oldSelfRank = SelfRank?.Rank ?? 0;
            m_Ranks.Clear();
            SaveRanks();
            OnRanksChanged?.Invoke(m_Ranks);
            if (oldSelfRank != 0) OnSelfRankChanged?.Invoke(oldSelfRank, 0);
        }

        /// <summary>
        /// 开启新的排行榜周期（固定 24H）：自动从可选目标特殊牌中选一个。
        /// </summary>
        public RankCycleData StartNewCycle()
        {
            return StartNewCycle(DailyDurationSeconds, PickRandomTargetItemId());
        }

        /// <summary>
        /// 开启新的排行榜周期：自动从可选目标特殊牌中选一个，并设置倒计时。
        /// </summary>
        public RankCycleData StartNewCycle(int durationSeconds)
        {
            int targetId = PickRandomTargetItemId();
            return StartNewCycle(durationSeconds, targetId);
        }

        /// <summary>
        /// 开启新的排行榜周期：指定目标项与周期秒数，并标记当日 DateKey。
        /// </summary>
        public RankCycleData StartNewCycle(int durationSeconds, int targetItemId)
        {
            if (durationSeconds <= 0)
            {
                Log.Warning("[RankComponent] StartNewCycle failed: durationSeconds={0}", durationSeconds);
                return m_CurrentCycle;
            }

            int validTargetId = NormalizeTargetItemId(targetItemId);
            long now = GetNowUnixTimeSeconds();
            long endUnixTime = now + durationSeconds;
            string todayKey = GetTodayKey();
            m_CurrentCycle = new RankCycleData(validTargetId, durationSeconds, endUnixTime, todayKey);

            m_LastHandledCycleEndUnixTimeSeconds = -1;
            m_LastRemainingSeconds = -1;
            SaveCycle();
            NotifyCycleChanged();
            TickCycle();
            return m_CurrentCycle;
        }

        /// <summary>
        /// 用外部时间戳同步排行榜周期（例如服务端下发）。
        /// </summary>
        public void SetCycle(RankCycleData cycleData)
        {
            if (cycleData == null)
            {
                StopCycle();
                return;
            }

            int validTargetId = NormalizeTargetItemId(cycleData.TargetItemId);
            int duration = Mathf.Max(0, cycleData.DurationSeconds);
            long endUnixTime = cycleData.EndUnixTimeSeconds < 0 ? 0 : cycleData.EndUnixTimeSeconds;
            string dateKey = string.IsNullOrEmpty(cycleData.DateKey) ? GetTodayKey() : cycleData.DateKey;
            m_CurrentCycle = new RankCycleData(validTargetId, duration, endUnixTime, dateKey);

            m_LastHandledCycleEndUnixTimeSeconds = -1;
            m_LastRemainingSeconds = -1;
            SaveCycle();
            NotifyCycleChanged();
            TickCycle();
        }

        /// <summary>停止排行榜周期倒计时，不清空当前排行榜数据。</summary>
        public void StopCycle()
        {
            if (m_CurrentCycle == null)
                m_CurrentCycle = new RankCycleData();
            else
                m_CurrentCycle.EndUnixTimeSeconds = 0;

            m_LastHandledCycleEndUnixTimeSeconds = -1;
            m_LastRemainingSeconds = -1;
            SaveCycle();
            NotifyCycleChanged();
            NotifyCountdownChanged(0);
        }

        public RankData GetByRank(int rank)
        {
            for (int i = 0; i < m_Ranks.Count; i++)
                if (m_Ranks[i].Rank == rank) return m_Ranks[i];
            return null;
        }

        public RankData GetByPlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;
            for (int i = 0; i < m_Ranks.Count; i++)
                if (m_Ranks[i].PlayerId == playerId) return m_Ranks[i];
            return null;
        }

        /// <summary>添加或更新一条记录（按 PlayerId 匹配）。</summary>
        public void AddOrUpdate(RankData data)
        {
            if (data == null) return;
            int oldSelfRank = SelfRank?.Rank ?? 0;

            var existed = GetByPlayerId(data.PlayerId);
            if (existed != null)
            {
                existed.PlayerName = data.PlayerName;
                existed.AvatarId = data.AvatarId;
                existed.AvatarFrameId = data.AvatarFrameId;
                existed.Score = data.Score;
                existed.ItemCount = data.ItemCount;
                existed.ItemIconId = data.ItemIconId;
                existed.IsSelf = data.IsSelf;
            }
            else
            {
                m_Ranks.Add(data);
            }

            Resort();
            NotifyChanged(oldSelfRank);
        }

        /// <summary>确保榜单已扩展到完整 50 条（结算界面首次展示时调用，保证玩家一进入就看到完整榜单）。</summary>
        public void EnsureFullRankList()
        {
            if (m_CurrentCycle != null && !m_CurrentCycle.HasPlayed)
            {
                m_CurrentCycle.HasPlayed = true;
                SaveCycle();
                BuildRanks(FullFakeCount, FullMaxScore, SelfPlayerName, preservedSelfScore: SelfRank?.Score ?? 0);
            }
        }

        /// <summary>
        /// 累加本地玩家当前收集数量（内部复用 <see cref="UpdateSelfScore"/> 走首局扩榜与重排逻辑）。
        /// </summary>
        public void AddSelfScore(int delta)
        {
            if (delta == 0) return;

            var self = SelfRank;
            if (self == null)
            {
                Log.Warning("[RankComponent] AddSelfScore failed: self not found.");
                return;
            }

            int newScore = Mathf.Max(0, self.Score + delta);
            UpdateSelfScore(newScore);
        }

        /// <summary>
        /// 在结算页首次展示前，先让假玩家分数提前增长，
        /// 这样用户一打开页面看到的就是“别人已经打完一段时间后的榜单”。
        /// </summary>
        public void ApplyFakeSettlementAmbientProgress(int selfDelta)
        {
            if (selfDelta == 0) return;

            var self = SelfRank;
            if (self == null)
            {
                Log.Warning("[RankComponent] ApplyFakeSettlementAmbientProgress failed: self not found.");
                return;
            }

            bool needExpand = m_CurrentCycle != null && !m_CurrentCycle.HasPlayed;
            if (needExpand)
            {
                m_CurrentCycle.HasPlayed = true;
                SaveCycle();
                BuildRanks(FullFakeCount, FullMaxScore, SelfPlayerName, preservedSelfScore: self.Score);
                return;
            }

            int oldSelfRank = self.Rank;
            ApplyFakeSettlementScoreProgress(selfDelta, oldSelfRank);
            Resort();
            SaveRanks();
        }

        /// <summary>更新本地玩家积分并重新排序；首次调用时会把榜单扩展到完整 50 条并把玩家名字由占位改为正式名字。</summary>
        public void UpdateSelfScore(int score)
        {
            var self = SelfRank;
            if (self == null)
            {
                Log.Warning("[RankComponent] UpdateSelfScore failed: self not found.");
                return;
            }

            bool needExpand = m_CurrentCycle != null && !m_CurrentCycle.HasPlayed;
            if (needExpand)
            {
                m_CurrentCycle.HasPlayed = true;
                SaveCycle();
                BuildRanks(FullFakeCount, FullMaxScore, SelfPlayerName, preservedSelfScore: score);
                return;
            }

            if (self.Score == score) return;

            int oldSelfRank = self.Rank;
            self.Score = score;
            Resort();
            NotifyChanged(oldSelfRank);
        }

        /// <summary>移除一条记录。</summary>
        public bool Remove(string playerId)
        {
            var target = GetByPlayerId(playerId);
            if (target == null) return false;

            int oldSelfRank = SelfRank?.Rank ?? 0;
            m_Ranks.Remove(target);
            Resort();
            NotifyChanged(oldSelfRank);
            return true;
        }

        /// <summary>按 Score 降序排序，同分时本地玩家排最前，并重写 Rank 字段（首次未通关时本地玩家 Rank 置 0，UI 显示为“-”）。</summary>
        private void Resort()
        {
            var originalIndices = new Dictionary<RankData, int>(m_Ranks.Count);
            for (int i = 0; i < m_Ranks.Count; i++)
                if (m_Ranks[i] != null) originalIndices[m_Ranks[i]] = i;

            m_Ranks.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;

                int scoreCompare = b.Score.CompareTo(a.Score);
                if (scoreCompare != 0) return scoreCompare;

                if (a.IsSelf != b.IsSelf)
                    return a.IsSelf ? -1 : 1;

                return originalIndices[a].CompareTo(originalIndices[b]);
            });
            bool hideSelfRank = m_CurrentCycle != null && !m_CurrentCycle.HasPlayed;
            for (int i = 0; i < m_Ranks.Count; i++)
            {
                var r = m_Ranks[i];
                r.Rank = (hideSelfRank && r.IsSelf) ? 0 : i + 1;
            }
        }

        private void NotifyChanged(int oldSelfRank)
        {
            SaveRanks();
            OnRanksChanged?.Invoke(m_Ranks);

            int newSelfRank = SelfRank?.Rank ?? 0;
            if (newSelfRank != oldSelfRank)
                OnSelfRankChanged?.Invoke(oldSelfRank, newSelfRank);
        }

        /// <summary>
        /// 每次 Update 先判断当前周期日期是否为“今日”；非当日（包括首次进入）则重新获取一次 RankCycleData。
        /// </summary>
        private void EnsureDailyCycle()
        {
            string todayKey = GetTodayKey();
            bool needNewCycle = m_CurrentCycle == null
                                || string.IsNullOrEmpty(m_CurrentCycle.DateKey)
                                || m_CurrentCycle.DateKey != todayKey;

            if (needNewCycle)
            {
                SavePendingRankRewardIfEligible();
                Clear();
                StartNewCycle(DailyDurationSeconds);
                GenerateDailyRanks();
                return;
            }

            if (m_Ranks.Count == 0)
                GenerateDailyRanks();
        }

        /// <summary>
        /// 生成当日排行榜数据：根据当前周期是否已通关，选择首次（少量虚假玩家、玩家占位名）或完整（50 条、玩家正常显示）模式。
        /// </summary>
        private void GenerateDailyRanks()
        {
            bool hasPlayed = m_CurrentCycle != null && m_CurrentCycle.HasPlayed;
            if (hasPlayed)
                BuildRanks(FullFakeCount, FullMaxScore, SelfPlayerName, preservedSelfScore: SelfRank?.Score ?? 0);
            else
                BuildRanks(Random.Range(20, InitialFakeCount), Random.Range(50, InitialMaxScore), SelfPlayerName, preservedSelfScore: 0);
        }

        /// <summary>根据参数构造排行榜数据并写入（虚假玩家分数降序，最低为 FakeMinScore）。</summary>
        private void BuildRanks(int fakeCount, int fakeMaxScore, string selfName, int preservedSelfScore)
        {
            var ranks = new List<RankData>(fakeCount + 1);

            var customConfig = GameEntry.CustomConfig;
            var avatarDb = customConfig != null ? customConfig.AvatarConfig : null;
            var allAvatars = avatarDb != null ? avatarDb.GetAllAvatars() : null;
            var allFrames = avatarDb != null ? avatarDb.GetAllFrames() : null;

            int[] scores = GenerateDescendingEvenScores(fakeCount, fakeMaxScore, FakeMinScore);
            List<string> fakeNames = PickFakeNames(fakeCount);
            for (int i = 0; i < fakeCount; i++)
            {
                string playerName = i < fakeNames.Count ? fakeNames[i] : $"Bot{i:000}";
                string avatarId = DefaultAvatarId;
                string frameId = null;
                if (allAvatars != null && allAvatars.Count > 0)
                    avatarId = allAvatars[Random.Range(0, allAvatars.Count)].id.ToString();
                if (allFrames != null && allFrames.Count > 0)
                    frameId = allFrames[Random.Range(0, allFrames.Count)].id.ToString();
                var fake = new RankData($"fake_{i}", playerName, scores[i], false)
                {
                    AvatarId = avatarId,
                    AvatarFrameId = frameId,
                };
                ranks.Add(fake);
            }

            var saveData = GameEntry.SaveData;
            string resolvedName = saveData != null && !string.IsNullOrEmpty(saveData.PlayerName)
                ? saveData.PlayerName : selfName;
            var self = new RankData(SelfPlayerId, resolvedName, preservedSelfScore, true)
            {
                AvatarId = saveData != null ? saveData.AvatarId.ToString() : DefaultAvatarId,
                AvatarFrameId = saveData?.AvatarFrameId.ToString(),
            };
            ranks.Add(self);

            SetRanks(ranks);
        }

        /// <summary>
        /// 让靠近玩家名次的部分假玩家获得少量涨分，并保持假玩家原有先后顺序稳定。
        /// 整体追赶强度按"玩家当前名次"动态调整：
        ///  - 玩家排名靠前（如 1-3）：近邻假玩家几乎与玩家同步涨分，避免高连胜倍数下长期霸榜；
        ///  - 玩家排名靠后：假玩家涨幅大幅衰减，让玩家能尽快跳出低位。
        /// </summary>
        private void ApplyFakeSettlementScoreProgress(int selfDelta, int selfRank)
        {
            if (selfDelta <= 0 || m_Ranks.Count <= 1) return;

            int total = m_Ranks.Count;
            int resolvedSelfRank = selfRank > 0 ? selfRank : total;
            float rankRatio = total > 1 ? (float)(resolvedSelfRank - 1) / (total - 1) : 0f;

            // 近邻 / 远端 在"顶部场景"和"底部场景"下的相对涨幅系数（相对 selfDelta）。
            float nearScale = Mathf.Lerp(m_FakeSettlementTopNearScale, m_FakeSettlementBottomNearScale, rankRatio);
            float farScale = Mathf.Lerp(m_FakeSettlementTopFarScale, m_FakeSettlementBottomFarScale, rankRatio);
            int nearbyRankSpan = Mathf.Max(1, m_FakeSettlementNearbyRankSpan);
            int extraRandomSlots = Mathf.Max(0, m_FakeSettlementMaxExtraDelta / 2) + 1;

            int previousFakeNewScore = int.MaxValue;
            int previousFakeOldScore = int.MaxValue;

            for (int i = 0; i < m_Ranks.Count; i++)
            {
                RankData rank = m_Ranks[i];
                if (rank == null || rank.IsSelf) continue;

                int oldScore = rank.Score;
                int currentRank = i + 1;
                int distance = Mathf.Abs(currentRank - resolvedSelfRank);
                float proximity = 1f - Mathf.Clamp01((float)distance / nearbyRankSpan);
                float scale = Mathf.Lerp(farScale, nearScale, proximity);

                int baseDelta = RoundDownToEven(Mathf.RoundToInt(selfDelta * scale));
                // 随机扰动概率随 scale 衰减：低位玩家场景下假玩家大概率不动，高位场景下高频抖动维持追击感
                int extraDelta = Random.value < Mathf.Clamp01(scale) ? Random.Range(0, extraRandomSlots) * 2 : 0;
                int newScore = oldScore + baseDelta + extraDelta;

                if (previousFakeNewScore != int.MaxValue)
                {
                    int originalGap = previousFakeOldScore - oldScore;
                    int maxAllowedScore = originalGap >= 4 ? previousFakeNewScore - 2 : previousFakeNewScore;

                    newScore = Mathf.Min(newScore, maxAllowedScore);
                }

                rank.Score = Mathf.Max(oldScore, newScore);
                previousFakeNewScore = rank.Score;
                previousFakeOldScore = oldScore;
            }
        }

        /// <summary>
        /// 根据玩家离线时长，让一部分假玩家分数自然增长，模拟其他玩家在你离线期间继续游戏。
        /// 默认每位被选中的假玩家每小时增长 4 分（按比例线性折算到不足 1 小时的时段，分数对齐为偶数）。
        /// </summary>
        /// <param name="elapsedSeconds">距上次退出的秒数。</param>
        /// <param name="affectedCount">本次受影响的假玩家上限（默认 20）。</param>
        /// <param name="pointsPerHour">每小时增长的分数（默认 4）。</param>
        public void ApplyFakePlayersOfflineProgress(long elapsedSeconds, int affectedCount = 20, int pointsPerHour = 4)
        {
            if (elapsedSeconds <= 0 || pointsPerHour <= 0 || affectedCount <= 0) return;
            if (m_Ranks.Count <= 1) return;

            int rawDelta = (int)(elapsedSeconds * pointsPerHour / 3600L);
            int delta = rawDelta & ~1;
            if (delta <= 0) return;

            var fakeIndices = new List<int>(m_Ranks.Count);
            for (int i = 0; i < m_Ranks.Count; i++)
            {
                RankData r = m_Ranks[i];
                if (r != null && !r.IsSelf) fakeIndices.Add(i);
            }
            if (fakeIndices.Count == 0) return;

            for (int i = 0; i < fakeIndices.Count; i++)
            {
                int j = Random.Range(i, fakeIndices.Count);
                (fakeIndices[i], fakeIndices[j]) = (fakeIndices[j], fakeIndices[i]);
            }

            int take = Mathf.Min(affectedCount, fakeIndices.Count);
            for (int i = 0; i < take; i++)
            {
                m_Ranks[fakeIndices[i]].Score += delta;
            }

            int oldSelfRank = SelfRank?.Rank ?? 0;
            Resort();
            NotifyChanged(oldSelfRank);
        }

        /// <summary>头像数据库加载完成后调用：为所有尚未分配随机头像的假玩家补充头像和头像框。</summary>
        public void RefreshFakePlayerAvatars()
        {
            var db = GameEntry.CustomConfig != null ? GameEntry.CustomConfig.AvatarConfig : null;
            if (db == null) return;
            var allAvatars = db.GetAllAvatars();
            var allFrames = db.GetAllFrames();
            if ((allAvatars == null || allAvatars.Count == 0) && (allFrames == null || allFrames.Count == 0))
                return;

            bool changed = false;
            foreach (var rank in m_Ranks)
            {
                if (rank == null || rank.IsSelf) continue;
                if (allAvatars != null && allAvatars.Count > 0)
                {
                    rank.AvatarId = allAvatars[Random.Range(0, allAvatars.Count)].id.ToString();
                    changed = true;
                }
                if (allFrames != null && allFrames.Count > 0)
                {
                    rank.AvatarFrameId = allFrames[Random.Range(0, allFrames.Count)].id.ToString();
                    changed = true;
                }
            }

            if (changed)
            {
                SaveRanks();
                OnRanksChanged?.Invoke(m_Ranks);
            }
        }

        /// <summary>从 NameDataEN 数据表中随机抽取不重复的名字。</summary>
        private static List<string> PickFakeNames(int count)
        {
            var result = new List<string>(count);
            if (count <= 0) return result;

            IDataTable<DRNameDataEN> dt = GameEntry.DataTable == null
                ? null
                : GameEntry.DataTable.GetDataTable<DRNameDataEN>();
            if (dt == null || dt.Count == 0)
            {
                Log.Warning("[RankComponent] PickFakeNames: NameDataEN data table is empty or not loaded.");
                return result;
            }

            var pool = new List<string>(dt.Count);
            foreach (var row in dt)
            {
                if (row != null && !string.IsNullOrEmpty(row.Name))
                    pool.Add(row.Name);
            }

            for (int i = 0; i < pool.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, pool.Count);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int take = Mathf.Min(count, pool.Count);
            for (int i = 0; i < take; i++)
                result.Add(pool[i]);
            return result;
        }

        /// <summary>按曲线递减生成 count 个偶数分；分数空间足够时每个假玩家尽量不同分。</summary>
        private static int[] GenerateDescendingEvenScores(int count, int maxScore, int minScore)
        {
            if (count <= 0) return Array.Empty<int>();

            int max = maxScore - (maxScore & 1);
            int min = minScore - (minScore & 1);
            if (min < 0) min = 0;
            if (max < min) max = min;

            int[] result = new int[count];
            if (count == 1)
            {
                result[0] = max;
                return result;
            }

            result[0] = max;
            for (int i = 1; i < count; i++)
            {
                float t = (float)i / (count - 1);
                int score = Mathf.RoundToInt(Mathf.Lerp(max, min, Mathf.Pow(t, 0.92f)));
                if ((score & 1) == 1) score--;
                score = Mathf.Clamp(score, min, max);
                if (result[i - 1] - min >= 2)
                    score = Mathf.Min(score, result[i - 1] - 2);
                result[i] = Mathf.Max(min, score);
            }

            return result;
        }

        private static int RoundDownToEven(int value)
        {
            if (value <= 0) return 0;
            return value & ~1;
        }

        /// <summary>尝试从持久化存储加载已保存的排行数据（只当日期匹配当前周期时返回 true）。</summary>
        private bool TryLoadRanks()
        {
            var saved = GameEntry.SaveData.GetData<RankListSaveData>(RanksSaveKey, null);
            if (saved == null || saved.Ranks == null || saved.Ranks.Count == 0)
                return false;

            m_Ranks.Clear();
            m_Ranks.AddRange(saved.Ranks);
            Resort();
            return true;
        }

        /// <summary>把当前排行数据写入持久化存储。</summary>
        private void SaveRanks()
        {
            var data = new RankListSaveData { Ranks = new List<RankData>(m_Ranks) };
            GameEntry.SaveData.SetData(RanksSaveKey, data);
        }

        /// <summary>若玩家当前排名在前 6 且尚无待领奖励，则将排名保存为待领状态。</summary>
        private void SavePendingRankRewardIfEligible()
        {
            int existing = GameEntry.SaveData.GetData(PendingRankRewardKey, 0);
            if (existing >= 1 && existing <= MaxRankRewardRank) return;

            var self = SelfRank;
            if (self == null || self.Rank < 1 || self.Rank > MaxRankRewardRank) return;

            GameEntry.SaveData.SetData(PendingRankRewardKey, self.Rank);
        }

        /// <summary>[测试用] 直接写入指定名次的待领奖励状态，绕过正常周期结束逻辑。</summary>
        public void DEV_ForceRankReward(int rank)
        {
            int clamped = Mathf.Clamp(rank, 1, MaxRankRewardRank);
            GameEntry.SaveData.SetData(PendingRankRewardKey, clamped);
            Log.Info("[RankComponent] DEV: ForceRankReward rank={0}", clamped);
        }

        /// <summary>
        /// 尝试弹出待领的排行榜奖励名次。返回 true 时 selfRank 为 1-MaxRankRewardRank；调用后立即清除待领状态。
        /// </summary>
        public bool TryPopPendingRankReward(out int selfRank)
        {
            selfRank = GameEntry.SaveData.GetData(PendingRankRewardKey, 0);
            if (selfRank < 1 || selfRank > MaxRankRewardRank)
            {
                selfRank = 0;
                return false;
            }

            GameEntry.SaveData.SetData(PendingRankRewardKey, 0);
            return true;
        }

        private void TickCycle()
        {
            if (m_CurrentCycle == null || m_CurrentCycle.EndUnixTimeSeconds <= 0)
            {
                if (m_LastRemainingSeconds != 0)
                    NotifyCountdownChanged(0);
                return;
            }

            int remainingSeconds = GetRemainingSeconds();
            if (remainingSeconds != m_LastRemainingSeconds)
                NotifyCountdownChanged(remainingSeconds);

            if (remainingSeconds > 0)
                return;

            if (m_LastHandledCycleEndUnixTimeSeconds == m_CurrentCycle.EndUnixTimeSeconds)
                return;

            m_LastHandledCycleEndUnixTimeSeconds = m_CurrentCycle.EndUnixTimeSeconds;
            RankCycleData expiredCycle = m_CurrentCycle.Clone();

            SavePendingRankRewardIfEligible();
            Clear();
            OnCycleExpired?.Invoke(expiredCycle);
        }

        private void NotifyCycleChanged()
        {
            OnCycleChanged?.Invoke(m_CurrentCycle?.Clone());
        }

        private void NotifyCountdownChanged(int remainingSeconds)
        {
            m_LastRemainingSeconds = remainingSeconds;
            OnCycleCountdownChanged?.Invoke(remainingSeconds);
        }

        private int GetRemainingSeconds()
        {
            if (m_CurrentCycle == null || m_CurrentCycle.EndUnixTimeSeconds <= 0)
                return 0;

            long diff = m_CurrentCycle.EndUnixTimeSeconds - GetNowUnixTimeSeconds();
            return diff > 0 ? (int)diff : 0;
        }

        private IReadOnlyList<int> GetTargetItemIds()
        {
            m_TargetItemIds.Clear();

            if (m_TargetConfig != null)
                m_TargetConfig.AppendValidIds(m_TargetItemIds);

            if (m_TargetItemIds.Count > 0)
                return m_TargetItemIds;

            if (m_DefaultTargetItemIds == null)
                return m_TargetItemIds;

            for (int i = 0; i < m_DefaultTargetItemIds.Length; i++)
            {
                int id = m_DefaultTargetItemIds[i];
                if (id <= 0) continue;
                if (m_TargetItemIds.Contains(id)) continue;

                m_TargetItemIds.Add(id);
            }

            return m_TargetItemIds;
        }

        private int PickRandomTargetItemId()
        {
            IReadOnlyList<int> targetIds = GetTargetItemIds();
            if (targetIds.Count <= 0)
            {
                Log.Warning("[RankComponent] PickRandomTargetItemId failed: no valid target item ids.");
                return 0;
            }

            return targetIds[UnityEngine.Random.Range(0, targetIds.Count)];
        }

        private int NormalizeTargetItemId(int inputId)
        {
            IReadOnlyList<int> targetIds = GetTargetItemIds();
            for (int i = 0; i < targetIds.Count; i++)
            {
                if (targetIds[i] == inputId)
                    return inputId;
            }

            if (targetIds.Count <= 0)
            {
                if (inputId > 0)
                {
                    Log.Warning("[RankComponent] NormalizeTargetItemId fallback to input id: {0}, because target id list is empty.", inputId);
                    return inputId;
                }

                Log.Warning("[RankComponent] NormalizeTargetItemId failed: no valid target id and target id list is empty.");
                return 0;
            }

            return PickRandomTargetItemId();
        }

        private static long GetNowUnixTimeSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private DateTime m_CachedToday;
        private string m_CachedTodayKey;
        /// <summary>获取本地当日日期键（yyyy-MM-dd）。</summary>
        private string GetTodayKey()
        {
            DateTime today = DateTime.Today;

            if (m_CachedTodayKey == null || today != m_CachedToday)
            {
                m_CachedToday = today;
                m_CachedTodayKey = today.ToString(DateKeyFormat);
            }

            return m_CachedTodayKey;
        }

        private static string FormatRemainingTime(int totalSeconds)
        {
            if (totalSeconds <= 0)
                return "00:00:00";

            TimeSpan span = TimeSpan.FromSeconds(totalSeconds);
            int totalHours = (int)span.TotalHours;
            return $"{totalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
        }
    }
}
