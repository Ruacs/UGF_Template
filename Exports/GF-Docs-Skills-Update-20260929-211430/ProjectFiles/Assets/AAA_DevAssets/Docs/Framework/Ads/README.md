# Ads 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. 广告与服务器配置

**功能**：AdsServerConfig 组织公共功能/计时字段和按 GameMode 注册的游戏策略；AdsManager 承接项目 SDK 调用。公共商店、排行等解锁跟随主玩法，关卡广告跟随所属游戏。

**新增/注册**：游戏专用字段、默认值和 Key 放自己的 `Scripts/Config/`，按现有策略注册；公共 7 个功能/计时字段保留 Common。配置和测试入口按 [子游戏扩展接入](../SubGame/Extensions.md) 接入，不向公共配置重复添加每个玩法的同名字段。

**使用**：业务通过现有 AdsManager/策略入口请求，明确成功、失败、取消和 SDK 未就绪路径；游戏事实必须携带所属模式。主玩法是否配置有效可以从 `AdsServerConfig.TryGetPrimaryLevel(out level)` 的返回值判断。

**验收**：默认值与远端值、主/副玩法、不可用/失败回调、切游戏期间回调、Editor 模拟与目标平台真机。开启调试成功开关只验证模拟分支，不能作为真实广告已通过的结论。

依据：[配置接入](../SubGame/Extensions.md)、[GameEntry 的 SDK 启动](../../../../GameMain/Scripts/Base/GameEntry.cs)。
