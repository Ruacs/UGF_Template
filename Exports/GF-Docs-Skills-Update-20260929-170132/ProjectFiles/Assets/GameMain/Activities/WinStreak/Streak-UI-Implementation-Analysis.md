# Hexa Streak UI 与实现分析

## hook工具地址:
D:\AAA_AgentProject\APK分析\hexa_hooks

## 范围与证据

分析对象为 `jp.co.goodroid.hyper.hexaaway` 的 Hexa Streak，代码模块为 `OutGame.WinStreak`。结论来自：

- 真机运行时导出的 Win Streak 类、字段和方法。
- `data.unity3d` 中的首页入口、开始页、主页面、详情页、结束页和塔楼组件层级。
- Event 1/2/3 的运行时活动配置、玩家 DTO。
- 真机截图：当前设备在普通关 2000 附近展示 Event 2 的短连胜页面。

## 活动数据

### 活动选择与周期

| 项目 | 结论 |
|---|---|
| 解锁 | 普通关 `40`；调用 `WinStreakEventService.IsUnlock(stageLevel)` |
| Event 1 | 普通关 `0-200` 使用，12 个检查点，最高 100 连胜 |
| Event 2/3 | 普通关 `201+` 使用，8 个检查点，最高 70 连胜；两套奖励内容相同，用于不同活动时间窗口 |
| 周期 | 每周三个窗口：周一 00:00-周三 07:00、周三 08:00-周五 15:00、周五 16:00-周日 23:00 |
| 连胜定义 | 每完成一关增加一次；失败调用 `ResetProgress` 或进入预约重置流程 |
| 领取 | 达到检查点后由 `CanReceiveReward` 判定，`TryReceiveReward()` 返回奖励结果 |

### 检查点配置

| Event | 连胜检查点 | 最高值 |
|---|---|---:|
| 1 | `2, 5, 8, 12, 17, 23, 30, 37, 45, 60, 80, 100` | 100 |
| 2 | `2, 5, 8, 16, 24, 36, 48, 70` | 70 |
| 3 | `2, 5, 8, 16, 24, 36, 48, 70` | 70 |

### Event 2/3 奖励表

| 连胜数 | `RewardId` | 宝箱等级 | 已解析奖励 |
|---:|---:|---:|---|
| 2 | 5001 | 0 | 金币 300 |
| 5 | 5002 | 0 | 无限体力 15 分钟 |
| 8 | 5003 | 0 | Hammer x1 |
| 16 | 5005 | 1 | 金币 600 + 无限体力 15 分钟 |
| 24 | 5006 | 1 | 金币 600 + Drill x1 |
| 36 | 5007 | 2 | Hammer x1 + Drill x1 |
| 48 | 5010 | 2 | 金币 600 + Hammer x1 + Drill x1 |
| 70 | 5012 | 3 | 金币 3000 + Bomb x1 + Hammer x1 + Drill x1 + 无限体力 60 分钟 |

Event 1 使用另一组奖励 ID：`5201-5212`，其检查点更密集，具体奖励内容仍通过通用奖励表按 `RewardId` 解析。

### 玩家状态 DTO

`Shared.Database.Player.PlayerWinStreakEventDTO` 保存活动状态：

| 字段 | 用途 |
|---|---|
| `EventId` | 当前使用的活动配置，当前运行时为 `2` |
| `CurrentProgress` | 当前连胜数 |
| `MaxCheckPointIndex` | 已达到的最高检查点索引 |
| `BestProgress` | 历史最佳连胜数 |
| `RewardReceiveProgress` | 已领取到的检查点进度 |
| `JoinCount` | 加入/参与次数 |
| `LastCheckedProgress` | 上一次 UI 检查并播放动画时的连胜值 |
| `IsReserveReset` | 失败后是否等待下一次流程执行重置 |
| `IsShowStart` | 开始页是否已经展示 |
| `IsCompleteTutorial` | 活动规则引导是否完成 |

另有 `PlayerWinStreakDTO` 保存基础的 `CurrentCount` 和 `BestCount`；Event 页面主要使用上面的 `PlayerWinStreakEventDTO`。

## 页面清单

