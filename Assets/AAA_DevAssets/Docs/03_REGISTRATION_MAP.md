# 03 · Registration Map — Current Sample

> 本文只描述当前样例的已实现链路。它不是下一代统一子游戏注册器的设计稿；目标架构见根目录 `GF_FRAMEWORK_TEMPLATE_GUIDELINES.md`。

## 先理解两套“注册”

当前工程有两类不同职责的注册，不能混淆：

```text
运行时子游戏路由
GameManager 显式管理器列表 → GameEntry.SubGames → SceneConfigKey / GameProcedureType

资源域解析
SubGameAssetRegistryConfig.asset → SubGameAssetRegistry → AssetUtility → 实际资源路径
```

`SubGameAssetRegistryConfig.asset` 只注册 UI、场景和 ScriptableObject 的资源归属；它**不会**自动创建管理器、加入 Procedure，或让主菜单自动出现入口。

## 当前注册点一览

| 目标 | 当前位置 | 新增子游戏时的动作 | 注意事项 |
| --- | --- | --- | --- |
| 游戏模式 ID | `Assets/GameMain/Scripts/Common/GameEnums.cs` 中的 `GameMode` | 增加唯一枚举值 | 不复用或重排已有数值。 |
| 管理器组件 | `Assets/GameMain/SubGame/{GameName}/Scripts/{GameName}GameManagerComponent.cs` | 继承 `SubGameManagerComponent`；将组件配置到 GF Entry 对象 | 当前样例每个游戏都有明确组件类型。 |
| 运行时管理器注册 | Launcher 的 `GameManagerComponent.m_SubGameManagers` | 挂载管理器实例，加入显式列表 | `GameEntry.SubGames.Get(mode)` / `Get<T>(mode)` 查询；不扫描程序集。 |
| 主菜单启动路由 | 自己的管理器 `SceneConfigKey` | 提供 Scene 配置键，不改公共 switch | `StartGame()` 跟随主玩法；`StartGame(mode)` 显式选择。 |
| 场景定义 | `Assets/GameMain/DataTables/Scene.txt` → `Scene.bytes` | 加入 ID、AssetName、音乐 ID，并重新生成 bytes | 只创建 `.unity` 文件不会被流程识别。 |
| Build Settings | `ProjectSettings/EditorBuildSettings.asset` | 将场景加入 Build Settings | 确保场景在目标平台构建中可用。 |
| 切场景资源预热 | 通用 `ProcedureChangeScene` → `SubGameRuntimeRegistry.GetForScene()` | 管理器提供正确 SceneConfigKey 并实现资源初始化 | 此处在进入游戏 Procedure 前调用 `InitializeResourcesAsync()`。 |
| 目标游戏 Procedure | 自己的管理器 `GameProcedureType` | 返回游戏 Procedure 类型，并加入 Launcher 的 Procedure 组件配置 | Scene Config / 表 / Build Settings 仍需显式登记。 |
| 子游戏资源域 | 各游戏 `ScriptableObjects/Registry/{GameName}AssetRegistryConfig.asset` + 全局 `SubGameAssetRegistryConfig.asset` | 游戏清单维护逻辑名；全局 `m_Manifests` 只挂引用 | 使用 `Tools/SubGame/Asset Registry Config`，Missing/重复键会报错；启动元数据资源组也要登记清单。 |
| UI 注册 | `Assets/GameMain/DataTables/UIForm.txt` → `UIForm.bytes`、`Scripts/UI/Runtime/UIFormId.cs` | 添加 ID、组别、暂停行为、Prefab，并同步生成 | 当前 `UIFormId : byte`，ID 不能超过 `255`；子游戏 UI 还必须添加到资源注册表。 |
| 子游戏存档 | 自己的 `Scripts/Data/`、管理器 `InitializeModule(SaveDataStore)` | 实现 `IGameSaveData`，由游戏自己注册模块 | 公共 Store 不 new 具体玩法；使用 `GameEntry.SaveData.Get<T>()`。 |
| 子游戏测试模式 | 各游戏 `Scripts/TestMode/`、自己的 Procedure、公共 `TestModeModuleRegistry` | 资源就绪后注册自己的模块；退出按实例注销并释放上下文 | `TestModeModules.cs` 仅保留通用模块，游戏退出不会清空其他页。 |
| 广告/服务器配置 | 各游戏 `Scripts/Config/` + `AdsServerConfig.Common` | 玩法专用策略按 GameMode 注册；7 个公共功能/计时字段只维护一份 | 主玩法在 GameManager Inspector 设置；副玩法不自动成为公共进度来源。参见 `04_SUBGAME_MODULES.md`。 |

