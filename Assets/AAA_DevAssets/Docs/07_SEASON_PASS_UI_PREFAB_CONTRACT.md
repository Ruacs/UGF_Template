# 通行证 UI Prefab 接口

更新：2026-09-16。

通行证的业务、页面路由和数据绑定已经完成；页面视觉 Prefab 由设计师维护。安装工具不会创建、覆盖或修改这些 Prefab。

## 文件位置与页面 ID

| 页面 | 必须创建的 Prefab | 根组件 | UIForm ID |
| --- | --- | --- | --- |
| 主页 | `Assets/GameMain/Activities/SeasonPass/UI/SeasonPassMainPanel.prefab` | `SeasonPassMainPanel` | 216 |
| 规则页 | `Assets/GameMain/Activities/SeasonPass/UI/SeasonPassRulesPanel.prefab` | `SeasonPassRulesPanel` | 217 |
| Gold Pass购买页 | `Assets/GameMain/Activities/SeasonPass/UI/SeasonPassGoldPassPurchasePanel.prefab` | `SeasonPassGoldPassPurchasePanel` | 218 |

三个 Prefab 都属于 `Activities/SeasonPass/UI/`，不要放入 `Assets/GameMain/UI/`。根节点使用现有 UI Panel 模板或等价的 `RectTransform` 页面根节点；脚本继承 `UGuiForm`，会在运行时补齐所需的 Canvas 组件。

`UIForm.txt`、`UIForm.bytes` 与 `UIFormId.cs` 已登记这三个 ID。不要手改生成的 `.bytes` 或 `UIFormId.cs`；完成 Prefab 后运行 **Tools/Activity/Season Pass/Install Config And Validate UI**，它只同步既有 UIForm 生成物并检查绑定，不会编辑 Prefab。

## 主页绑定

在根节点的 `SeasonPassMainPanel` Inspector 绑定：

| 字段 | 用途 |
| --- | --- |
| `m_Title` | 活动名，例如“开学通行证” |
| `m_Countdown` | 活动结束倒计时 |
| `m_Progress` | 当前目标 Tier 的局部充能，显示为“本档已获得 / 本档所需”。配置的 `RequiredCharge` 是累计阈值，运行时会扣除上一个不同阈值，不显示整条通行证的 `当前 / 70`。 |
| `m_Status` | 开启条件、领取失败或服务端待接入说明 |
| `m_ProgressFill` | 当前目标 Tier 的局部进度条；`Image.Type = Filled` 时写 `fillAmount`，否则更新横向锚点 |
| `m_RulesButton` | 打开规则页 |
| `m_CloseButton` | 关闭主页 |
| `m_TierContent` | 25 档奖励行的内容容器，通常是 `ScrollRect.content` |
| `m_TierRowTemplate` | 内容容器中的**禁用**行模板，带 `SeasonPassTierRowView` |
| `m_TextFont` | 可选的页面字体；中文页面应绑定覆盖所需汉字的 TMP Font Asset |

`m_TierRowTemplate` 在首次打开时按配置档位数量克隆。当前配置有 25 档，但代码按配置数量生成，不依赖固定的 25 行布局。

## 奖励行模板绑定

在禁用模板节点上挂 `SeasonPassTierRowView`，并绑定：

| 字段 | 用途 |
| --- | --- |
| `m_TierText` | MilestoneLane 中的纯数字档位号 |
| `m_RequiredChargeText` | 可选的所需充能文本；当前 Prefab 没有对应节点，可留空 |
| `m_ProgressFill` | MilestoneLane 的 Fill_Progress；它是奖励列表的全局时间线，接收总充能 / 最后一档累计阈值。例如总共需 70 充能、当前为 20 时，每一行均显示 20 / 70 的填充。顶部 `m_ProgressFill` 则显示当前目标 Tier 的局部进度。 |
| `m_FreeLane` | FreeLane / RewardCard 的奖励区域绑定 |
| `m_PremiumLane` | PremiumLane 的奖励区域绑定 |

两条轨道复用同一个序列化绑定结构：

