# 开发文档导航

按“框架能力、具体模块、示例、设计、历史记录”组织。文档描述当前工作区的实现与限制；代码、注册表和 Unity 序列化配置仍是事实来源。

## 从这里开始

| 需要做什么 | 阅读入口 |
| --- | --- |
| 第一次运行工程 | [Getting Started](GettingStarted.md) |
| 查框架模块职责、配置与 API | [框架模块索引](Framework/README.md) |
| 按效果图新增或修改页面 | [UI 制作流程](Framework/UI/PageWorkflow.md) → [复用目录](Framework/UI/ReuseCatalog.md) → [Prefab 规范](Framework/UI/PrefabRules.md) → [验收](Framework/UI/Validation.md) |
| 修改多语言文案或接入语言 | [本地化](Framework/Localization/README.md) |
| 修改字体、样式或更新字符集 | [TMPFont](Framework/TMPFont/README.md) → [字体制作](Framework/TMPFont/FontAuthoring.md) |
| 接入活动或子游戏 | [活动](Framework/Activity/README.md) / [子游戏](Framework/SubGame/README.md) |
| 排查注册遗漏 | [跨模块注册映射](Framework/RegistrationMap.md) |
| 制作通行证页面 | [通行证模块说明](../../GameMain/Activities/SeasonPass/Docs/README.md) |
| 查看示例、导出、移除或回装 | [示例索引](Samples/README.md) |
| 查待实现方案 | [设计索引](Design/README.md) |
| 查过去做过什么、当时验证了什么 | [历史记录](Archive/README.md) |

## 具体模块的说明在哪里

通行证、收集、竞赛等具体规则和页面字段归 `Assets/GameMain/Activities/{ActivityName}/` 内的 README 或 Docs；具体玩法说明归 `Assets/GameMain/SubGame/{GameName}/`。框架手册引用案例，不持有具体活动的规则、奖励档位或 Prefab 字段契约。

现有入口：[SeasonPass](../../GameMain/Activities/SeasonPass/Docs/README.md)、[Collector](../../GameMain/Activities/Collector/README.md)、[Race](../../GameMain/Activities/Race/README.md)、[GalaxyChallenge](../../GameMain/Activities/GalaxyChallenge/README.md)、[Mining](../../GameMain/Activities/Mining/README.md)。未整理的模块以其代码和配置为准，不用其他活动的说明代替。

## 人与 Agent 共用一套规则

UIAgent 的任务路由见 [ui-agent Skill](../../../.agents/skills/ui-agent/SKILL.md)。它根据需求选择上述手册，再读取所属业务说明；不复制框架规则，不把示例或设计稿当作现成功能。Skill 是操作流程入口，不代表已有自动识图、自动制作 Prefab 的独立程序。

## 文档维护约定

- 每个模块以 README 说明职责与边界、功能、前置配置、接入、使用、生命周期、排错和验收；内容较多时才拆专项流程。
- 一个规则只由一个专题维护；其他文档提供链接。跨模块注册映射是索引，不再复制完整操作步骤。
- 不使用全局数字编号；文件名表示内容，模块顺序由导航维护。
- 当前使用说明放 Framework；具体业务跟模块；未实现方案放 Design；历史实施与验证放 Archive。已实现内容转入使用手册，历史依据仍保留。
- 新增复用条目应列真实路径、输入、限制和说明入口。未验证的候选明确标注，不能因文件存在就称为可直接复用。
- 移动 Unity 管理的文档通过 AssetDatabase 保留 GUID，同步 Markdown、Skill 和工具路径引用；不借文档整理改业务逻辑。

完整架构约束见[框架规范](../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md)，执行边界见 [AGENTS.md](../../../AGENTS.md)。
