using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    public interface IActivityEntryProvider
    {
        IReadOnlyList<ActivityEntryInfo> GetEntries();
        event Action EntriesChanged;
        UniTask OpenEntryAsync(string entryId, CancellationToken cancellationToken);
    }

    public sealed class ActivityEntryInfo
    {
        public string EntryId { get; }
        public string InstanceId { get; }
        public string TitleKey { get; }
        public string IconKey { get; }
        public bool Visible { get; }
        public bool Interactable { get; }
        public string UnavailableReasonKey { get; }
        public int BadgeCount { get; }
        public int Order { get; }
        public DateTimeOffset? CountdownEndUtc { get; }

        public ActivityEntryInfo(string entryId, string titleKey, string iconKey = null, string instanceId = null,
            bool visible = true, bool interactable = true, string unavailableReasonKey = null,
            int badgeCount = 0, int order = 0, DateTimeOffset? countdownEndUtc = null)
        {
            EntryId = ActivityContract.RequireId(entryId, nameof(entryId));
            TitleKey = ActivityContract.RequireId(titleKey, nameof(titleKey));
            IconKey = iconKey;
            InstanceId = instanceId;
            Visible = visible;
            Interactable = interactable;
            UnavailableReasonKey = unavailableReasonKey;
            if (badgeCount < 0) throw new ArgumentOutOfRangeException(nameof(badgeCount));
            BadgeCount = badgeCount;
            Order = order;
            CountdownEndUtc = countdownEndUtc?.ToUniversalTime();
        }
    }

    /// <summary>首页保存此摘要，不保存具体模块或其服务。点击必须携带 Generation。</summary>
    public sealed class ActivityEntrySnapshot
    {
        public string ModuleId { get; }
        public long Generation { get; }
        public ActivityEntryInfo Entry { get; }

        public ActivityEntrySnapshot(string moduleId, long generation, ActivityEntryInfo entry)
        {
            ModuleId = moduleId;
            Generation = generation;
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }
    }

    // 仅为模块装载状态，不表示活动报名、结算或领奖状态。
    public enum ActivityModuleState { Initializing, Ready, Stopping, Failed }

    public sealed class ActivityModuleSnapshot
    {
        public string ModuleId { get; }
        public long Generation { get; }
        public ActivityModuleState State { get; }
        public Exception LastError { get; }

        public ActivityModuleSnapshot(string moduleId, long generation, ActivityModuleState state, Exception lastError)
        {
            ModuleId = moduleId;
            Generation = generation;
            State = state;
            LastError = lastError;
        }
    }
}