| 页面/表面 | 资源根节点 | 主要职责 | 关键类/方法 |
|---|---|---|---|
| 首页入口 | `IconBtn_WinStreak` | 显示连胜数字、锁定状态、活动倒计时、通知和入口动画 | `WinStreakButtonView.Init(model)` |
| 首次开始页 | `WinStreakStartPopup` | 展示活动标题、三段式小进度预览、规则说明、倒计时和开始按钮 | `WinStreakEventVM.ShowStartPopup`、`WinStreakStartPopup.Init(endTime)` |
| 连胜主页面 | `WinStreakPopup` | 展示塔楼、检查点、宝箱、玩家气球、当前进度和倒计时 | `Init(data, isFirstShow)`、`CreateContents`、`SetEvent` |
| 规则详情页 | `WinStreakDetailPopup` | 三张规则图：不失败通关、保持连胜、领取奖励 | `WinStreakEventVM.ShowDetailPopup` |
| 结束页 | `WinStreakEndPopup` | 活动结束提示、倒计时文案和下一步按钮 | `ShowEndPopup`、`WinStreakEndPopup.Init()` |
| 检查点奖励单元 | `TowerView` / `RewardContentView` | 显示检查点数字、塔段进度、宝箱状态、奖励图标和领取按钮 | `TowerView.UpdateProgress`、`RewardContentView.DoReceiveAnim` |

## 页面流程

```text
WinStreakEventService.IsUnlock(stage)
  -> WinStreakEventModel.Init(eventState)
  -> WinStreakEventVM.SetEvent
  -> WinStreakEventVM.Show()
     -> 首次：ShowStartPopup -> Join -> ShowMainPopup
     -> 已加入：ShowMainPopup
        -> DetailBtn -> ShowDetailPopup

通关成功
  -> WinStreakEventService.AddProgress(normalStage, calcStage, count)
  -> 检查是否跨过 checkpoint
  -> CanReceiveReward / TryReceiveReward
  -> WinStreakPopup.PlayProgressAnimation(prev, current, callback)
     -> AnimateGauges
     -> AnimateCamera
     -> AnimateCharacter
  -> TowerView / ChestView / RewardContentView 更新

通关失败
  -> ResetProgress 或 ReserveResetProgress
  -> ResetProgressIfReserve
  -> 更新 CurrentProgress / LastCheckedProgress
```

`WinStreakEventService` 的核心接口已经完整暴露：`AddProgress`、`ResetProgress`、`ReserveResetProgress`、`ResetProgressIfReserve`、`GetNextProgress`、`GetPrevProgress`、`HasLoseProgress`。但 `WinStreakResetParam.ResetToStreakCount` 是运行时计算参数，当前主配置没有直接给出固定回退表，因此失败后回退到哪个检查点仍需要真实失败局 Trace。

## 预制体结构

### 首页入口 `IconBtn_WinStreak`

```text
IconBtn_WinStreak
|- Flame_Out / Flame_In
|- IconBtnAnim
|  `- root
|     |- balloon -> balloon_top / balloon_center / balloon_bottom
|     |- streak_cloud / streak_cloud_1
|     |- bird_all -> bird_left / bird_right
|     `- streak_all
|- WinStreak_icon -> hexa_part -> Text (TMP)_1
|- IconBtnLock
|- TextLabel -> TextLabelText (TMP)
|- RemainTimeLabel -> time / Text (TMP)
|- UnlockText / lock / NotiIcon
```

入口的连胜数字由 `_winStreakCount` 绑定。`IconBtnAnim` 使用 Animator 和气球、鸟、云、连胜标志组合成入口动效。

### 主页面 `WinStreakPopup`

```text
WinStreakPopup
|- Overlay
|- Bg
|  |- Scroll -> Viewport -> Content
|  |  |- Sky
|  |  |- TowerBackground
|  |  |- Tower
|  |  `- Character
|  |     |- Balloon -> balloon_top_fire / balloon_bottom / balloon_top_himo / balloon_top
|  |     |  `- Profle -> Frame / Icon
|  |     `- Arrow
|  `- TopArea -> HomeBtn / DetailBtn / Title -> Timer / Logo
|- Footer
`- InputBlock
```

主页面不是固定 19 项列表，而是由 `WinStreakPopup.CreateContents()` 根据当前 Event 的检查点动态创建塔段。主页面将塔楼拆成 `TowerBottom`、`TowerMiddle`、`TowerTop` 三段，并让相机、背景、进度条和玩家气球一起滚动。

### 塔段与奖励

```text
TowerView
|- CheckPointView -> levelObject / achievementObject / levelText
|- ProgressGaugeView -> fillImage / cameraRoot
`- RewardContentView
   |- ChestView -> closedRoot / openedRoot
   |- ClaimBtn
   |- ItemThumbRoot -> RewardThumbView[]
   `- ItemGridLayout