| 轨道内字段 | 用途 |
| --- | --- |
| `m_RewardContentRoot` | 奖励内容容器；无奖励时隐藏 |
| `m_RewardSlot` | 容器中的 `RewardSlotView`；按奖励包自动显示单项或宝箱 |
| `m_RewardsText` | 可选的奖励清单文本；数量/无限时长按统一格式显示 |
| `m_RewardButton` | 保留旧卡片引用，卡片本身不再触发领取 |
| `m_ClaimButton` | 独立领取按钮；无奖励或已领取时隐藏 |
| `m_ClaimButtonText` | 按钮状态：领取、已领取、未解锁或待接入 |
| `m_LockedMask` | 未达到充能门槛的遮罩；高级轨道未接入领取时也保持锁定 |
| `m_ClaimedMask` | 已领取遮罩 |

免费轨道只有独立领取按钮使用 `Bind` 传入的领取回调。宝箱内部按钮只打开奖励预览；锁定或已领取也可预览，遮罩的 Graphic 不拦截射线。高级轨道尚无购买状态和领取能力，领取按钮保持不可交互；不得用免费轨道回调代领。空奖励、未解锁、待接入、可领取、已领取五种显示状态统一处理，每次 Bind 更新奖励内容、遮罩和按钮。

模板可自行使用 `VerticalLayoutGroup`、`ContentSizeFitter`、遮罩和动效。`RewardSlotView.prefab` 内部复用 `RewardItemView_Base` 与 `RewardChestView_Base`，绑定时切换显隐和数据。当前奖励行两条轨道均已绑定，主页面的 `m_TierContent / m_TierRowTemplate` 已关联到现有滚动内容和行模板；其他页面字段仍按设计师 Prefab 的实际绑定维护。

## 奖励配置

`SeasonPassActivityConfig.Tiers` 每档通过 `Free Reward / Premium Reward` 引用 `SeasonPassRewardDefinition` SO，后者继承通用 `RewardDataSO`。奖励包填写资源引用、发放方式、数量/秒数和宝箱品质样式。一项使用单项视图，多项使用宝箱；品质由 `RewardChestStyleSO` 的等级和图片定义，不能根据条目数推算。

示例包：`Activities/SeasonPass/ScriptableObjects/Rewards/SchoolPass202609/Tier_05_Premium.asset`。品质资源：`GameMain/ScriptableObjects/Reward/Chests/`。数量和无限奖励可以引用同一资源 SO；当前限时权益仅支持配置/预览，实际发放仍保持不可领取。

安装工具在配置首次创建时写入示例；再次运行保留设计师修改，必要时将原内嵌奖励迁为 SO。25 档现有配置已迁移为 49 份包，第 1 档高级奖励为空。完整配置与迁移说明见 [通用奖励数据与展示](08_REWARD_UI_DESIGN_DRAFT.md)。

旧版扁平轨道字段已改为 `m_FreeLane` / `m_PremiumLane`。调整前检查过本项目两处奖励行组件，旧轨道引用均为空；以后引入携带旧字段的 Prefab 时，需要重新绑定，不存在自动迁移。

## 规则页与领奖页

- `SeasonPassRulesPanel`：绑定 `m_CloseButton`。规则标题、正文、图标和版式全部由 Prefab 提供。
- `SeasonPassGoldPassPurchasePanel`：Prefab 中保留 `Btn_Get` 与 `Btn_Close`。当前 `Btn_Get` 调用模拟购买成功；真实商店支付接入时替换模块的购买实现。

静态文案可沿用项目的 `UIStringKey` 本地化。`UGuiForm` 初始化会应用项目当前的全局 TMP 字体；若页面包含中文而全局字体缺字，请在页面脚本的 `m_TextFont` 字段绑定完整字体，脚本会在初始化后重新应用它。

## 验证

1. 创建并保存三个 Prefab 到上表的精确路径。
2. 在根节点添加对应页面脚本，完成 Inspector 引用。
3. 运行 **Tools/Activity/Season Pass/Install Config And Validate UI**。缺少 Prefab、错误组件或空引用会输出具体警告。
4. 进入主菜单，点击 `PASS` 入口；检查主页、规则页、领取结果页。
5. 验证第 1 档金币奖励只能领取一次；未映射的 `extra_move`、`bomb`、`hammer`、`drill`、`unlimited_life` 保持“待接入”状态，不能伪装成已发放。
