using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    /// <summary>发奖目标和单位必须显式映射；接收方负责入账与回执的可靠性。</summary>
    public interface IActivityRewardGateway
    {
        bool CanGrant(string resourceKey, string target);
        UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request, CancellationToken cancellationToken = default);
        UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId, CancellationToken cancellationToken = default);
    }

    public enum ActivityRewardStatus { Granted, AlreadyGranted, Pending, Unsupported, Failed, NotFound, Conflict }

    public sealed class ActivityRewardItem
    {
        public string ResourceKey { get; }
        public string Unit { get; }
        public long Amount { get; }

        public ActivityRewardItem(string resourceKey, string unit, long amount)
        {
            ResourceKey = ActivityContract.RequireId(resourceKey, nameof(resourceKey));
            Unit = ActivityContract.RequireId(unit, nameof(unit));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Amount = amount;
        }
    }

    public sealed class ActivityRewardRequest
    {
        public string GrantId { get; }
        public string InstanceId { get; }
        public string Target { get; }
        public IReadOnlyList<ActivityRewardItem> Items { get; }

        public ActivityRewardRequest(string grantId, string instanceId, string target, IEnumerable<ActivityRewardItem> items)
        {
            GrantId = ActivityContract.RequireId(grantId, nameof(grantId));
            InstanceId = instanceId;
            Target = ActivityContract.RequireId(target, nameof(target));
            if (items == null) throw new ArgumentNullException(nameof(items));
            var copy = new List<ActivityRewardItem>(items);
            if (copy.Count == 0 || copy.Exists(item => item == null))
                throw new ArgumentException("At least one non-null reward item is required.", nameof(items));
            Items = copy.AsReadOnly();
        }
    }

    public sealed class ActivityRewardReceipt
    {
        public string GrantId { get; }
        public ActivityRewardStatus Status { get; }
        public string ReceiptId { get; }
        public string Reason { get; }

        public ActivityRewardReceipt(string grantId, ActivityRewardStatus status, string receiptId = null, string reason = null)
        {
            GrantId = ActivityContract.RequireId(grantId, nameof(grantId));
            Status = status;
            ReceiptId = receiptId;
            Reason = reason;
        }
    }
}
