# 01 · Getting Started

本指南面向第一次接手 GF Template 的开发者。目标是先建立一个可靠的编辑、生成、验证闭环，再开始修改玩法或资源。

## 环境要求

- Unity：`2022.3.62f3c1`，以 `ProjectSettings/ProjectVersion.txt` 为准。
- 首次从版本库打开工程时，建议安装 Git。`Packages/manifest.json` 包含 Git URL 依赖，Unity Package Manager 需要能够解析它们。
- 不要把其他 Unity 版本生成的 `Library/` 目录当作可复用工程内容；Unity 应自行导入与重建缓存。

## 首次打开与最小验证

1. 在 Unity Hub 中选择工程根目录并使用指定版本打开。
2. 等待 Package Manager 和脚本编译完成。编译仍在进行时，不要保存场景或 Prefab。
3. 打开 `Assets/GameMain/Scenes/GameLauncher.unity`。
4. 进入 Play Mode，确认可进入主菜单。
5. 从主菜单进入一个已注册子游戏，再测试暂停、恢复、重启和返回菜单。
6. 检查 Console：任何新增的 Error 都应先定位并处理，再继续资源或玩法工作。

模板初始 Build Settings 包含 Launcher、主菜单、HexaAway 和 RectMatch 场景；移除或回装示例后，以工具同步的实际场景列表为准。新增场景不能只放入目录，还必须经过场景表、注册、Procedure 和 Build Settings 的完整链路，详见 [Registration Map](03_REGISTRATION_MAP.md)。

> 当前工程包含 HexaAway 和 RectMatch 两个示例子游戏。新产品初始化时不要手动删除它们的目录；请先阅读 [Sample SubGames](05_SAMPLE_SUBGAMES.md)。

## 设置主玩法与寻找扩展入口

打开 `GameLauncher.unity`，选择 `GameEntry/GameFramework/GameManager`，在 Inspector 的“主玩法”下设置模式；模板初始默认 HexaAway，使用者调整后的配置不会被目录迁移或包导入重置。也可从 `Tools/Template/Sample SubGames` 点击“定位主玩法 / 已安装管理器”。副玩法不会自动成为公共解锁进度来源。

每游戏测试页、广告配置及资源清单的接入步骤见 [SubGame Modules](04_SUBGAME_MODULES.md)。新项目通过 `Tools/Template/Sample SubGames` 受控移除两个示例；回装时选择 `.unitypackage`，并保留旁边的 `.json` 清单。不要直接删目录，完整步骤见 [Sample SubGames](05_SAMPLE_SUBGAMES.md)。

## 开发辅助文件放在哪里

文档统一在 `Assets/AAA_DevAssets/Docs/`；PowerShell 等外部脚本在 `Assets/AAA_DevAssets/Tools/`；Unity C# 编辑器工具仍在 `Assets/AAA_DevAssets/Editor/`。

示例包在 `Assets/AAA_DevAssets/SamplePackages~/`。末尾 `~` 会让 Unity 忽略导入，所以在 Project 面板找不到这个文件夹是正常的；请从 `Tools/Template/Sample SubGames` 点击“打开示例包目录”。保留 `.unitypackage` 与同名 `.unitypackage.json`，不要改掉目录末尾的 `~`。根目录 `Backups/` 是本机恢复点，不属于 Assets，也通常不随模板交付。

完整工程压缩范围见根目录 [README](../../../README.md#交付给其他开发者)。辅助脚本如需使用 MCP，从工程根目录执行 `./Assets/AAA_DevAssets/Tools/Invoke-UnityMcp.ps1`；它不参与游戏运行，也不替代 Unity 编辑器工具。

## Unity 资源操作规则

- 始终让 `.meta` 与资源文件一起移动、复制和提交；不要单独删除或重建 `.meta`。
- 优先通过 Unity Project 面板移动或重命名 `.unity`、`.prefab`、`.asset`。这样 GUID 引用会被 Unity 维护。
- 不在业务代码中手拼 `Assets/GameMain/...` 路径；使用现有资源键、`AssetUtility` 和子游戏资源注册表。
- 子游戏专用资源放在 `Assets/GameMain/SubGame/{GameName}/`，音效和音乐例外，统一位于 `Assets/GameMain/Audio/Sound/` 与 `Assets/GameMain/Audio/Music/`。
- 资源路径、资源注册或序列化字段变化后，必须实际进入目标场景验证，不能只以文件存在作为通过标准。

## 源文件与生成物

| 你修改的内容 | 同步内容 | 正确入口 |
| --- | --- | --- |
| `Assets/GameMain/DataTables/UIForm.txt` | `UIForm.bytes`、`Scripts/UI/Runtime/UIFormId.cs` | `Tools/UI/UI Panel Manager` |
| DataTable 文本源 | 对应 `.bytes`，以及生成代码（如该表需要） | `Tools/DataTable/Generate DataTables` |
| 子游戏 UI/场景/SO 所属资源域 | 游戏自有资源清单 + 全局 `m_Manifests` 引用 + 启动元数据资源组 | `Tools/SubGame/Asset Registry Config`，随后核对 GF Resource Editor/Builder |
| 含文本的多语言图片 | 各语言同名资源、引用和资源收集 | 按 `GF_FRAMEWORK_TEMPLATE_GUIDELINES.md` 第 9 节检查 |

`UI Panel Manager` 目前只生成通用 UI 到公共目录，不能直接生成子游戏 UI。新增子游戏 UI 时不要为了使用它把文件放入公共目录；请按目标子游戏目录创建并完成 UIForm 与资源注册。

## 验证方式

按改动范围选择验证，而不是只做一次编译：

- **代码**：确认 Unity 无新增编译错误；在 Test Runner 中执行相关 EditMode 或 PlayMode 测试。
- **UI**：打开页面、检查组别/覆盖暂停/多实例行为，并确认 Prefab 能由 UIForm 配置解析。
- **资源与 SO**：从真实运行入口加载，确认逻辑资源键、注册表类别和实际路径一致。
- **场景与 Procedure**：从主菜单进入，确认加载页覆盖首屏资源初始化，并能返回主菜单。
- **多语言**：切换所有受影响语言，检查 Key、图片、字符集和 TMP SDF 字体。

## 常见故障排查

### UI 页面脚本存在，但页面无法打开

依次检查 UIForm ID、`UIForm.txt`、`UIForm.bytes`、Prefab 路径、UI 分组以及子游戏资源注册表。不要只检查 C# 脚本是否编译。

### 子游戏场景能在 Editor 里打开，但从主菜单无法进入

检查 Scene DataTable 的 ID/AssetName、Build Settings、`SubGameAssetRegistryConfig.asset`，以及 GameManager 显式管理器列表中的 `SceneConfigKey` / `GameProcedureType`。当前路由使用 `GameEntry.SubGames`；不要通过增加公共 switch 掩盖未注册的问题。

### 新增资源后运行时找不到

先确认资源使用的是逻辑键而不是硬编码路径，再检查其资源域、注册表项、Category 与实际目录是否一致。

### 文档中的 API 在代码里找不到

先确认它是否属于目标模板能力。`GF_FRAMEWORK_TEMPLATE_GUIDELINES.md` 描述的是目标架构；维护当前样例应优先查阅 [New SubGame — Current Sample](02_NEW_SUBGAME_CURRENT.md) 和当前代码。
