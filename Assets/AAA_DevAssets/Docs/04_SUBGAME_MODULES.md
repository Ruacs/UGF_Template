# 04 · SubGame Modules — 当前接入方式

本文说明已落地的三个扩展点：测试页面、服务器配置、资源清单。它们复用原有 GF 组件/Procedure，不新增全局 Manager；**不代表整个示例已可直接删目录**。

## 1. 先区分公共功能与玩法策略

| 内容 | 配置归属 | 读取进度 |
| --- | --- | --- |
| 关卡广告间隔、前 N 关免广告、Banner 门槛、广告道具数量 | 各游戏 `Scripts/Config/{GameName}ServerConfig.cs` | 显式指定的所属游戏 |
| `LevelTimerIdleStopSeconds` | `AdsServerConfig.Common` | 公共计时参数，当前计时游戏共用 |
| `TaskUnlockLevel`、`RankUnlockLevel`、`ShopUnlockLevel`、`FirstWinUnlockLevel` | `AdsServerConfig.Common` | 默认使用配置的主玩法，不随副玩法切换 |
| `ShowFirstWinClaim2Button`、`EnableShop` | `AdsServerConfig.Common` | 公共开关，副玩法不必重复声明 |
| 广告不可用面板开关、每日倒计时奖励额度 | `AdsServerConfig.Common` | 沿用全局奖励次数存档 |
| `RectMatchInfinitePropDisplayMaxLevel` | 仅 RectMatch 配置 | RectMatch |

设置主玩法：

1. 打开 `Assets/GameMain/Scenes/GameLauncher.unity`。
2. 选中 `GameEntry/GameFramework/GameManager`，在 `GameManagerComponent` Inspector 的“主玩法”分组设置模式。当前场景默认 **HexaAway**。
3. 也可打开 `Tools/Template/Sample SubGames`，点击“定位主玩法 / 已安装管理器”。
4. 新增组件未设置时为 None；主玩法为空或其配置未注册，公共关卡判断不会自动回退到其他示例。

当前首页的主按钮跟随主玩法；副按钮由 `MainUIPanel` Prefab 的 `Secondary Game Mode` 显式配置（当前 RectMatch）。未安装的入口、与主玩法相同的副入口会隐藏。它是两入口模板，不会自动生成任意数量的游戏列表；修改主玩法不会迁移存档。

### 原有行为与边界

- TaskComponent 原本仅检查存档存在，`TaskUnlockLevel` 只用于提示；本次未启用这条未接入的门槛。
- ShopComponent 原本使用 `AdsManager.ShopUnlockLevel = 1`，并未使用服务器字段的 10；本次只将进度来源改为可配置的主玩法，保留旧入口阈值。
- `EnableShop` 原 Load 未读取服务器 Key，本次保持 true 的缺省行为；ShopComponent 的 Inspector 开关仍保留。
- 公共排行/首胜判断已读取主玩法；RectMatch 的排行提示跟随公共排行是否已解锁，不再另配一份解锁字段。
- 广告间隔改为每游戏独立计数，使用该游戏的 `ShowAdsInterval`；原代码误用固定间隔 1 的问题已在迁移读取点时纠正。
- 公共存档（排行、首胜、商店等）没有因此改成各游戏独立存档。

## 2. 给子游戏添加测试页面

参考现有：

- `SubGame/HexaAway/Scripts/TestMode/HexaAwayTestModeModule.cs`：选关、皮肤、组合远程配置辅助模块。
- `SubGame/RectMatch/Scripts/TestMode/RectMatchTestModeModule.cs`：状态、资源就绪、关卡和引导诊断。

接入清单：

1. 文件放在自己的 `Scripts/TestMode/`，实现 `ITestModeModule` 的 OwnerId、ModuleName、PageName、Order、Build。
2. 通过构造函数传入所属游戏的数据和管理器，不让公共测试组件读取你的存档。
3. 有订阅/异步任务时实现 `ITestModeModuleLifecycle`。在 OnRegistered 保存上下文并绑定事件，在 OnUnregistered 解绑最初绑定的对象。
4. 自己的 Procedure 在资源初始化完成后调用 `GameEntry.TestMode.RegisterModule(module)`；OnLeave 调用 `UnregisterModule`，传入原实例。
5. Build 可以多次调用，只构建控件；不要在其中重复订阅事件或自动发起远程请求。
6. 异步操作传入 `context.CancellationToken`，每次 await 后重新检查 `context.IsActive` 及管理器；退出后不能建关或刷新旧页。

公共窗口保留 Common/Ads、Common/Runtime。注册键是 `(OwnerId, ModuleName)`；注销一个游戏不清空其他游戏。HexaAway 远程辅助模块已随游戏保存，原 GUID `cb917372cc948214ca30c15288f667bd` 未变化。

无测试 UI 时也可安全注册/注销模块；页面显示仍受原有测试模式启用规则控制。主菜单没有默认 Hexa 专页，进入游戏并初始化后才注册。

## 3. 给子游戏添加服务器配置

