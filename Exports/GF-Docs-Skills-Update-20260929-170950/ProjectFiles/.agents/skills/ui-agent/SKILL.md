---
name: ui-agent
description: 在本 GF Unity 工程中分析 UI 效果图、选择改版或新增页面、查找可复用 Prefab/Widget，并完成页面制作、绑定、注册和验收。适用于结算页、弹窗、活动页面及局部 UI 修改；业务规则由所属模块提供。
---

# UIAgent

此 Skill 定义页面任务的执行流程，使用项目现有 Unity 工具。它不是独立的自动识图或 Prefab 生成程序。开发者和 Agent 共用框架手册，不在这里复制组件 API 和制作规范。

## 在其他项目使用

推荐把本 Skill 放在目标工程的 `.agents/skills/ui-agent/`，与 `Assets/AAA_DevAssets/Docs/` 一起更新，下面的相对链接即可按同样目录解析。若工程目录不同，先定位目标项目的文档导航并调整路由，不使用来源工程的绝对路径。

文档中的页面名、ID 范围、资源目录和复用目录是来源版本的实现说明。接手目标项目时先核对对应代码、注册表、Prefab 与工具是否存在，保留目标项目的有效差异；缺少的模块标为未接入，不能靠复制文档宣称已具备其能力。

## 阅读入口

以项目根目录为基准，先按任务选择必要文档：

| 任务 | 读取 |
| --- | --- |
| 新增页面、按效果图改版 | [页面制作流程](../../../Assets/AAA_DevAssets/Docs/Framework/UI/PageWorkflow.md) |
| 查已有模板和 Widget | [复用目录](../../../Assets/AAA_DevAssets/Docs/Framework/UI/ReuseCatalog.md)，随后核对真实资产及使用方 |
| 制作结构、滚动区域或条目 | [Prefab 规范](../../../Assets/AAA_DevAssets/Docs/Framework/UI/PrefabRules.md) |
| 打开关闭、绑定事件、异步和清理 | [UI 手册](../../../Assets/AAA_DevAssets/Docs/Framework/UI/README.md) |
| 文案与语言图片 | [本地化](../../../Assets/AAA_DevAssets/Docs/Framework/Localization/README.md) |
| 字体或文字样式 | [TMPFont](../../../Assets/AAA_DevAssets/Docs/Framework/TMPFont/README.md)；缺字与图集操作再读 [FontAuthoring](../../../Assets/AAA_DevAssets/Docs/Framework/TMPFont/FontAuthoring.md) |
| 验证和交付 | [UI 验收](../../../Assets/AAA_DevAssets/Docs/Framework/UI/Validation.md) |

路径相对于本 Skill；所属活动/子游戏的 README 或 Docs 提供业务规则和字段契约。具体案例、历史报告、待实现设计不能替代当前模块说明。

## 先确定要改什么

1. 读取用户已经明确的范围：所属业务、目标页面、效果图、打开时机、交互与交付要求。
2. 查找现有页面、注册记录、Prefab 和调用方。“添加结算页”不自动等于新增 UIForm。若存在实质选择，带着具体候选确认修改已有页或新建；用户已明确时直接遵循。
3. 从图片区分固定区、滚动区、重复项、弹层与交互状态；无法从静态图确定的行为明确列出，不凭外观发明购买、领奖或结算规则。
4. 查询可复用元素，说明直接复用、变体或新建的依据；修改共享源 Prefab 前核对受影响使用方。已有业务页面不自动视作通用模板。
5. 对尚未明确的关键交互、素材或业务接口提出简短问题，同时继续不依赖答案的调查。能从项目确认的事实由 Agent 自行查证；局部绑定或样式修复不强制执行整套方案确认。

## 制作和接入

- 方案用简短结构清单说明页面区域、滚动行为、复用元素、必要素材、数据输入、动作回调及文件归属。方案规模随任务而定。
- 使用当前可用的 Unity Editor 能力，并遵守对应工具协议。保留既有 GUID、Prefab/Variant 关系和用户修改；不以手写 YAML 代替常规绑定。
- 沿用 UGuiForm、所属业务上下文和现有组件；只在确实新增/变更独立页面时同步注册。嵌入式 Widget 不新增 UIForm ID。
- 页面负责显示和提交交互请求。数据计算、发奖、购买、任务推进归所属业务；缺接口时指出缺少的契约，不创建临时 Manager 或虚构成功回调。
- 文案与字体按各自手册接入；更新字体时范围来自用户请求，不来自上次窗口勾选。占位素材/数据应明确标注。

## 验证和结束

按本次实际变化选择 UI 验收项：编译、保存后引用读回、真实入口、交互、重开与清理、适配及视觉对照。业务功能和视觉分别报告；静态检查、已有测试或预览不代替运行验收。

结束时交代修改/新增了哪个页面、复用了什么、注册是否变化、已验证及未验证内容。工具不可用时保留已完成的分析与文件，说明准确限制，不声称页面已运行通过。
