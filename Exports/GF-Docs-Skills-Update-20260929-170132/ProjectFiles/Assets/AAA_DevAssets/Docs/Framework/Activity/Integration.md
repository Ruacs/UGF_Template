# 活动模块接入：当前实现

更新：2026-09-15。

## 1. 已实现的边界

“通用”指各活动通过同一组宿主接口接入新游戏。每个活动自行拥有规则、数值配置、实例状态、服务接口、存档格式和多个页面。

本次已完成公共接入层，并以通行证验证了首个具体活动：

| 能力 | 当前状态 |
|---|---|
| 显式模块工厂、初始化、停用、重复注册检查 | 已实现，`ActivityModuleHost` |
| 隔离上下文、取消、代次、清理失败后重试 | 已实现 |
| 首页入口的全量摘要、刷新通知、点击分发 | 已实现；`ActivityEntryLauncher` 提供当前样例的通用 `PASS` 入口 |
| 本地时间、模块存储、GF 事实订阅 | 已实现适配 |
| 发奖请求、目标、单位、回执接口 | 已实现 GF 本地适配：金币、Hint、Shuffle、AddTime 可发放；未知资源明确返回 `Unsupported` |
| 多页面请求、窗口实例句柄、关闭接口 | 已实现 `GameFrameworkActivityUI`，按 Catalog 的模块页面清单路由到 GF UIForm |
| 活动专用 UI 资源域、页面清单与表生成 | 已实现 `ActivityPageRegistry` 与 UIForm 同步；活动 Prefab 保持模块内目录 |
| 模块装配 | 已实现 SO Catalog，在 `ProcedurePreload` 的预加载阶段显式安装 |
| 模块移除、导入、重装工具 | 未实现；安装工具目前只维护配置和注册、校验设计师 Prefab |
| SeasonPass | 已实现数值、状态、领取、事实接入、三页路由；Prefab 视觉由设计师维护 |

本地奖励适配不是服务端经济事务；生产环境仍需由每个活动自己的服务端接口完成订单、回执和可靠入账。

## 2. 代码与资源归属

```text
Assets/GameMain/CustomComponents/ActivitySystem/Scripts/
  Contracts/                 公共接口、不可变 DTO
  Runtime/                   模块宿主、作用域生命周期
  Integration/               GF 事件、Setting、本地时钟适配

Assets/GameMain/Activities/{ActivityName}/    后续具体模块的归属
  Scripts/
    {ActivityName}Module.cs
    Runtime/ Data/ Config/ UI/
  UI/                        本模块所有页面 Prefab 和私有 Widget
  ScriptableObjects/         本模块配置、资源清单
  Texture/ Material/
  README.md
```

例如 Race 的主页面、规则页、结算页均归属于 `Activities/Race/`。跨活动真正通用的 UI 才进入公共 UI 目录。活动不依附某一个子游戏目录；目标游戏事实的适配脚本仍属于 `SubGame/{GameName}/Scripts/`。

`ActivityModuleCatalogConfig` 在预加载时安装 `ActivityPageRegistry`。`AssetUtility.GetUIFormAsset()` 先查询该注册表，因此活动页面必须在模块自己的定义资产中同时声明页面键、UIForm ID、资源名和 Prefab 路径。音频、多语言和公共字体继续遵守全局资源约定。

## 3. 宿主装配与生命周期

`GameLauncher` 场景的 `GameEntry` 对象挂载 `ActivityComponent : GameFrameworkComponent`。`GameEntry.InitCustomComponents()` 获取该组件并调用 `Initialize()`，组件持有纯 C# 的 `ActivityModuleHost`；`GameEntry.Activities` 继续返回这个宿主，供已有入口和业务调用。Inspector 只读显示目录中配置的活动模块以及 Play Mode 下的装载状态，不把每个具体活动做成场景组件、全局单例或程序集扫描目标。首版本地档案 ID 为 `local`；场景切换不会重建活动宿主。