## 当前实际进入链路

1. 主菜单或其他入口调用 `ProcedureMenu.StartGame(GameMode)`。
2. `ProcedureMenu` 根据模式读取 `Scene.{GameName}` 配置键，并写入 `NextSceneId`。
3. `ProcedureChangeScene` 从 `Scene` DataTable 读取 AssetName，加载场景。
4. 场景加载成功后，`ProcedureChangeScene.GetTargetSubGameManager()` 根据 Scene ID 找到对应管理器，并在加载页期间调用 `InitializeResourcesAsync()`。
5. `ChangeToGameProcedure()` 根据同一个 Scene ID 进入 `ProcedureGame{GameName}`。
6. 游戏 Procedure 打开 UI、启动管理器并执行玩法入口。

这些步骤共用同一管理器契约，不再逐个维护具体游戏的 C# switch。安装清单 `Editor/PackageManifest.json` 则用于编辑器同步磁盘注册，不能与运行时资源清单混淆。

## 资源域与逻辑键

当前 `SubGameAssetRegistryConfig.asset` 在预加载阶段注册资源域，`AssetUtility` 根据它解析：

- 子游戏 UI：`Assets/GameMain/SubGame/{GameName}/UI/{UIForm}.prefab`
- 子游戏场景：`Assets/GameMain/Scenes/SubGame/{GameName}/{Scene}.unity`
- 子游戏 SO：`Assets/GameMain/SubGame/{GameName}/ScriptableObjects/{Category}/{AssetName}.asset`

因此新增资源时，`GameName`、UIForm/Scene 的 AssetName、SO Category 与实际路径必须完全一致。资源表不是可选的说明性配置，而是运行时路径解析的一部分。

## 存档的当前规则

每个子游戏的存档应放入：

```text
Assets/GameMain/SubGame/{GameName}/Scripts/Data/{GameName}GameData.cs
```

实现 `IGameSaveData` 后，由该游戏管理器的 `InitializeModule` 向 Store 注册。`SaveDataComponent` 已移除具体游戏属性，使用：

```csharp
var data = GameEntry.SaveData.Get<MyGameGameData>();
```

不要把子游戏字段塞回公共全局数据结构，也不要使用 `CurrentLevel` 这类无前缀的 Setting/PlayerPrefs key。

## 新增前后的验证

完成一个当前样例子游戏的注册后，至少逐项确认：

- [ ] `GameMode`、Scene 配置键和 Scene DataTable 行是一一对应的。
- [ ] 场景在 Build Settings 中启用。
- [ ] 管理器组件存在于正确的 GF Entry 对象上，且 `GameEntry` 能取到它。
- [ ] `ProcedureChangeScene` 能选择正确的管理器和 Procedure。
- [ ] `SubGameAssetRegistryConfig.asset` 已登记 UI、场景和 SO。
- [ ] UIForm 的文本、bytes、ID、Prefab 路径和 UI 分组同步。
- [ ] 从主菜单进入、暂停、恢复、重启、退出和再次进入均通过。
- [ ] 子游戏退出后没有遗留事件订阅、运行对象或对象池对象。
- [ ] 如有专用测试功能，进入/退出、重复打开面板和远程请求期间切换游戏均安全；移除/导入前同时核对测试模式依赖，见 [示例子游戏管理说明](05_SAMPLE_SUBGAMES.md)。

若某个步骤需要改动公共路由、公共加载协议或资源目录规则，应视为架构变更并先确认，不应以“新增一个子游戏”为由顺手改造。
