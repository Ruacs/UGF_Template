# 02 · New SubGame — Current Sample

> 本文对应当前已实现的显式运行时注册、资源清单和示例包工具；新增自定义玩法仍需完成下列注册，并非全自动生成。完整架构约定请看 `GF_FRAMEWORK_TEMPLATE_GUIDELINES.md`；注册点见 [Registration Map](03_REGISTRATION_MAP.md)。

## 开始前先定 8 件事

在创建文件前，写下并确认以下信息：

1. `GameName`：PascalCase、无空格，例如 `HexaAway`。
2. `GameMode`：唯一枚举值。
3. Scene ID、Scene 配置键和 Scene AssetName。
4. 是否需要独立 Procedure、UI、场景和首屏预加载。
5. 是否需要存档、对象池、计时、广告/数据统计。
6. 玩法资源、SO、UI、音频和多语言资源的归属。
7. 哪些资源是公共的，哪些只属于该子游戏。
8. 进入、暂停、恢复、重启、结束和返回菜单的验收方式。

任何一项不明确时，先不要创建新的全局 Manager、Singleton、事件总线或资源目录。

## 1. 创建子游戏资源域

创建以下目录；未使用的类别可以暂时不创建，但不要把专用内容塞入公共目录：

```text
Assets/GameMain/SubGame/{GameName}/
├── Scripts/
│   ├── Core/
│   ├── Data/
│   ├── Input/
│   ├── Runtime/
│   ├── View/
│   ├── Procedure/
│   └── UI/
├── Config/
├── Prefabs/
├── Entity/
├── Texture/
├── Material/
├── UI/
└── ScriptableObjects/
    ├── Database/
    ├── Level/
    ├── Pattern/
    └── Rule/
```

例外：音效和音乐统一放到 `Assets/GameMain/Audio/Sound/`、`Assets/GameMain/Audio/Music/`，并通过 SoundId、DataTable 或资源注册表达用途和归属。

## 2. 创建运行时管理器

在 `Scripts/` 下创建 `{GameName}GameManagerComponent`，继承 `SubGameManagerComponent`；有对象池需求时使用 `PooledSubGameManagerComponent<T>`。

管理器需要明确承担：

- `GameStart`、`PauseGame`、`ResumeGame`、`Restart`、`GameOver`、`ReturnMenu`、`ResetGame`。
- 资源初始化：重写 `OnInitializeResourcesAsync()`，让场景切换阶段可完成首屏资源加载。
- 注册能力：提供 `GameMode`、`SceneConfigKey`、`GameProcedureType`、`CurrentLevel` / `DisplayLevel`，在进度变化时调用 `NotifyProgressChanged()`。
- 可选能力：仅按需实现 `CanGrantProp` / `TryGrantProp`、设置项能力；公共 UI 不添加具体游戏类型判断。
- 输入、对象池、关卡构建、运行对象和事件订阅的清理。

不要由玩法管理器创建新的全局 Singleton；它应作为现有 GF Entry 的组件配置并由框架取得。

## 3. 建立独立存档域

在 `Scripts/Data/` 创建 `{GameName}GameData.cs` 并实现 `IGameSaveData`。然后：

1. 在自己的管理器 `InitializeModule(SaveDataStore store)` 中调用 `store.Register(new MyGameGameData())`，重复初始化时复用 `store.Get<T>()`，不要在公共 Store 构造函数逐个 new 游戏存档。
2. 新代码通过 `GameEntry.SaveData.Get<{GameName}GameData>()` 获取模块。
3. 所有底层 key 使用子游戏前缀，例如 `{GameName}CurrentLevel`。
4. 验证首次默认值、已有存档读取、写入后重启恢复和事件不重复触发。

`SaveDataComponent` 不再提供 HexaAway / RectMatch 强类型属性。公共 Store 只管理显式注册模块；模块缺失时 `Get<T>()` 返回 null。移除示例不删除底层存档 Key。

## 4. 接入当前运行时路由

当前通过 `GameManagerComponent.SubGames` / `GameEntry.SubGames` 的显式注册集合路由：

| 步骤 | 当前位置 | 完成条件 |
| --- | --- | --- |
| 增加模式 | `Scripts/Common/GameEnums.cs` | `GameMode.{GameName}` 唯一。 |
| 获取管理器 | Launcher 的 `GameManagerComponent` 已安装列表 | 挂载管理器 Prefab，并把实例显式加入列表；不改公共 GameEntry。 |
| 主菜单选择场景 | 自己的管理器 `SceneConfigKey` | 返回 `Scene.{GameName}`；入口调用 `StartGame(GameMode)`，不加公共 switch。 |
| 场景资源预热 | 自己的管理器资源初始化覆盖 | 通用路由根据场景配置键找到管理器并初始化资源。 |
| 进入游戏 Procedure | 自己的管理器 `GameProcedureType` | 返回 `typeof(ProcedureGame{GameName})`。 |
| Procedure 配置 | `GameLauncher.unity` 中的 GF Procedure 组件 | 新 Procedure 已加入可用 Procedure 列表。 |
| 全局计时/统计 | 管理器 `CurrentLevel` 与原有状态事件 | 公共计时按显式模式查询，不添加具体类型依赖。 |

