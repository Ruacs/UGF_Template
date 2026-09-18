# Collector 活动

本目录提供 Collector 的 UI、活动页面路由、可编辑的任务/奖励配置，以及基于目标游戏通关事实的进度、存档和领奖流程。

## 当前资产

- `UI/CollectorHomeWidget.prefab`：首页当前收集进度摘要卡片，包含进度、计时器和两个
  `RewardSlotView` 交替显示槽位。`CollectorHomeWidget.Bind`/`SetRewards` 负责刷新数据；
  进度推进时旧槽位上移淡出，新槽位从 0 缩放到预制体比例。两个槽位的 `CanvasGroup`、
  `RectTransform` 和 `RewardSlotView` 均由 Prefab Inspector 显式绑定。`CollectBox` 点击
  通过 `MainPanelRequested` 交给 `MainUIPanel` 打开 Collector 主页面。
- `UI/CollectorMainPanel.prefab`：Collector 主页面的 UIForm 空壳。
- `UI/CollectorTaskRowView.prefab`：主页面任务行模板空壳。
- `ScriptableObjects/Config/HexaCollector202609.asset`：活动时间、解锁条件和 19 个 Collector 档位。
  每档的 `RequiredCount` 是该档自己的目标（50/50/50/50/50、100/100、150/150/150、
  200/200/200、400/400/400、800/800/800），不是跨档累计阈值。
- `ScriptableObjects/Rewards/HexaCollector202609/`：19 个奖励包，按 Collector 奖励表配置金币、
  Extra Move、Drill、无限体力、Bomb 和 Hammer；多奖励档位使用普通宝箱样式。奖励包使用公共
  `RewardEntry`/`RewardDefinitionSO`，图标从现有奖励资源复制引用，后续可在 Inspector 中替换数值或资源。
- `Scripts/CollectorActivityModule.cs` 与 `Scripts/CollectorActivityDefinition.cs`：活动模块消费定义中配置的
  `GameIds`（当前为 `Game`）对应的 `ActivityLevelCompletedFact`。每个胜利事实按配置的
  `CollectionPerWin` 增加当前档收集数；达成档位后索引前进且当前收集数清零，不把溢出数量带入下一档。
  使用 `FactId` 去重，并保存当前档索引、当前档收集数、已领奖档位和已处理事实。
- `Scripts/TestMode/CollectorTestModeModule.cs`：编辑器/Development Build 下注册到 Test Mode，提供批量模拟通关和重置活动数据，
  用于验证首页 Widget 和主页面任务列表刷新。批量模拟在模块内合并为一次保存/通知，避免连续重启动画。

Prefab 的视觉层级、图片、字体、布局、动画和奖励卡内容由设计师维护。`CollectorTaskRowView`
提供纯 UI 映射：`SetTier`、`SetReward`、`SetState` 和 `SetListPosition`；其中
`SetListPosition(rowIndex, rowCount)` 使用 0-based 行号，首行隐藏 `Line_1`，末行隐藏
`Line_2`，单行时两条连接线都隐藏。行组件和主页面的引用均由 Prefab Inspector 显式绑定，
运行时不通过名称或层级路径查找组件。

## 当前注册

运行 Unity 菜单 `Tools/Activity/Collector/Install UI Route` 可重复同步：

- `ActivityModuleCatalog` 中的 Collector Definition；
- `CollectorMainPanel` 的 Activity 页面映射；
- `UIForm.txt`、`UIForm.bytes` 和 `UIFormId.cs` 的 222 号页面注册；
- Collector 配置、资源定义和 19 个里程碑奖励包（旧版 4 档配置会自动迁移到当前 19 档表）。

该安装会注册页面路由并生成任务/奖励配置；当前默认本地奖励接收器会记录领取并返回成功，
不会直接修改金币、道具或限时权益。接入正式经济系统时，将 `IActivityRewardReceiver` 替换为
对应的游戏数据适配器即可。

主页面打开时会根据配置任务数量动态实例化 `CollectorTaskRowView`，按高档位到低档位展示；首页 Widget 订阅模块的
`StateChanged`，通关事实到达后会刷新进度、奖励和倒计时。