宿主创建时不扫描或构造具体活动。`ActivityComponent` 保存目录的逻辑资源名，当前样例在 `ProcedurePreload` 完成子游戏资源清单后按该名称加载 `ActivityModuleCatalogConfig`，安装页面注册表并逐项注册其中定义的模块。组件不直接序列化目录资产，以保持现有 GF 资源加载链。手动装配仍可在所需存档、配置和 GF UI 表就绪后显式注册：

```csharp
// MyActivityFactory 是具体模块自己的工厂；此处是未来模块的接入示例。
await GameEntry.Activities.RegisterAsync(
    new MyActivityFactory(myActivityConfig, myActivityService),
    new[] { "MyGame" },
    cancellationToken);
```

公共宿主不引用 `MyActivityFactory`。调用位于目标产品的装配层或该活动自己的 SO Catalog 定义中；不要将示例类型加入公共 `GameEntry`。

约束：

- `ModuleId` 发布后保持不变，按大小写敏感的完整字符串比较。每个宿主只允许同 ID 的一个运行模块，多个活动实例由该模块自行管理。
- 工厂只构造对象，初始化才开始订阅与加载。重复/并发注册同 ID 明确失败，不会重复初始化。
- 仅在初始化成功后发布入口。初始化失败或取消会清理已创建资源。
- 所有宿主、上下文与本地适配调用都在 Unity 主线程。后台任务先切回主线程；不要直接修改存档或页面。
- `GameIds` 必须显式传入。省略或空列表表示不接收游戏事实，不代表订阅所有游戏。
- `Generation` 在当前运行期间跨宿主递增。不要持久化这个值；页面和按钮应保存摘要中的代次，阻止旧账号/旧模块回调操作新模块。
- 清理方法必须可以处理部分初始化，并允许失败后重试。普通任务使用 `LifetimeToken`，自行取消并结束；不要在模块自身尚未返回的初始化/入口调用里等待宿主卸载自己。

停用调用：

```csharp
await GameEntry.Activities.UnregisterAsync(moduleId, generation);
// 切换账号、销毁产品上下文或关闭服务前：
await GameEntry.Activities.ShutdownAsync();
```

停用会先撤回入口、拒绝新入口/开页/发奖请求，取消普通任务并解绑事实，关闭所属 UI，等待初始化和入口操作结束，调用模块清理，等待已经进入宿主适配的操作，刷新存档，最后释放上下文。

清理/刷新失败时，`GetModules()` 保留 `Failed` 状态与 `LastError`，可重试 `UnregisterAsync` 或 `ShutdownAsync`。此时不能继续删除模块资产。宿主一旦开始整体关闭便拒绝新注册；重新装配应使用新宿主。

`ActivityComponent.OnDestroy` 提供最后的失效与清理兜底，但 Unity 销毁回调无法等待外部 I/O，进程强制终止也没有完成保证。正常账号切换/卸载必须先显式等待；当前没有自动账号切换流程。

## 4. 模块存储

`context.Storage` 将模块内 key 编码到 `Activity/v1/{ProfileId}/{ModuleId}/{Key}` 的独立命名空间，实际各段使用 Base64URL 编码避免分隔符碰撞。不要把 `Generation` 写进存储命名空间，否则重装会失去旧状态。

```csharp
ActivityStorageReadResult saved = await context.Storage.ReadAsync("state", token);
// 必须分别处理 Missing、Found、Corrupt；格式升级由活动自己实现。
await context.Storage.WriteAsync("state", serializedState, schemaVersion: 1);
```

- 存储封套保存自己的 format 版本、模块 schemaVersion 和字符串 payload。`Corrupt` 检测封套损坏；payload 的业务校验由模块负责。
- 后端 I/O 异常不会被当作 `Missing`；保存失败以异常返回。
- GF 适配直接检查 `ISettingManager.Save()` 的 bool，没有使用会丢弃失败结果的 `SaveDataStore.SetData` 或 `PlayerPrefsManager.Save`。
- 写入失败后内存中可能仍有脏数据，不能假定已回滚；应以相同状态重试保存或刷新。存储接口没有跨记录或跨经济系统事务保证。
- 模块负责串行修改自己的状态。清理期间仍可写恢复记录、查询回执；上下文释放后拒绝存储访问。
- 宿主不自动给已接受的持久化/发奖操作附加 `LifetimeToken`。模块要持有并等待这些操作；适配器应仅在开始提交前接受取消，提交后的不确定结果通过回执恢复。
- 停用不会删除任何模块存档。

