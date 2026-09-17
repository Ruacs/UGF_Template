---
name: gf-workflow
description: "在使用 Lokas 命名空间和 GF 模板规范的 Unity 工程中修改脚本、绑定组件、接入 UI/资源、子游戏或活动模块时使用。按任务选择必要文档、修改范围与验证；不用于无关项目、纯概念问答或通用文案。"
---

# GF 项目工作流

这是随工程维护的 AI 执行入口。遵守根目录 [AGENTS.md](../../../AGENTS.md) 的授权、分级与验证要求；本 Skill 不授权额外改造，不默认启动其他 Agent。

## 从目标进入

1. 从用户请求确定交付物与完成条件。用户只要求方案或解释时，就交付方案或解释；不要顺手执行附带示例。
2. 先看指定脚本、Prefab/场景、所在模块和直接调用接口。需要参考时优先找一个现有同类实现；有证据表明跨模块依赖时再扩大搜索。
3. 选下表对应行，读取指定文档的相关小节。普通绑定不读新增子游戏流程；同一会话已读且未变化的文档无需重复读取。
4. 复用现有 Unity 工具完成操作，再按 AGENTS 的对应行验证。未完成的验证明确报告，不能因追求快速而跳过必要检查。

## 小任务的边界

“创建脚本并绑定到现有 Prefab”的通常范围是目标脚本、目标 Prefab 及必要直接调用。

- 先确认目标资产、组件接口和字段，再添加组件、绑定、保存和读回引用。
- 普通 Widget/子组件不是独立 GF 窗口，不因新增一个脚本就添加 UIForm ID 或改表。只有新增/改变独立窗口注册时才走 UI 注册流程。
- 新增绑定逻辑所需的空值处理、事件解绑属于正确实现；新业务规则、异步和复杂状态另按其风险验证。
- 旧组件仅在阻碍要求的功能时做必要修复，并说明依赖；不顺手重写刷新机制、奖励逻辑或其他页面。
- 不为一次绑定自动新增配置工具、测试体系或设计文档。已有相关测试可以复用，是否补测试由新增行为与风险决定。
- 当要求的组件、引用和交互均已验证后结束；发现其他问题单独报告。

## 必守的项目约束

以下是稳定约束摘要；解释、示例及迁移边界见 [完整规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) 对应章节，不默认全文加载。

| 项目 | 约束 |
| --- | --- |
| 命名空间 | 运行时根命名空间固定 `Lokas`，编辑器 `Lokas.Editor`；换产品名不改根命名空间，生成器保持一致。 |
| 通用与玩法边界 | 公共层不依赖具体子游戏类型；复用现有管理器、Procedure、事件和加载流程，不复制全局系统。 |
| 子游戏代码 | 具体玩法仅位于 `Assets/GameMain/SubGame/{GameName}/Scripts/`。`Assets/GameMain/Scripts/SubGame/` 只放跨游戏辅助。 |
| 子游戏资源 | 专用 UI 脚本在其 `Scripts/UI/`，Prefab 在其 `UI/`，SO 在其 `ScriptableObjects/`；其余玩法资源归属同一子游戏目录。场景沿用 `Assets/GameMain/Scenes/SubGame/{GameName}/`。 |
| 公共资源 | `Assets/GameMain/UI/`、`Entities/` 等公共目录仅放通用资源；全局 SO/注册索引在 `Assets/GameMain/ScriptableObjects/`。 |
| 音频例外 | 音效/音乐统一在 `Assets/GameMain/Audio/Sound/`、`Audio/Music/`；通过 ID/表/配置表达用途，不按子游戏物理拆分。 |
| 活动资源 | 可复用活动在 `Assets/GameMain/Activities/{ActivityName}/`，UI 脚本/Prefab 分别在 `Scripts/UI/`、`UI/`，配置在 `ScriptableObjects/`；游戏事实适配仍归目标子游戏。 |
| 资源加载 | 使用现有统一路径工具、资源键、注册表，显式声明资源域；不靠调用上下文猜归属。拆清单不表示已支持跨游戏同名资源键。 |
| 开发辅助 | 文档/外部脚本/Unity 编辑器工具分别在 `Assets/AAA_DevAssets/Docs/`、`Tools/`、`Editor/`；AI Skill 在工程 `.agents/skills/`。不把新资源堆到根目录或临时 Create 目录。 |
| 包与备份 | Assets 内仅向 `Assets/AAA_DevAssets/SamplePackages~/` 导出示例包，保留末尾 `~`；操作备份在根目录 `Backups/`。完整工程 ZIP 用文件系统打包并包含被忽略目录。 |

## 按任务查阅

文档链接相对本 Skill。先定位表中标题/编号，再读取相关段落；不要递归读取每个链接。跨领域任务组合必要行即可。

