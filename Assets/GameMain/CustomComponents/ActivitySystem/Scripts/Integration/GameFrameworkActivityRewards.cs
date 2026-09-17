using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 活动奖励的本地接收点。子游戏接入时实现这里即可，不需要改活动的领取、回执或 UI 流程。
    /// </summary>
    public interface IActivityRewardReceiver
    {
        bool CanReceive(string resourceKey, string target);
        bool TryReceive(ActivityRewardItem item, string target, out string reason);
    }

    /// <summary>模板默认接收器：只记录领取，不直接改任意子游戏数据。</summary>
    public sealed class LogActivityRewardReceiver : IActivityRewardReceiver
    {
        public bool CanReceive(string resourceKey, string target) =>
            !string.IsNullOrWhiteSpace(resourceKey) && string.Equals(target, "profile", StringComparison.Ordinal);

        public bool TryReceive(ActivityRewardItem item, string target, out string reason)
        {
            Debug.Log($"[ActivityReward] Granted '{item.ResourceKey}' x{item.Amount} ({item.Unit}) to {target}. Configure an IActivityRewardReceiver to update game data.");
            reason = null;
            return true;
        }
    }

    /// <summary>本地客户端奖励接收器。服务端接入时以服务端回执适配器替换它。</summary>
    public sealed class GameFrameworkActivityRewardGateway : IActivityRewardGateway
    {
        [Serializable]
        private sealed class ReceiptRecord
        {
            public string grantId;
            public string fingerprint;
            public int status;
            public string receiptId;
            public string reason;
        }

        private const string ReceiptKeyPrefix = "receipt/";
        private readonly IActivityStorage m_Storage;
        private readonly IActivityRewardReceiver m_Receiver;
        private readonly HashSet<string> m_InFlight = new HashSet<string>(StringComparer.Ordinal);

        public GameFrameworkActivityRewardGateway(IActivityStorage storage, IActivityRewardReceiver receiver = null)
        {
            m_Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            m_Receiver = receiver ?? new LogActivityRewardReceiver();
        }

        public bool CanGrant(string resourceKey, string target)
        {
            return m_Receiver.CanReceive(resourceKey, target);
        }

        public async UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request == null) throw new ArgumentNullException(nameof(request));
            string fingerprint = BuildFingerprint(request);
            if (m_InFlight.Contains(request.GrantId))
                return new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Pending, reason: "The reward grant is already in progress.");

            ActivityStorageReadResult read = await m_Storage.ReadAsync(ReceiptKeyPrefix + request.GrantId, cancellationToken);
            if (read.Status == ActivityStorageReadStatus.Corrupt)
                return new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Failed, reason: "The local reward receipt is corrupt.");
            if (read.Status == ActivityStorageReadStatus.Found)
            {
                ReceiptRecord record = JsonUtility.FromJson<ReceiptRecord>(read.Payload);
                if (record == null || !string.Equals(record.grantId, request.GrantId, StringComparison.Ordinal))
                    return new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Failed, reason: "The local reward receipt is invalid.");
                if (!string.Equals(record.fingerprint, fingerprint, StringComparison.Ordinal))
                    return new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Conflict, reason: "The grant ID was reused with different rewards.");
                ActivityRewardStatus previous = (ActivityRewardStatus)record.status;
                if (previous == ActivityRewardStatus.Granted)
                    return new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.AlreadyGranted, record.receiptId);
                if (previous == ActivityRewardStatus.Pending)
                    return new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Pending, record.receiptId, record.reason);
            }

            foreach (ActivityRewardItem item in request.Items)
            {
                if (!CanGrant(item.ResourceKey, request.Target))
                    return new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Unsupported,
                        reason: $"No activity reward receiver is available for '{item.ResourceKey}' ({item.Unit}).");
            }

            m_InFlight.Add(request.GrantId);
            string receiptId = "local:" + request.GrantId;
            try
            {
                await WriteReceiptAsync(new ReceiptRecord
                {
                    grantId = request.GrantId,
                    fingerprint = fingerprint,
                    status = (int)ActivityRewardStatus.Pending,
                    receiptId = receiptId,
                    reason = "Local grant checkpointed before delivery."
                }, cancellationToken);

                foreach (ActivityRewardItem item in request.Items)
                {
                    if (!m_Receiver.TryReceive(item, request.Target, out string reason))
                    {
                        await WriteReceiptAsync(new ReceiptRecord
                        {
                            grantId = request.GrantId,
                            fingerprint = fingerprint,
                            status = (int)ActivityRewardStatus.Failed,
                            receiptId = receiptId,
                            reason = reason
                        }, cancellationToken);
                        return new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Failed, receiptId, reason);
                    }
                }

                await WriteReceiptAsync(new ReceiptRecord
                {
                    grantId = request.GrantId,
                    fingerprint = fingerprint,
                    status = (int)ActivityRewardStatus.Granted,
                    receiptId = receiptId
                }, cancellationToken);
                return new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Granted, receiptId);
            }
            finally
            {
                m_InFlight.Remove(request.GrantId);
            }
        }

        public async UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ActivityContract.RequireId(grantId, nameof(grantId));
            ActivityStorageReadResult read = await m_Storage.ReadAsync(ReceiptKeyPrefix + grantId, cancellationToken);
            if (read.Status == ActivityStorageReadStatus.Missing)
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.NotFound);
            if (read.Status == ActivityStorageReadStatus.Corrupt)
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Failed, reason: "The local reward receipt is corrupt.");
            ReceiptRecord record = JsonUtility.FromJson<ReceiptRecord>(read.Payload);
            if (record == null || !string.Equals(record.grantId, grantId, StringComparison.Ordinal))
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Failed, reason: "The local reward receipt is invalid.");
            return new ActivityRewardReceipt(grantId, (ActivityRewardStatus)record.status, record.receiptId, record.reason);
        }

        private async UniTask WriteReceiptAsync(ReceiptRecord record, CancellationToken cancellationToken)
        {
            await m_Storage.WriteAsync(ReceiptKeyPrefix + record.grantId, JsonUtility.ToJson(record), 1, cancellationToken);
            await m_Storage.FlushAsync(cancellationToken);
        }

        private static string BuildFingerprint(ActivityRewardRequest request)
        {
            var parts = new List<string> { request.InstanceId ?? string.Empty, request.Target };
            foreach (ActivityRewardItem item in request.Items)
                parts.Add(item.ResourceKey + ":" + item.Unit + ":" + item.Amount.ToString(CultureInfo.InvariantCulture));
            return string.Join("|", parts);
        }

    }
}