参考已有游戏的 `GameManagerComponent.Module.cs`；可选强类型 GameEntry 快捷属性也必须放在自己目录的 partial 文件中。示例包工具目前只支持 HexaAway / RectMatch 白名单，不是通用新游戏生成器。

## 5. 注册场景和资源域

1. 将场景创建在 `Assets/GameMain/Scenes/SubGame/{GameName}/`。
2. 在 `Assets/GameMain/DataTables/Scene.txt` 增加 Scene ID、AssetName、背景音乐；生成 `Scene.bytes`。
3. 将场景加入 Build Settings。
4. 在该游戏的 `ScriptableObjects/Registry/` 创建 `SubGameAssetManifest` 资产（Create → GF → SubGame → Asset Manifest），命名 `{GameName}AssetRegistryConfig.asset`。
5. 在自己的清单登记 GameName、UI Form AssetName、Scene AssetName、SO 的 AssetName/Category；再用 `Tools/SubGame/Asset Registry Config` 把清单加入全局索引的 `Manifests`。不要恢复在全局填写玩法明细。
6. 确认实际路径与资源注册表一致：

```text
UI: Assets/GameMain/SubGame/{GameName}/UI/{UIForm}.prefab
Scene: Assets/GameMain/Scenes/SubGame/{GameName}/{Scene}.unity
SO: Assets/GameMain/SubGame/{GameName}/ScriptableObjects/{Category}/{AssetName}.asset
```

不要从业务代码调用硬编码的 `GetSubGameAsset("GameName", ...)` 来表达归属；使用资源键、注册表和现有加载入口。

## 6. 创建 UI

子游戏 UI 的目标位置是：

```text
Assets/GameMain/SubGame/{GameName}/Scripts/UI/
Assets/GameMain/SubGame/{GameName}/UI/
```

每个页面还需同步：

- UI Panel 脚本和 Prefab；
- `UIForm.txt`、`UIForm.bytes`、`UIFormId.cs`；
- UI 分组、覆盖页面暂停行为、多实例策略；
- 自己的资源清单中的 UI Form，以及全局清单引用；
- 多语言文本、图片和必要的字符集/TMP SDF 字体。

当前 `Tools/UI/UI Panel Manager` 只支持公共 UI 输出目录。它可协助理解 UIForm 生成链，但不能直接作为子游戏 UI 的生成器；在工具升级前，请按上述子游戏路径创建资源，再手工完成注册与生成。

## 7. 测试页面、配置与主玩法

每个游戏还需接入自己的 `Scripts/TestMode/` 页面及 `Scripts/Config/` 服务器配置，具体步骤见 [SubGame Modules](04_SUBGAME_MODULES.md)。公共首胜/排行/商店/计时配置不要复制进副玩法。若新游戏替代主玩法，在启动场景 GameManager Inspector 修改主玩法，再验证公共功能的进度来源。

## 8. 最小验收清单

- [ ] Unity 无新增编译错误。
- [ ] 场景在 Build Settings、Scene.txt/Scene.bytes 和子游戏资源注册表中均已登记。
- [ ] 从主菜单可进入新子游戏，加载页不会在首屏资源完成前关闭。
- [ ] 资源、SO、Prefab、UI 都通过逻辑键加载，不依赖硬编码物理路径。
- [ ] 暂停、恢复、重启、失败/胜利、返回菜单和再次进入都正常。
- [ ] 独立存档可读写，Key 不与其他游戏冲突。
- [ ] 退出后事件、运行对象和对象池对象均已清理。
- [ ] 所有受影响语言下，文本、图片和字体显示正常。

## 不应作为“快速接入”的做法

- 把玩法脚本放入 `Assets/GameMain/Scripts/UI`、`Scripts/Component` 或 `Scripts/SubGame`。
- 为省步骤把玩法 UI 放进公共 `Assets/GameMain/UI/UIPanel/`。
- 只建场景或 Prefab，不更新表、注册表和流程路由。
- 复制某个历史子游戏的全局组件、静态单例或事件系统。
- 为新增玩法修改公共入口去实例化具体管理器/存档，或用程序集扫描替代显式安装列表。
