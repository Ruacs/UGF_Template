# Rank 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. RankComponent 排行榜

**功能**：维护榜单、自己的分数、排名目标、周期倒计时和待领取结果；实现中包含模拟玩家与离线推进逻辑，不应描述为已经连通服务端实时排行榜。

**配置**：预加载通过 `SetRankTargetConfig` 注入 RankTargetConfigSO；存档和配置就绪后，ProcedurePreload.OnLeave 调用 `StartUp()`。不要提前在 Awake 读存档或反复启动一套新榜单。

**使用**：读 `Ranks`、`SelfRank`、`CurrentCycle`、`CycleRemainingTimeText`；用 `AddSelfScore(delta)` / `UpdateSelfScore(score)` 更新分数，外部数据可 `SetRanks(...)`。`TryPopPendingRankReward(out rank)` 消费待领奖结果，UI 刷新时不要重复消费。开发验证可用已有 TestMode 入口，避免业务 UI 直接调用 DEV 方法。

**新增目标/周期**：修改对应 SO 并核对图标、语言 Key、奖励配置；如果要用服务端数据，需单独明确权威来源和同步协议，不能只替换列表便宣称具备在线同步。

**验收**：未解锁、空列表、自身排名、分数更新、跨周期、重启回读、待领奖只消费一次。

依据：[RankComponent](../../../../GameMain/CustomComponents/Rank/Runtime/RankComponent.cs)。