## 5. 游戏事实

首个公共事实为不可变的 `ActivityLevelCompletedFact`。由目标游戏的结算适配代码发出：

```csharp
var fact = new ActivityLevelCompletedFact(
    savedFactId, "MyGame", sessionId, settlementSequence,
    occurredAtUtc, levelId, won);
GameEntry.Event.Fire(this, ActivityGameFactEventArgs.Create(fact));
```

活动订阅：

```csharp
context.GameFacts.Subscribe<ActivityLevelCompletedFact>(fact =>
{
    // 加入活动自己的处理队列；去重记录属于活动存档。
});
```

`FactId` 由源游戏提供，同一次结算重试保持原值。不要在适配器里临时生成 GUID，也不要用缺少来源/对局/顺序的旧事件拼出看似完整的事实。当前 `SubGameManagerComponent.GameWin()` 在调用 `GameOver()` 前发布这一事实，并继续发布既有 `LevelPassedEventArgs`；活动不从旧事件倒推事实 ID。

使用既有 GF Event；池化 EventArgs 只作为外壳，传给模块的是可独立保留的不可变事实。不提供事件历史重放，模块自己处理去重、断点与顺序。取消订阅后已排队的旧回调还会通过作用域检查被丢弃。

## 6. 首页与多页面 UI

首页只依赖 `GetEntries()`、`EntriesChanged` 和 `OpenEntryAsync(snapshot)`。订阅刷新通知后应立即读一次全量摘要，重新激活页面时也重新查询；点击时将原摘要交回宿主。

每个摘要包含模块 ID、代次以及活动自己的入口 ID、标题/图标 Key、显示/交互状态、红点、排序和倒计时。详情页使用活动自己的业务通知与参数。

模块通过 `context.UI.OpenAsync(new ActivityPageRequest("rules", instanceId, arguments), token)` 发起页面请求。

- `PageKey` 是模块内的键；页参数类型由活动拥有。
- `ActivityPageHandle` 使用 ModuleId + Generation + **GF SerialId**，不使用全局 UIFormId 作为窗口实例。
- 不同模块/代次的关闭请求会被忽略。取消后才完成的开页，会在返回取消前关闭它返回的旧页面句柄。
- 项目传入的 UI 适配器必须按 scope 独立创建，`CloseAllAsync()` 必须处理已打开以及仍在加载的页面；不能将一个跨模块关闭器直接共享给所有上下文。
- `GameFrameworkActivityUI` 将活动页面键解析为 Catalog 声明的 UIForm ID，并通过 `ActivityPageRegistry` 给 `AssetUtility` 提供模块内 Prefab 路径。它只关闭自己的 ModuleId + Generation + GF SerialId 页面实例。
- 活动页面按 `UIFormIdRanges` 分区，每个活动预留 20 个号：Season Pass 300–319、Race 320–339、Collector 340–359、Win Streak 360–379、Mining 380–399、Galaxy Challenge 400–419。现有通行证三页使用 300–302；后续页面只能在本活动区间内追加，不能占用示例或其他活动区间。变更页面名后通过安装入口同步 `UIForm.txt`、`UIForm.bytes` 和 `UIFormId.cs`。
- 通行证页面视觉由 `Activities/SeasonPass/UI/` 的设计师 Prefab 持有。绑定约定见 [通行证 UI Prefab 接口](../../../../GameMain/Activities/SeasonPass/Docs/UIPrefabContract.md)。

## 7. 各活动自己的业务服务与奖励

Race 可定义 `IRaceService`，SeasonPass 可定义 `ISeasonPassService`。模块工厂注入本地实现，未来换为相应远端实现；公共层不增加统一报名/领奖/排行榜/进度接口。