| 任务触发条件 | 阅读位置 | 执行时关注 |
| --- | --- | --- |
| 局部 Bug、现有组件绑定或 Widget 调整 | 目标代码、Prefab、直接依赖；本 Skill 的小任务边界 | 有新增 GF 窗口、存档、资源注册或生命周期影响时才加对应行。 |
| 新增/改变独立 GF UI 窗口 | [规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) §6；[注册映射](../../../Assets/AAA_DevAssets/Docs/03_REGISTRATION_MAP.md) 的 UI 注册行 | UIForm 文本、bytes、ID、Prefab、分组、暂停/多实例规则和资源域同步。使用 UI Panel Manager 维护生成链；先核实当前 ID 类型与工具输出目录限制。 |
| 新增 SO、改变资源键/归属或加载 | [规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) §7/§10；[注册映射](../../../Assets/AAA_DevAssets/Docs/03_REGISTRATION_MAP.md) 的资源域部分 | 按字段需要选 IdOnlyConfigSO/DisplayConfigSO；配置、注册键、路径和收集一致；移动资产或改加载协议先按高风险处理。 |
| 场景、Procedure、Config、DataTable | [注册映射](../../../Assets/AAA_DevAssets/Docs/03_REGISTRATION_MAP.md) 对应行；[规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) §8 | 场景表、生成物、Build Settings、Procedure、资源注册是独立环节；只做本次需要的环节并验证链路。 |
| 新增子游戏 | [当前新增流程](../../../Assets/AAA_DevAssets/Docs/02_NEW_SUBGAME_CURRENT.md)；按该游戏需求查注册映射/模块接入 | 管理器显式加入 GameManager 列表，经 GameEntry.SubGames 路由；提供场景键/Procedure/进度，切场景阶段准备首屏资源；不改公共层引用具体游戏。 |
| 子游戏存档 | [当前新增流程](../../../Assets/AAA_DevAssets/Docs/02_NEW_SUBGAME_CURRENT.md) §3；[规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) §5.5 | 自己的 Scripts/Data 内实现 IGameSaveData，经 InitializeModule(SaveDataStore) 注册；用 GameEntry.SaveData.Get<T>()；Key 带游戏前缀，不恢复全局 GameData 或改公共 Store 逐个 new。 |
| 测试页、服务器配置、资源清单 | [模块接入](../../../Assets/AAA_DevAssets/Docs/04_SUBGAME_MODULES.md) 匹配小节；涉及约束时查[规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) §5.6–5.8 | 每游戏必须有 Scripts/TestMode 页面入口；Scripts/Config 持有专用字段/默认值/Key。7 个公共功能/计时字段留 Common，公共功能跟随 PrimaryGameMode，关卡广告按所属游戏。清单在各游戏 ScriptableObjects/Registry，全局只引用聚合；检查重复、缺失、空模板与退出清理。 |
| 多语言文本、图片、字符集/字体 | [规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) §9；相关现有源文件和生成入口 | Key 集合、源/目标语言、字符集、TMP SDF、语言图片与收集同步；已有语言 Skill 可按需使用。 |
| 可复用活动接入、活动页面或生命周期 | [活动接入](../../../Assets/AAA_DevAssets/Docs/06_ACTIVITY_MODULES.md) 对应小节；[规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) §5.9 | 公共宿主/契约与具体活动分离，通过 GameEntry.Activities/Catalog 显式装配；声明游戏来源；停用清理/保存成功后才允许移除，默认保留存档。核实当前工具能力，不推定已有移除/导入工具。 |
| 示例导出、移除、回装或完整工程交付 | [示例管理](../../../Assets/AAA_DevAssets/Docs/05_SAMPLE_SUBGAMES.md)；完整工程交付另查 [README](../../../README.md) 的交付章节 | 使用 Tools/Template/Sample SubGames，先备份和检查依赖；包与同名 .unitypackage.json 一起保留，不直接删目录、不重排 ID/GUID/存档 Key；按零/单/双示例及重复导入/恢复矩阵验收。 |
| 新增可复用编辑器工具 | [规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) §6.4 | 菜单用 Tools/{Category}/{ToolName}，同步到 Assets/GameMain/Editor/CustomToolBars/CustomToolBars.cs 的 ShowToolsMenu()；不得借一次绑定扩建工具体系。 |
| 框架设计、新模板规划或公共契约变更 | [完整规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md) 相关领域；需要整体设计时再阅读全文 | 区分当前实现与目标能力，先明确授权范围、兼容性及迁移方式。 |

## 工具与事实来源

- 本 Skill 决定“要做哪些项目步骤”；现有 Unity Skills/MCP 决定“如何操作 Editor”。选择已可用工具，按该工具自己的连接、参数和权限说明执行。
- 已知目标时只加载需要的操作模块与 schema；不默认做全工程侦察、加载全部模块或调用完整 API 清单。已确认的连接/参数按工具缓存规则复用。
- 实际资产修改后，从 Editor/资产读回结果；工具返回成功不等于引用正确，也不等于运行验证完成。
- 当前 API 和行为以代码、序列化配置、注册表核实；人类规范中的示例 API、待实现工具、DRAFT 和历史验证结论不可当成当前能力。
- 出现冲突只核实受影响的事实，记录差异；不根据目标规范擅自迁移现有功能。

## 维护这份 Skill

只保留会改变 AI 决策的项目约束与路由。详细操作、原理、历史状态留在已有专题文档；变更规则时同步本摘要中受影响的行，不复制整个章节。

新增专题先增加一个明确触发条件和文档入口；只有现有文档缺少必要指引时才新增参考文件或脚本。不要把单次任务的解决办法升级成所有任务的强制流程。
