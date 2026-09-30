# Task 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. TaskComponent 任务系统

**功能**：从 Provider 读取任务，推进条件并领取奖励；监听消除、通关和道具使用三个 GF 事件。

**配置/新增**：

1. 在 TaskComponent Inspector 选择数据源。SO 模式绑定 TaskSOProvider，并把 TaskDataSO 加入它的任务列表。
2. JSON 模式使用 StreamingAssets 下 `_jsonRelativePath`；Remote 模式配置 `_remoteApiUrl`，请求使用当前 Provider 的鉴权逻辑。
3. 检查 taskId 唯一、条件与奖励类型可由现有工厂识别。新增类型应参考所属条件/奖励的注册方式，不能只在 JSON 中发明类型名。
4. 从真实玩法事实点发送对应事件，UI 通过 TaskComponent 查询/领取，不自行修改任务进度。

**当前行为**：组件 Start 异步初始化 Provider，随后接受所有 Available 任务并订阅事件。可用 API 为 `GetTask(taskId)`、`GetByStatus(status)`、`AcceptTask(taskId)`、`ClaimTask(taskId)`；在初始化完成之前不要抢先依赖任务列表。

**实现限制**：`DataSourceMode.Custom` 当前仍回落 `_soProvider`，不是通用自定义 Provider 注入入口；`IsUnlocked` 实际仅判断 SaveData 非空，与源码中“按关卡解锁”的注释不同；`ShowTaskPopup` 当前没有执行显示逻辑。远程 Provider 直接使用 UnityWebRequest，并非 GameEntry.WebRequest。以上按代码说明，不在本次文档整理中改实现。

领取会调用各奖励的 `Grant()`，但具体落地程度不同：GoldReward 写 SaveData.Money；ItemReward 和 UnlockReward 当前只记录日志；ExpReward 调用可选的 OnGrantExp 委托，是否到账取决于业务接入。不能把 ClaimTask 返回成功等同于所有类型奖励都已入账。依据：[奖励实现目录](../../../../GameMain/CustomComponents/TaskSystem/Scripts/Rewards)。

**验收**：任务加载、一次事件推进一次、达成/领取/重复领取、重启与每日刷新；分别测试实际启用的数据源。不要以调用 ShowTaskPopup 成功作为弹窗验收。

依据：[TaskComponent](../../../../GameMain/CustomComponents/TaskSystem/Scripts/Runtime/TaskComponent.cs)、[TaskSOProvider](../../../../GameMain/CustomComponents/TaskSystem/Scripts/Providers/TaskSOProvider.cs)、[TaskJsonProvider](../../../../GameMain/CustomComponents/TaskSystem/Scripts/Providers/TaskJsonProvider.cs)、[TaskRemoteProvider](../../../../GameMain/CustomComponents/TaskSystem/Scripts/Providers/TaskRemoteProvider.cs)。