宿主奖励请求要求明确 `GrantId`、接收目标、资源 Key、单位和数量。项目可通过 `LocalActivityServicesFactory` 的奖励工厂绑定具体适配，每次收到 `ActivityScope`，按 ProfileId + ModuleId + GrantId 保存回执并检查同 ID 内容冲突。

`GameFrameworkActivityRewardGateway` 已映射 `currency.money`、`prop.hint`、`prop.shuffle`、`prop.add_time`，并在活动存储命名空间记录本地回执。其他资源 Key 返回 `Unsupported`。当前经济数据与活动保存仍分别提交，不具备崩溃下可靠的一次性入账；不能仅通过“先改 Money，再存 claimed”宣称幂等。服务端已经入账的奖励也不能再调用本地重复入账。

## 8. 验证与下一步

自动测试位于 `Assets/AAA_DevAssets/Editor/Tests/` 的 `ActivityModuleHostTests.cs`、`ActivityStorageTests.cs` 和 `ActivityGameFactsTests.cs`。使用 Unity Test Runner 的 EditMode，按 `Lokas.Editor.Tests.Activity` 过滤。测试模块只存在于编辑器测试文件，不被装入客户端或首页。

测试覆盖重复注册、初始化取消/失败、事实过滤、旧代次、入口刷新、清理失败重试、清理保存、已接受奖励等待、迟到开页关闭，以及账号/模块/键的存储隔离、损坏和保存失败。

### 2026-09-15 公共宿主基线验证

已验证（Unity `2022.3.62f3c1`，当前 GF_Template）：

- Unity 脚本编译完成，Console 未出现编译错误。
- EditMode 按 `Lokas.Editor.Tests.Activity` 过滤，18 项通过、0 失败、0 跳过。
- Play Mode 从 `GameLauncher` 启动到 `MainScene`，当前 Procedure 为 `ProcedureMenu`，`MainUIPanel` 已打开；该次基线验证的活动宿主为 `local` 档案，初始模块/入口数量均为 0。
- Play Mode 临时注册内存测试模块：入口数量为 1，真实 GF 事件到达一次；默认 UI 返回 `Unavailable`。停用后模块/入口数量恢复为 0，生命周期令牌已取消。测试模块没有写入活动业务存档或创建页面资产。
- 临时 Unity MCP 连接脚本已通过 Unity 资源 API 删除。测试结束退出 Play Mode，未保存场景或 Prefab 修改。

退出验证的已知问题：Unity 报告 `Some objects were not cleaned up when closing the scene`，对象为 `[DOTween]`。退出后已确认活动宿主引用释放、当前场景恢复为 `GameLauncher`。新增活动代码没有创建 GameObject 或 Tween；现有 `ProcedureMenu.OnLeave` 会调用默认带动画的 `UGuiForm.Close()`，这是需要另外核实的相关路径。本次未修改公共动画清理代码，也未将整场景退出标记为“无错误”。

### 2026-09-15 通行证实施验证

已验证：

- 通行证配置资产包含 HexaAway 分析中的 25 档数值，最大档需要 70 充能；Catalog 声明三页、三个模块内资源路径和 UIForm 300–302（活动区间 300–319）。
- `SeasonPassActivityTests` 3/3 通过；`ActivityModuleHostTests` 13/13、`ActivityStorageTests` 4/4、`ActivityGameFactsTests` 1/1 通过。
- Unity 编译完成，Console 无新增编译错误；`UIForm.txt`、`UIForm.bytes` 与 `UIFormId.cs` 同步。
- 在临时原型页面中验证了 `PASS` 首页入口和 GF 页面路由。该原型已按设计师接管 Prefab 的要求从项目删除。

未验证/未实现：设计师 Prefab 完成后的实际主页、规则页和领奖页视觉验收；未映射道具的真实入账；高级轨道购买与订单；服务端时间、回执和崩溃恢复；活动移除/导入工具；移动端构建。

`UIFormId` 的底层类型已改为 `int`，页面 ID 按活动区间规划；`UIForm.bytes` 仍是 GF DataTable 的运行时二进制产物，不能因为枚举改为 `int` 就删除。源表、bytes、ID 和资源注册需由工具同步。