```

`ContentShapeType` 有 `Bottom`、`Middle`、`Top`，用于决定塔段的背景、进度条高度以及上下连接方式。`ProgressGaugeView` 以 `minLevel`/`maxLevel` 把连胜数映射到塔内相对位置，`TowerView.CalculateRelativeProgress` 决定当前塔段填充。

### GF Template 的具体塔段预制体（2026-09 更新）

本项目的 `WinStreakMainUIPanel` 不再把一张跨越全部 `Content` 的塔图和总进度条叠在动态奖励行上。`CheckpointContent` 的直接布局项固定为塔顶、可克隆中段模板和塔底；运行时克隆的中段插入模板与塔底之间：

```text
CheckpointContent (VerticalLayoutGroup, ContentSizeFitter)
|- TowerTop                    # 终点奖励；无向上的进度段
|  |- Stage / ClaimedState / CheckpointBadge -> Checkpoint / Chest
|- CheckpointRowTemplate       # 每个中段的固定高度
|  |- TowerMain / Stage / ProgressTrack -> Fill
|  |- ClaimedState / CheckpointBadge -> Checkpoint / Chest
|  `- RewardTooltipView (Canvas, RewardTooltipView) -> BG_1 / BG_2 / Box
`- TowerBottom                 # 起点；承载第一个进度段
   |- ProgressTrack -> Fill
   `- ClaimedState / CheckpointBadge -> Checkpoint
```

活动数据仍是累计连胜，但每个视觉进度条用自己的端点计算：塔底为 `firstCheckpoint - 1 -> firstCheckpoint`，中段在检查点 `C` 显示奖励并负责 `C -> nextCheckpoint`，塔顶显示最终奖励且没有额外进度条。统一公式为 `Clamp01((current - minLevel) / (maxLevel - minLevel))`；因此 `1->2` 只需 1 级填满，`2->5` 需 3 级，`8->12` 需 4 级，而所有轨道的视觉高度保持固定。这个结构同时避免不同检查点的塔图、奖励和 Fill 跨行叠层。

### 开始页与详情页

```text
WinStreakStartPopup
`- Bg -> Body
   |- HeaderImg
   |- ProgressGauge -> Level_1 / Level_2 / Level_3 / Box
   |- Timer -> RemainTimeLabel
   |- Title / Description
   `- BtnArea -> StartBtn / StartBtnText / CloseBtn

WinStreakDetailPopup
`- Bg -> Title
   |- Content_01 -> Peace01 / Text
   |- Content_02 -> Peace02 / Text
   |- Content_03 -> Peace01_2 / Text
   |- Arrow_01 / Arrow_02
   `- CloseBtn
```

### 结束页

```text
WinStreakEndPopup
`- Bg -> Body
   |- HeaderImg
   |- Timer -> RemainTimeLabel
   |- Title / Description
   `- BtnArea -> NextBtn / NextBtnText
```

## 动画与刷新实现

| 场景 | 实现接口 | 说明 |
|---|---|---|
| 进度条填充 | `ProgressGaugeView.SetProgress(progress, animation)` | DOTween 控制 `RectTransform` 填充位置，支持无动画和动画两种模式 |
| 跨检查点 | `WinStreakPopup.PlayProgressAnimation(prev, current, callback)` | 同时编排塔段进度、相机滚动和角色气球移动 |
| 角色移动 | `AnimateCharacter`、`BalloonView.PlayIdleAnim/PlayFallAnim` | 连胜推进时气球上升，切换塔段时播放气球动画 |
| 宝箱领取 | `RewardContentView.DoReceiveAnim` | 宝箱打开后逐个弹出奖励图标，使用固定延迟、间隔、位移、缩放和淡入参数 |
| 检查点状态 | `CheckPointView.SetAchieved`、`ChestView.SetOpen` | 检查点数字与宝箱开关分离，便于表现已达成但未领取的状态 |
| 页面滚动 | `ScrollRect` + `CalculateCameraPositionFromProgress` | 用户可手动滚动，自动进度动画也会把镜头移动到目标检查点 |

