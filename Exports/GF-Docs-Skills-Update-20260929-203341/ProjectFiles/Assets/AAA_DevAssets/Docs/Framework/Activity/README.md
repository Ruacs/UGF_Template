# Activity 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. ActivityComponent 活动系统

**功能**：ActivityComponent 持有宿主并安装 Catalog；`GameEntry.Activities` 提供公共活动访问；每个模块持有自己的状态、规则、UI、存储和游戏事实适配。

**新增/注册**：按 [活动接入](Integration.md) 在 `Activities/{ActivityName}/` 建模块、配置和 Definition；把 Definition 加入全局 Catalog；配置允许的游戏、入口和页面；复用模块安装入口同步 UIForm 与资源。活动不加入子游戏管理器列表，公共层不 new 每个活动的业务对象。

**使用**：首页订阅 `EntriesChanged` 后立即调用 `GetEntries()` 取得快照；点击传回 `OpenEntryAsync(snapshot)`。模块内部通过上下文使用 `UI.OpenAsync(new ActivityPageRequest(...), token)`，自己的页面参数由模块约定。页面制作另看 [UI 手册](../UI/README.md)。

**生命周期**：以 ModuleId + Generation + 实例句柄隔离页面、请求和事件；停用时取消任务、关页面、解绑事实并完成保存。只有安全停用/保存后才允许移除，默认保留历史存档；当前没有通用活动移除/导入工具。

**验收**：清单安装、入口可见性、允许/不允许的游戏、打开页面、取消、停用、重开及奖励重复请求。不能把历史原型 UI 验证当成设计师 Prefab 已验收。

依据：[ActivityComponent](../../../../GameMain/CustomComponents/ActivitySystem/Scripts/Runtime/ActivityComponent.cs)、[GameFrameworkActivityUI](../../../../GameMain/CustomComponents/ActivitySystem/Scripts/Integration/GameFrameworkActivityUI.cs)、[活动专题](Integration.md)。

## 所属模块与案例

公共接入协议见 [Integration](Integration.md)；具体活动的规则和页面字段跟模块维护，例如[通行证](../../../../GameMain/Activities/SeasonPass/Docs/README.md)。
