# Mining / Gem Adventure

本目录的首个可运行闭环是：点击格子消耗镐子，完整揭开宝石占用的全部格子后收集宝石，
再将宝石飞到 `Gate/Back/GemSlots` 中由配置指定的位置。

## 数据边界

- `MiningActivityConfig.asset` 仅作为配置入口，引用活动时间、宝石图片表、Gate 主题表、奖励表和各期关卡表，不再承载具体数据。
- `MiningScheduleConfig.asset` 单独保存当前 EventId、活动开始 UTC 与截止 UTC；页面与首页入口共用同一截止时间。
- `MiningGemVisualConfig.asset` 单独保存格子图片、14 类宝石的三态图片和占格尺寸。
- `Textures/related/MiningGateThemeConfig.asset` 单独保存 Step1–5 对应的 `Back` 与 `scene_BG` 图片。
- `Rewards/MiningRewardConfig.asset` 单独建立 RewardId 9001–9005 到 5 个奖励包的映射；Event01 与 Event02
  只保存 RewardId 并共享该表，不复制奖励条目。
- `Rewards/Chest/MiningRewardChest_1~5.asset` 保留五章各自的宝箱图片；奖励包直接引用对应样式。
- 奖励内容来自运行时导出：金币、Bomb/Hammer/Drill/ExtraMove、无限生命、联赛加成和 Mining 镐子；
  数量和时长保留原始值，其中限时奖励统一使用秒。
- `MiningEvent01Config.asset`、`MiningEvent02Config.asset` 分别保存各自 5 档关卡及 Gate 展示参数。
- `EventId / StepId / CellCount / RewardId / TreasureBoxType` 与 `GatePos / GateRotate / GateSize`
  来自运行时导出数据。
- 原导出没有 `GemId -> CellId` 映射。当前 `GridOrigin` 是依据 GatePos、宝石图片占格尺寸生成的
  确定性无重叠布局，属于本工程派生数据，不冒充 APK 原始位置。
- 若以后拿到真机格子映射，只调整配置中的 `GridOrigin / GridQuarterTurns`；挖掘状态机与飞行动画无需修改。

## Gate 衔接

- `MiningMainUIPanel` 的 `GateArea` 固定包含前层 `Gate` 与后层 `Gate_Next`。
- Step1–4 完成后，当前 Gate 先播放 `Gate_Exit`；经过 `MiningMainUIPanel` 上可配置的
  `Gate Entry Delay` 后，后层的下一主题 Gate 即开始播放 `Gate_Enter`，与前层退场重叠衔接。
- 两个 Gate 交替复用：退场 Gate 隐藏后立即切换为下下关主题、重建对应空宝石槽并放到后层，
  入场 Gate 同时成为新的当前 Gate；不存在下下关时保持隐藏。
- Step5 没有下一主题，只播放当前 Gate 退场。
- 页面初始化时会为本关每颗宝石创建 `GemSlots/GemSlot_{GemId}`，先显示全部 `gem_frame` 作为待填占位框；
  挖出宝石后只显示对应槽位中的 `Gem`。预载的下一 Gate 同样提前创建下一关的空框。

## 五关循环

- Step1–4 完成时从 `GridArea.prefab` 实例化下一关棋盘：新棋盘从 `GameArea` 上方滑入，
  当前棋盘同时向下滑出并在动画完成后销毁。
- 新关卡继承剩余镐子，重新绑定格子、宝石与下一 Gate，然后继续同一挖掘循环。
- Step5 完成后不再生成棋盘；当前棋盘下滑退出，并显示 `GameArea/CompletionPage`，阻挡游戏区交互，
  作为后续结算和奖励流程的承载层。

## 章节奖励进度与时间

- 页面顶部 5 个宝箱按当前 Event 各 Step 的 `RewardId -> ChestStyle` 自动绑定，不再使用 Prefab 中的占位宝箱图。
- `RewardSlider` 的 Handle 表示当前 Step：Step1 为 0%，Step3 为 50%，Step5 为 100%，并禁止玩家拖动。
- Step 文案由关卡序号生成；倒计时读取 `MiningScheduleConfig` 的截止 UTC，并优先使用活动系统时钟。

## 进度快照

- `MiningActivityModule` 使用活动存储的 `state` 记录当前 `EventId`、`StepId`、剩余镐子、已挖格子和已收集宝石。
- 棋盘每次变化及页面关闭时都会回传快照；从首页再次进入时使用最近快照重建 Gate、格子和宝石槽。
- 配置切换到另一 Event 时，旧 Event 快照会失效并从新 Event 的 Step1 开始，避免跨期串档。
- 若退出发生在换关动画中，已完成的 Step1–4 会规范化为下一关空棋盘，避免重新进入后停在一个不可操作的完成关卡。
- Step5 的完成快照会恢复完成遮罩，不会重新从 Step1 开始。

## 接入接口

打开页面时向 `MiningMainUIPanel` 传 `MiningBoardOpenArgs`，包含当前期、档位、镐子数、
`OpenCellIds` 与 `CollectedGemIds`。页面每次有效挖掘都会通过回调和 `StateChanged` 事件返回快照，
当前 `MiningActivityModule` 负责持久化；本阶段已接开发测试 UIForm 路由、奖励配置和活动时间，尚未接
奖励领取回执或服务端经济。

## 活动页面

- `MiningMainUIPanel`（UIForm 380，`main`）承载挖宝主循环。
- `MiningStartUIPanel`（UIForm 381，`start`）使用开始页素材，Start 按钮进入主页面。
- `MiningDetailUIPanel`（UIForm 382，`details`）使用玩法说明素材，可由主页面 `DetailBtn` 打开。
- `MiningEndUIPanel`（UIForm 383，`end`）使用活动结束素材，Next 按钮关闭页面。
- 三个补充页面均注册到活动 Definition 和 `Dialog` UI 组；现有开发测试入口仍直接打开主页面。

## 首页测试入口

- 开发包通过 `MiningActivityModule` 向通用活动入口发布“Mining 测试”按钮。
- 点击后打开 `MiningScheduleConfig` 当前 Event 的已保存 Step，并提供初始 99 把镐子；关闭页面后再次进入会恢复快照。
- 该入口只在 Unity Editor 或 Development Build 中显示，正式包不会展示测试按钮。

## Test Mode

- Unity Editor 或 Development Build 初始化 Mining 后，会向统一 Test Mode 注册 `Mining / Progress` 页面。
- 页面显示当前 Event、Step、镐子数、已挖格子和已收集宝石数量。
- Event1/2 与 Step1–5 通过 `Open Selected Stage` 切换；切换前会关闭旧 Mining 页面，避免旧页面快照覆盖测试数据。
- `Pickaxes (Immediate)` 独立控制镐子数量，步进按钮或输入结束后立即保存并刷新已打开的 Mining 页面，不需要点击 `Open Selected Stage`。
- `Reset Mining Data` 会关闭 Mining 页面并恢复当前 `MiningScheduleConfig` 对应 Event 的 Step1 初始状态。
