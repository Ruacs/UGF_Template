using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    public interface IActivityUI
    {
        UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request, CancellationToken cancellationToken = default);
        UniTask CloseAsync(ActivityPageHandle handle);
        // 必须处理本作用域已打开以及仍在加载的页面；返回时不再持有所属页面。
        UniTask CloseAllAsync();
    }

    public sealed class ActivityPageRequest
    {
        public string PageKey { get; }
        public string InstanceId { get; }
        // 参数类型属于活动模块；调用方负责保持不可变。
        public object Arguments { get; }

        public ActivityPageRequest(string pageKey, string instanceId = null, object arguments = null)
        {
            PageKey = ActivityContract.RequireId(pageKey, nameof(pageKey));
            InstanceId = instanceId;
            Arguments = arguments;
        }
    }

    public readonly struct ActivityPageHandle : IEquatable<ActivityPageHandle>
    {
        public string ModuleId { get; }
        public long Generation { get; }
        public int SerialId { get; }
        public bool IsValid => !string.IsNullOrEmpty(ModuleId) && Generation > 0 && SerialId > 0;

        public ActivityPageHandle(string moduleId, long generation, int serialId)
        {
            ModuleId = ActivityContract.RequireId(moduleId, nameof(moduleId));
            if (generation <= 0) throw new ArgumentOutOfRangeException(nameof(generation));
            if (serialId <= 0) throw new ArgumentOutOfRangeException(nameof(serialId));
            Generation = generation;
            SerialId = serialId;
        }

        public bool Equals(ActivityPageHandle other) => ModuleId == other.ModuleId && Generation == other.Generation && SerialId == other.SerialId;
        public override bool Equals(object obj) => obj is ActivityPageHandle other && Equals(other);
        public override int GetHashCode() => ((ModuleId?.GetHashCode() ?? 0) * 397 ^ Generation.GetHashCode()) * 397 ^ SerialId;
    }

    public enum ActivityUiOpenStatus { Opened, Reused, Unavailable, Failed }

    public sealed class ActivityUiOpenResult
    {
        public ActivityUiOpenStatus Status { get; }
        public ActivityPageHandle Handle { get; }
        public string Reason { get; }

        public ActivityUiOpenResult(ActivityUiOpenStatus status, ActivityPageHandle handle = default, string reason = null)
        {
            if ((status == ActivityUiOpenStatus.Opened || status == ActivityUiOpenStatus.Reused) && !handle.IsValid)
                throw new ArgumentException("An opened page requires an instance handle.", nameof(handle));
            Status = status;
            Handle = handle;
            Reason = reason;
        }
    }
}