规则详情页的真机截图显示三段文案为：`Beat Levels without failing!`、`Maintain a streak!`、`Claim Rewards!`，底部为 `TAP TO CONTINUE`。

## 真机截图

| 截图 | 内容 |
|---|---|
| [首页入口](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/screenshots/win_streak_home_before_open.png) | 首页顶部可见 Streak 入口，左下角显示连胜数字入口 |
| [主页面](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/screenshots/win_streak_main_open.png) | Event 2 主页面，塔楼、检查点 16/8、角色气球和宝箱 |
| [规则详情](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/screenshots/win_streak_detail.png) | 三段活动规则说明 |
| [中段滚动](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/screenshots/win_streak_mid_scroll.png) | 检查点 5/2 与奖励宝箱 |
| [底部滚动](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/screenshots/win_streak_end_scroll.png) | 低检查点、绿色完成标记和塔楼底部场景 |

## 图片素材

| 分组 | 素材 |
|---|---|
| 活动图集 | `sactx-0-2048x2048-ETC2-WinStreak-e0b6b7fe` |
| 首页图集 | `sactx-0-1024x512-ETC2-WinStreak_IconBtn-96ea4df9` |
| 塔楼/连胜 | `streak_pop`、`streak_end_pop`、`streak_all`、`streak_cloud` |
| 规则页 | `streak_step01`、`streak_step02`、`streak_step03` |
| 活动结束 | `stop_event_win_streak` |
| 宝箱/货币 | `coin_tower`、`coin_tower_default` |

素材总览见 [win_streak_material_contact_sheet.png](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/win_streak_material_contact_sheet.png)，完整尺寸和文件索引见 [win_streak_asset_inventory.json](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/win_streak_asset_inventory.json)。

## 导出物

- [预制体层级 JSON](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/win_streak_prefab_hierarchy.json)
- [预制体层级文本](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/win_streak_prefab_hierarchy.txt)
- [运行时 UI 类、字段与方法](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/win_streak_ui_classes.json)
- [素材目录](../hexa_hooks/activity_runtime_export/hexa_win_streak_ui/images/)
- [预制体与素材导出脚本](../hexa_hooks/dump_win_streak_prefab_assets.py)
- [运行时类导出脚本](../hexa_hooks/inspect_win_streak_ui_classes.js)

## 待补验证

- 失败后 `ResetToStreakCount` 的实际回退点。
- Event 1/2/3 具体周期与 EventId 选择的精确时间边界。
- `TryReceiveReward()` 是通关结算自动调用，还是在用户点击宝箱/领取按钮时调用。
- Event 1 的 5201-5212 奖励 ID 具体内容。
- 结束页出现的时机，以及未领取奖励在周期结束后的处理。

## GF Template 落地状态（2026-09）

- 已按 Event 2 安装 `2/5/8/16/24/36/48/70` 八个累计检查点及 `5001/5002/5003/5005/5006/5007/5010/5012` 奖励。
- 运行模块为 `win_streak`，接收 Catalog 声明的 `Game` 通关事实；重复事实和其他游戏来源会被忽略。
- 当前连续胜场跨检查点继续累加，不会因为到达检查点清零；失败会清空当前连续胜场，但保留历史最佳、已达成检查点与已领取奖励。
- 页面路由为 `start/main/details/end`，对应 `WinStreakStartUIPanel`、`WinStreakMainUIPanel`、`WinStreakDetailUIPanel`、`WinStreakEndUIPanel`；主页面的每个塔段 Fill 都使用纯色 UGUI Image，并按本段端点计算局部进度。
- 奖励通过公共 `IActivityRewardGateway` 发放并保存幂等回执；领取入口为检查点宝箱按钮。
- 编辑器/开发包提供 Win Streak Test Mode，可报名、批量模拟胜场、模拟失败和重置数据。
- 当前配置只安装 Event 2。Event 1 奖励内容、Event 3 独立时间窗以及线上服务端周期选择仍需产品数据后再扩展。


