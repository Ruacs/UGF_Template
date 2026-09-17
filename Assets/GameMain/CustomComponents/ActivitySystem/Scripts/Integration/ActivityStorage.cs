using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework.Setting;
using UnityEngine;

namespace Lokas
{
    /// <summary>目标项目最小键值存储适配。null 表示缺失；读取/写入异常必须向上传递。</summary>
    public interface IActivityStorageBackend
    {
        string Read(string key);
        void Write(string key, string value);
        bool Flush();
    }

    public sealed class GameFrameworkActivityStorageBackend : IActivityStorageBackend
    {
        private readonly ISettingManager m_Settings;
        public GameFrameworkActivityStorageBackend(ISettingManager settings)
        {
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        public string Read(string key) => m_Settings.HasSetting(key) ? m_Settings.GetString(key) : null;
        public void Write(string key, string value) => m_Settings.SetString(key, value);
        // SettingComponent.Save / PlayerPrefsManager.Save 均丢弃 bool；此处直接检查原始结果。
        public bool Flush() => m_Settings.Save();
    }

    public sealed class ActivityStorage : IActivityStorage
    {
        [Serializable]
        private sealed class Envelope
        {
            public int format;
            public int schemaVersion;
            public string payload;
        }

        private readonly IActivityStorageBackend m_Backend;
        private readonly string m_Prefix;

        public ActivityStorage(IActivityStorageBackend backend, string profileId, string moduleId)
        {
            m_Backend = backend ?? throw new ArgumentNullException(nameof(backend));
            // 每段分别编码，避免包含分隔符的账号或模块名产生串档。
            m_Prefix = "Activity/v1/" + Encode(profileId, nameof(profileId)) + "/" + Encode(moduleId, nameof(moduleId)) + "/";
        }

        public UniTask<ActivityStorageReadResult> ReadAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string raw = m_Backend.Read(m_Prefix + Encode(key, nameof(key)));
            if (raw == null) return UniTask.FromResult(new ActivityStorageReadResult(ActivityStorageReadStatus.Missing));
            try
            {
                Envelope data = JsonUtility.FromJson<Envelope>(raw);
                if (data == null || data.format != 1 || data.schemaVersion <= 0 || data.payload == null)
                    return UniTask.FromResult(new ActivityStorageReadResult(ActivityStorageReadStatus.Corrupt, error: "Invalid activity storage envelope."));
                return UniTask.FromResult(new ActivityStorageReadResult(ActivityStorageReadStatus.Found, data.payload, data.schemaVersion));
            }
            catch (ArgumentException error)
            {
                return UniTask.FromResult(new ActivityStorageReadResult(ActivityStorageReadStatus.Corrupt, error: error.Message));
            }
        }

        public UniTask WriteAsync(string key, string payload, int schemaVersion, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            string storageKey = m_Prefix + Encode(key, nameof(key));
            string raw = JsonUtility.ToJson(new Envelope { format = 1, schemaVersion = schemaVersion, payload = payload });
            m_Backend.Write(storageKey, raw);
            // 开始同步提交后不再检查取消；返回成功仅表示此后端报告保存成功，并非跨系统事务。
            if (!m_Backend.Flush()) throw new IOException("Activity storage save failed. The in-memory value may still be dirty; retry the same write or flush.");
            return UniTask.CompletedTask;
        }

        public UniTask FlushAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!m_Backend.Flush()) throw new IOException("Activity storage flush failed.");
            return UniTask.CompletedTask;
        }

        private static string Encode(string value, string name)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(ActivityContract.RequireId(value, name)))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