1. 在自己的 `Scripts/Config/` 实现 `ISubGameServerConfig`。接口只要求所属模式、通用广告策略、Load 和读取本游戏进度；**不要求上述 7 个公共字段**。
2. 字段、初始值、Load 缺省值及 Key 在自己的文件中声明，参考现有 `HexaAwayServerConfig` 或 `RectMatchServerConfig`。
3. 游戏管理器 Awake 中（先调用 base.Awake）注册自己的配置实例；OnDestroy 按同一实例注销。配置注册不等于资源已初始化，不应在注册时建关。
4. 业务代码读取自己的强类型配置，例如 `GameEntry.RectMatch.ServerConfig`；跨游戏公共代码使用 `AdsServerConfig.TryGet(gameMode, out config)`，不新增具体游戏静态属性。
5. 公共配置读取 `AdsServerConfig.Common`。主玩法进度读取 `AdsServerConfig.TryGetPrimaryLevel(out level)`；失败就不执行依赖进度的判断。

读取顺序：

- `ProcedurePreload` 保留 `Load()` 兜底。
- SDK 就绪调用 `ReloadFromServer()`，允许覆盖提前加载的缺省值。
- 后注册配置会从已可用来源完成 Load；重复注册同一个实例幂等，另一个同模式实例报冲突。
- 未安装/未知模式查询失败，不回退到 HexaAway。

### Key 兼容与默认值

各游戏显式使用 `ScopedServerConfigSource`：优先 `GameName.Key`，未提供时读取原 Key。因此可以只配置 `RectMatch.ShowBannerLevel` 而不影响 HexaAway；**无需服务端立刻迁移旧 Key**。公共字段只读取原公共 Key，不受 `RectMatch.RankUnlockLevel` 这类玩法前缀影响。

合法的 0/false 不作为“缺失”。作用域值格式错误时使用该字段的缺省值，不重新读旧值；整数溢出也回退。奖励数量/积分不允许负数；广告间隔或门槛中原有 <=0 语义保持不变。

迁移时特意保留以下差异，不能随意“统一默认值”：

| 字段 | 构造初始值 | Load 缺省值 |
| --- | --- | --- |
| NoAdsBeforeLevelCount | 0 | 5 |
| AdRewardItemCount | 3 | 1 |
| ShowBannerLevel | 8 | 5 |
| NoFailLevelAdsBeforeLevel | 10 | 5 |
| Common.TaskUnlockLevel | 10 | 6 |
| RectMatchInfinitePropDisplayMaxLevel | 2 | 0 |

旧的 LevelConfigName、IdleHintStages、行为推荐等当前无消费方的配置，暂按兼容字段保留在各自实例中，未启用新玩法逻辑。当前没有新增测试页配置覆盖入口；以后增加时必须明确退出还原与服务器刷新优先级。

## 4. 给子游戏添加资源清单

每个游戏保存独立资产：

```text
Assets/GameMain/SubGame/{GameName}/ScriptableObjects/Registry/{GameName}AssetRegistryConfig.asset
```

1. 在该目录 Create → GF → SubGame → Asset Manifest，填写 Entry 的 GameName、UIForms、Scenes、ScriptableObjects。
2. 清单仅填写逻辑名和 SO Category，不直接引用整个玩法的 Prefab/SO。
3. 打开 `Tools/SubGame/Asset Registry Config`，把清单加入全局 Manifests 列表；点击 Edit 可定位各游戏清单。
4. 通过 GF Resource Editor 将清单与全局索引一起收集到当前 `ScriptableObjects` 启动元数据组，避免根索引依赖整个玩法包。
5. 分别核对 UIForm、Scene 表及生成文件、Build Settings、Procedure 和资源构建。清单不能替代这些注册。
6. 重新构建 GF 资源后再验证非 EditorResourceMode；旧资源包不会自动更新。

当前 ResourceBuilder 输出目录仍是旧工程路径 `D:/2026/GF_Block/AssetBundle`，构建前必须先核对并改为本项目输出目录，不能直接覆盖其他项目产物。

规则：

- 空 Manifests 列表合法；Missing 清单不是“空模板”。
- GameName 不能重复；同类 AssetName 仍全局唯一，跨游戏同名不能靠清单隔离。
- 验证失败不替换已有索引；重复完整注册是重建，不会遗留旧条目。
- 移除清单引用后要重建索引；`UnregisterSubGame(gameName)` 可撤销单个游戏当前映射，但不会编辑磁盘注册文件。
- 旧 m_Entries 仅保留作迁移用途，运行时不会将其当作备用来源。旧资产使用窗口的“Back Up and Migrate Legacy Entries”；先导出根资产备份，不覆盖已有清单。

## 5. 验证与示例包边界

测试在 `Assets/AAA_DevAssets/Editor/Tests/`，Unity Test Runner 的 EditMode 下运行：

- TestModeModuleRegistryTests：模块归属、重复注册、注销、异步上下文失效。
- SubGameServerConfigTests：公共字段、独立广告策略、初始/Load 缺省值、SDK/模块加载顺序。
- SubGameAssetRegistryTests：空/单/双清单、重复键、失败回滚、注销、实际序列化清单和依赖。
- SamplePackageTests：路径边界、注册冲突/幂等、资源拥有权、通用运行时与存档注册。

游戏专用配置测试位于各自 `Scripts/Editor/`，随游戏包一起移除；公共测试不依赖具体游戏类型。

这些测试中的空/单/双注册对象不是“实际删除示例包”测试。真实删除/导入状态及后续要求见 [Sample SubGames](05_SAMPLE_SUBGAMES.md)。
