# GF Template

这是一个基于 Unity Game Framework 的多子游戏示例模板。它用于演示通用系统与具体玩法的边界、资源域、UI/场景注册和配置加载方式。

本文档优先帮助新成员把工程跑起来；新增功能前，请先确认你维护的是“当前样例”还是“下一代模板目标”。两者不能混为一谈。

## 首次运行

1. 使用 Unity `2022.3.62f3c1` 打开工程。首次拉取工程时，Package Manager 中的 Git URL 包需要本机可用的 Git。
2. 等待 Unity 完成 Package 导入，并先处理 Console 中的编译错误；不要在编译未完成时修改 Prefab 或场景。
3. 打开 `Assets/GameMain/Scenes/GameLauncher.unity`，它也是当前 Build Settings 的第一个场景。
4. 进入 Play Mode。正常路径应为启动预加载，再进入主菜单；从主菜单可进入已注册的子游戏。
5. 首次验证以“无新增编译错误、可进入主菜单、可从主菜单进入一个子游戏并返回”为最低标准。

如果资源、UI 或配置没有加载，不要先在业务代码中硬编码路径。先查看 [注册映射](Assets/AAA_DevAssets/Docs/03_REGISTRATION_MAP.md) 和 [当前样例新增子游戏流程](Assets/AAA_DevAssets/Docs/02_NEW_SUBGAME_CURRENT.md)。

## 从哪里开始阅读

`Assets/AAA_DevAssets/Docs/` 中的 5 份使用指南按 `01–05` 编号；第一次接手可按顺序阅读，需要移除或回装示例时可直接查看 `05`。未编号的验证报告和实施记录用于内部追溯，不属于新人必读内容。

| 目标 | 文档 |
| --- | --- |
| 首次打开工程、生成文件和常见故障 | [01 · Getting Started](Assets/AAA_DevAssets/Docs/01_GETTING_STARTED.md) |
| 维护当前样例并新增子游戏 | [02 · New SubGame — Current Sample](Assets/AAA_DevAssets/Docs/02_NEW_SUBGAME_CURRENT.md) |
| 查找当前所有注册点 | [03 · Registration Map](Assets/AAA_DevAssets/Docs/03_REGISTRATION_MAP.md) |
| 接入测试页面、服务器配置和资源清单 | [04 · SubGame Modules](Assets/AAA_DevAssets/Docs/04_SUBGAME_MODULES.md) |
| 将样例工程初始化为不含玩法的产品基线 | [05 · Sample SubGames](Assets/AAA_DevAssets/Docs/05_SAMPLE_SUBGAMES.md) |
| 新建模板或演进架构 | [GF Framework Template Guidelines](GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) |
| 让 Agent 协作时遵守的边界 | [AGENTS.md](AGENTS.md) |

## 当前样例与目标模板

`GF_FRAMEWORK_TEMPLATE_GUIDELINES.md` 定义模板约束。当前已实现 `GameEntry.SubGames`：GameManager 持有显式管理器列表，公共路由通过游戏提供的场景键与 Procedure 类型进入玩法。少量强类型快捷入口放在所属子游戏的 partial GameEntry 中，不再由公共入口依赖具体游戏。

因此：

- 维护现有样例时，以代码、Unity 配置、资源注册表和 `Assets/AAA_DevAssets/Docs/02_NEW_SUBGAME_CURRENT.md` 为准。
- 新建模板或改造公共机制时，以 `GF_FRAMEWORK_TEMPLATE_GUIDELINES.md` 为准。
- 两者冲突时，记录差异并确认后再改；不要仅为了让文档“看起来一致”而移动资源或重构运行时代码。

## 示例子游戏与新项目初始化

当前工程带有两个可运行的示例子游戏：`HexaAway` 与 `RectMatch`。它们用于演示关卡、UI、资源域、存档和场景切换，不应默认成为新产品功能。

创建新产品时，应移除这两个示例玩法。使用 `Tools/Template/Sample SubGames` 的检查、导出、受控移除与导入功能，**不要直接删文件夹**：工具还需要同步 Launcher、流程、UI/Scene 表及生成物、Build Settings、资源索引和资源收集。导出文件位于 `Assets/AAA_DevAssets/SamplePackages~/`，每个 `.unitypackage` 必须与旁边的 `.json` 清单一起保存。通过工具的“打开示例包目录”查看文件；末尾 `~` 使 Unity 忽略该目录，因此不会出现在 Project 面板。详见 [Sample SubGames](Assets/AAA_DevAssets/Docs/05_SAMPLE_SUBGAMES.md)。

测试页、游戏专用服务器配置和资源清单已分别归属到两个游戏目录。接入方法和公共字段边界见 [SubGame Modules](Assets/AAA_DevAssets/Docs/04_SUBGAME_MODULES.md)。

## 目录速览

```text
Assets/GameMain/Scripts/                 通用运行时代码
Assets/GameMain/SubGame/{GameName}/      子游戏脚本和专用资源域
Assets/GameMain/Scenes/SubGame/          子游戏场景
Assets/GameMain/Audio/{Sound,Music}/     全局音频入口
Assets/GameMain/ScriptableObjects/       全局框架配置和注册表
Assets/AAA_DevAssets/Docs/               新人指南、开发规范补充及验证记录
Assets/AAA_DevAssets/Tools/              外部辅助脚本（如 PowerShell MCP 连接）
Assets/AAA_DevAssets/SamplePackages~/    示例包及校验清单（Unity 忽略导入）
Assets/AAA_DevAssets/Editor/             Unity 编辑器工具与测试
Backups/                               本机操作恢复点，不放入 Assets
```

子游戏专用 UI 的目标位置是：

```text
Assets/GameMain/SubGame/{GameName}/Scripts/UI/
Assets/GameMain/SubGame/{GameName}/UI/
```

## 交付给其他开发者

完整模板 ZIP 应包含 `Assets/`（连同 `.meta` 及 `SamplePackages~/` 的真实文件）、`Packages/`、`ProjectSettings/`，以及根目录的 `README.md`、`AGENTS.md`、`GF_FRAMEWORK_TEMPLATE_GUIDELINES.md`。通常不包含 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`Backups/`、IDE 缓存和生成的 `.csproj` / `.sln`。发送前另行审查 SDK Key、服务地址等产品私有配置。

用文件管理器或压缩工具打包，并检查 ZIP 内确实包含 `SamplePackages~/`；不要使用 Unity 的 Export Package 代替完整工程压缩，因为被 Unity 忽略的目录不会随资源导出。示例目录内的 `SampleSubGames-*.zip` 只是两个玩法包和说明的合集，**不是完整模板工程**。

## 不要踩的 Unity 资源坑

- 不要删除、重新生成或在资源管理器中单独移动 `.meta` 文件。
- 移动场景、Prefab、ScriptableObject 等资源优先在 Unity Editor 内完成。
- `UIForm.txt`、`UIForm.bytes` 与 `UIFormId.cs` 是同一条生成链；修改后必须同步生成，不要只改其中一个。
- 子游戏资源通过逻辑资源键和注册表加载，业务代码不要拼接 `Assets/GameMain/...` 路径。

详细规则见 [Getting Started](Assets/AAA_DevAssets/Docs/01_GETTING_STARTED.md)。
