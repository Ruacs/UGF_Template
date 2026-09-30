# UI 复用目录

[UI 手册](README.md) · [页面制作流程](PageWorkflow.md)

以下路径已与当前工作区核对；本目录不是 Prefab 运行验收报告。复用前检查真实组件、必填引用、依赖和现有使用方，判断直接复用、变体或仅作参考。修改共享源 Prefab 前先确认影响范围。

## 页面模板

| 入口 | 用途 | 使用限制 |
| --- | --- | --- |
| [UIPanel_Template](../../../../GameMain/UI/Template/UIPanel_Template.prefab) | 新建 GF 页面结构起点 | [UI Panel Manager](../../../../GameMain/Editor/UIFormPanelGeneratorWindow.cs)当前复制此模板；仍需正确根逻辑、注册与字段绑定 |
| [PopupUIPanel_Template](../../../../GameMain/UI/Template/PopupUIPanel_Template.prefab) | 弹窗结构候选 | 当前生成器不自动切换到此模板；核对模态遮挡和 Canvas 设置后使用 |
| [公共页面目录](../../../../GameMain/UI/UIPanel/) | 查找已有设置、语言、奖励、结算页面 | 已有页面含业务逻辑，不能只因外形相似就直接复制或替换；先查调用方和数据协议 |

## 奖励 Widget

| Prefab / 组件 | 输入和配置 | 边界 |
| --- | --- | --- |
| [RewardItemView_Base](../../../../GameMain/UI/UIPrefabs/Reward/RewardItemView_Base.prefab)、[默认变体](../../../../GameMain/UI/UIPrefabs/Reward/RewardItemView.prefab) | RewardItemViewData；图标、文本和可选动效引用 | 单项显示；保留基底/变体关系；动效背景可能为空 |
| [RewardSlotView](../../../../GameMain/UI/UIPrefabs/Reward/RewardSlotView.prefab) | Bind(RewardDataSO)，绑定 ItemView / ChestView | 空/单/多项切换；多项需宝箱外观；不执行领取 |
| [RewardChestView_Base](../../../../GameMain/UI/UIPrefabs/Reward/RewardChestView_Base.prefab) | 宝箱图标、奖励展示数据、提示框引用 | 点击预览不代表发奖；不要替代带玩法进度的业务宝箱 |
| [RewardTooltipView](../../../../GameMain/UI/UIPrefabs/Reward/RewardTooltipView.prefab) | 条目模板、容器、清单及可选锚点 | 预览与常驻清单调用不同；核对关闭、裁切和条目重绑 |

具体绑定实现查 [Reward 脚本目录](../../../../GameMain/Scripts/UI/Widget/Reward/)，数据职责查 [Reward 手册](../Reward/README.md)。

## 其他可复用能力

- [ButtonClickAnim](../../../../GameMain/Scripts/UI/Widget/Button/ButtonClickAnim.cs)：指针按压/释放与缩放效果。它不处理业务按钮行为；不要重复叠加已有点击动画。
- [UIItemBase](../../../../GameMain/Scripts/UI/Runtime/UIItemBase.cs)：初始化静态文本/图片本地化的条目基类；动态数据绑定和回收时重置由条目实现。
- [UGuiForm](../../../../GameMain/Scripts/UI/Runtime/UGuiForm.cs)：页面生命周期与 Item 管理；嵌入式 Widget 不因复用而增加 UIForm ID。
- 滚动区域使用实际页面的 ScrollRect / Viewport / Content，并按[制作规范](PrefabRules.md)检查布局。目前未在本目录承诺一个能覆盖所有业务的通用列表 Prefab；大量数据是否需虚拟化需另行核对实现。
- 字体样式查 [TMPFont](../TMPFont/README.md)，静态/动态文案查 [Localization](../Localization/README.md)，飞行和飘字查 [UIEffects](../UIEffects/README.md)。

## 活动和玩法私有组件

[SeasonPass 契约](../../../../GameMain/Activities/SeasonPass/Docs/UIPrefabContract.md)中的 TierRow、BonusBank 等依赖通行证业务，只能作为该模块组件或结构参考。其他活动/子游戏的页面和私有组件也按此边界处理；跨模块复用前先核对并显式抽取通用职责。

新增目录条目时记录：真实资产/源码路径、输入、必要引用、生命周期、复用限制、最近核对范围。以资源本身保存字段细节，避免在目录中维护第二份 Inspector 清单。
