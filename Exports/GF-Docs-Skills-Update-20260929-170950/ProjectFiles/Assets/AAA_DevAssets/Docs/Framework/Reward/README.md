# Reward 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. 奖励配置与 Widget

**功能分工**：RewardDefinitionSO / RewardEntry 描述奖励和单位，RewardDataSO 组织奖励；RewardPresentation 生成图标/数值展示数据；RewardItemView、RewardSlotView、RewardChestView、RewardTooltipView 等承担显示与交互。

**新增/接入**：配置 Definition 和合法数量/发放模式，加入所属业务的奖励配置；在页面 Prefab 中绑定所需 Widget 和资源。普通奖励 Widget 不注册独立 UIForm；只有独立窗口才走页面注册链。

**使用**：`RewardPresentation.Build(bundle)` / `BuildItem(entry)` / `FormatValue(entry, multiplier)` 只转换显示，不修改奖励清单、不发奖、不启动无限道具计时。业务先通过所属任务/商店/活动奖励入口确认结果，再决定播放展示。活动奖励资源 Key、回执与当前支持范围见 [活动接入 §7](../Activity/Integration.md#7-各活动自己的业务服务与奖励)。

**验收**：普通数量/时长单位、倍数、缺图、空列表、提示框关闭、重复点击，以及实际到账与表现次数一致。金额/次数/秒数不能只根据 UI 文本混用。

依据：[RewardPresentation](../../../../GameMain/Scripts/UI/Widget/Reward/RewardPresentation.cs)、[RewardEntry](../../../../GameMain/Scripts/Config/Reward/RewardEntry.cs)、[奖励 UI 草案](../../Archive/Implementation/RewardUI-20260916.md)（仅设计参考）、[通行证具体绑定](../../../../GameMain/Activities/SeasonPass/Docs/UIPrefabContract.md)。

## 当前配置和显示入口

`RewardDefinitionSO` 表示资源身份；`RewardEntry` 保存资源引用、发放方式和数值；`RewardDataSO` 保存有序奖励清单及宝箱样式。`AddQuantity` 的 Amount 为数量，`UnlimitedUse` 为秒；是否可领取由实际接收器决定。

创建入口为 `Assets/Create/GF/Common/Reward/Resource`、`Bundle`、`Chest Style`。奖励包配置 Entries，多项奖励必须指定有图标的 ChestStyle；`ValidateEntries()` 检查清单与样式。配置归公共或所属业务资源域，页面通过已加载配置引用使用。

`RewardSlotView.Bind(bundle)`：空包隐藏；单项显示 Item；多项显示 Chest，重新绑定先关闭旧预览。`RewardTooltipView` 的 Show 用于可关闭预览，ShowPersistent 用于页面内常驻清单。使用前核对对应组件字段和加载依赖。

真实 Prefab 与组件入口见 [UI 复用目录](../UI/ReuseCatalog.md)。[通行证配置](../../../../GameMain/Activities/SeasonPass/Docs/README.md)属于具体活动；[后续可靠发奖与权益设计](../../Design/RewardUI.md)与当前显示能力分开维护。旧迁移清单见[历史记录](../../Archive/Implementation/RewardUI-20260916.md)。
