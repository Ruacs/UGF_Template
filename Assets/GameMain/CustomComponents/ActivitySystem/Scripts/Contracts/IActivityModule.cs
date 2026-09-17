using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    /// <summary>活动只共享接入契约，业务规则、状态和服务由各模块自行实现。</summary>
    public interface IActivityModule
    {
        string ModuleId { get; }
        UniTask InitializeAsync(IActivityContext context, CancellationToken cancellationToken);
        // 必须能清理部分初始化，且允许在上一次清理失败后重试。
        UniTask ShutdownAsync();
    }

    public interface IActivityModuleFactory
    {
        // 只构造对象；订阅和资源加载应在 InitializeAsync 中开始。
        IActivityModule CreateModule();
    }

    public interface IActivityContext
    {
        string ModuleId { get; }
        string ProfileId { get; }
        long Generation { get; }
        CancellationToken LifetimeToken { get; }
        IActivityClock Clock { get; }
        IActivityStorage Storage { get; }
        IActivityGameFacts GameFacts { get; }
        IActivityRewardGateway Rewards { get; }
        IActivityUI UI { get; }
    }

    /// <summary>标识一次模块装配；游戏来源必须显式声明，空列表表示不接收游戏事实。</summary>
    public sealed class ActivityScope
    {
        public string ProfileId { get; }
        public string ModuleId { get; }
        public long Generation { get; }
        public IReadOnlyList<string> GameIds { get; }

        public ActivityScope(string profileId, string moduleId, long generation, IEnumerable<string> gameIds)
        {
            ProfileId = ActivityContract.RequireId(profileId, nameof(profileId));
            ModuleId = ActivityContract.RequireId(moduleId, nameof(moduleId));
            if (generation <= 0) throw new ArgumentOutOfRangeException(nameof(generation));
            Generation = generation;
            var ids = new SortedSet<string>(StringComparer.Ordinal);
            if (gameIds != null)
                foreach (string id in gameIds) ids.Add(ActivityContract.RequireId(id, nameof(gameIds)));
            GameIds = new List<string>(ids).AsReadOnly();
        }
    }

    /// <summary>目标项目装配自己的宿主适配；每次调用必须提供本次作用域专属的 UI 适配器。</summary>
    public interface IActivityServicesFactory
    {
        ActivityServices CreateServices(ActivityScope scope);
    }

    public sealed class ActivityServices
    {
        public IActivityClock Clock { get; }
        public IActivityStorage Storage { get; }
        public IActivityGameFacts GameFacts { get; }
        public IActivityRewardGateway Rewards { get; }
        public IActivityUI UI { get; }

        public ActivityServices(IActivityClock clock, IActivityStorage storage, IActivityGameFacts gameFacts,
            IActivityRewardGateway rewards, IActivityUI ui)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            GameFacts = gameFacts ?? throw new ArgumentNullException(nameof(gameFacts));
            Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
        }
    }

    internal static class ActivityContract
    {
        public static string RequireId(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("An explicit identifier is required.", name);
            return value;
        }
    }
}
