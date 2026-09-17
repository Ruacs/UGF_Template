using System;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Event;
using GameFramework.Setting;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    /// <summary>
    /// GameEntry 上的活动模块宿主。活动业务仍由 <see cref="ActivityModuleHost"/> 管理，
    /// 此组件仅负责场景生命周期和活动目录的装配入口。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActivityComponent : GameFrameworkComponent
    {
        [Tooltip("活动目录的 ScriptableObject 逻辑名；由 ProcedurePreload 通过 GF 资源流程加载。")]
        [SerializeField] private string m_CatalogAssetName = ActivityModuleCatalogConfig.AssetName;
        [Tooltip("活动奖励接收器类型；默认 LogActivityRewardReceiver 只记录日志和回执。")]
        [SerializeField] private string m_RewardReceiverTypeName = "Lokas.LogActivityRewardReceiver";

        /// <summary>当前活动模块的业务宿主；不会在 Awake 中提前构造具体活动。</summary>
        public ActivityModuleHost Host { get; private set; }

        /// <summary>活动目录的逻辑资源名。</summary>
        public string CatalogAssetName => m_CatalogAssetName;
        public string RewardReceiverTypeName => m_RewardReceiverTypeName;

        /// <summary>当前已经完整安装到宿主的目录。</summary>
        public ActivityModuleCatalogConfig InstalledCatalog { get; private set; }

        private bool m_IsGameEntryHost;

        // protected override void Awake()
        // {
        //     m_IsGameEntryHost = GetComponent<GameEntry>() != null;
        //     if (!m_IsGameEntryHost)
        //     {
        //         Debug.LogWarning("[Activity] ActivityComponent must be attached directly to the GameEntry object.", this);
        //         enabled = false;
        //         return;
        //     }

        //     base.Awake();
        // }

        /// <summary>在 GameEntry 的既有组件初始化时创建本地活动宿主。</summary>
        public void Initialize()
        {
            if (Host != null) return;

            // 当前模板仅有本地档案。账号切换须先 await 旧宿主 ShutdownAsync，再用新 ProfileId 装配。
            var storage = new GameFrameworkActivityStorageBackend(GameFrameworkEntry.GetModule<ISettingManager>());
            var facts = new GameFrameworkActivityFacts(GameFrameworkEntry.GetModule<IEventManager>());
            IActivityRewardReceiver receiver = CreateRewardReceiver();
            Host = new ActivityModuleHost("local", new LocalActivityServicesFactory(storage, facts,
                rewardReceiver: _ => receiver), Debug.LogException);
        }

        /// <summary>由预加载流程调用；公共层只读取定义，不引用任何具体活动类型。</summary>
        public async UniTask InstallCatalogAsync(ActivityModuleCatalogConfig catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (Host == null) throw new InvalidOperationException("Activity component has not been initialized.");

            ActivityPageRegistry.Install(catalog);
            foreach (ActivityModuleDefinition definition in catalog.Modules)
            {
                if (definition == null) continue;
                await Host.RegisterAsync(definition, definition.GameIds);
            }

            InstalledCatalog = catalog;
        }

        private IActivityRewardReceiver CreateRewardReceiver()
        {
            if (string.IsNullOrWhiteSpace(m_RewardReceiverTypeName)) return new LogActivityRewardReceiver();

            Type receiverType = GameFramework.Utility.Assembly.GetType(m_RewardReceiverTypeName);
            if (receiverType == null || receiverType.IsAbstract || !typeof(IActivityRewardReceiver).IsAssignableFrom(receiverType))
                throw new InvalidOperationException($"Activity reward receiver '{m_RewardReceiverTypeName}' must implement {nameof(IActivityRewardReceiver)}.");
            if (Activator.CreateInstance(receiverType) is IActivityRewardReceiver receiver) return receiver;
            throw new InvalidOperationException($"Activity reward receiver '{m_RewardReceiverTypeName}' could not be created.");
        }

        private void OnDestroy()
        {
            if (!m_IsGameEntryHost) return;

            ActivityModuleHost host = Host;
            Host = null;
            InstalledCatalog = null;
            ActivityPageRegistry.Clear();
            GameEntry.ClearActivityComponent(this);

            // Unity 销毁回调无法等待外部 I/O；正常账号切换或卸载应提前显式等待 ShutdownAsync。
            if (host != null) host.ShutdownAsync().Forget(Debug.LogException);
        }
    }
}
