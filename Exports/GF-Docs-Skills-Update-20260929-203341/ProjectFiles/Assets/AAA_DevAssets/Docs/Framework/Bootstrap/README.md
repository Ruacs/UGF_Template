# Bootstrap 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. 启动入口与内置加载视图

**功能**：`Lokas.GameEntry` 暴露项目组件；`BuiltinViewComponent` 提供启动和切场景期间的加载/过渡显示。

**配置与接入**：从 `GameLauncher.unity` 启动，复用已有 GF 对象和组件。`GameEntry.Start` 先获取内置组件，再激活 customs 并初始化自定义组件，最后启动广告 SDK。不要在其他对象 Awake 中假定所有静态入口已就绪。新增自定义组件时，只有确需项目级公共入口才扩展 GameEntry，并在 Launcher 装配；普通页面子组件不加全局入口。

**使用**：现有流程调用 `GameEntry.BuiltinView.BeginLoadingProgress()`、`UpdateLoadingProgress(targetProgress, isComplete, elapseSeconds)` 和 `HideLoadingProgress()`。这些视图使用组件上的序列化引用，不需要为了显示首屏加载再注册一个 UIForm；进度完成条件仍由 Procedure 决定。

**验收/排错**：检查 Launcher 组件实例、customs 激活、加载视图引用和预加载事件；加载栏到达末尾不代表所有业务配置都成功。当前预加载对部分失败会记录日志后标记完成，需要同时检查错误信息。

依据：[GameEntry](../../../../GameMain/Scripts/Base/GameEntry.cs)、[自定义初始化](../../../../GameMain/Scripts/Base/GameEntry.Custom.cs)、[BuiltinViewComponent](../../../../GameMain/CustomComponents/BuiltinView/BuiltinViewComponent.cs)、[ProcedurePreload](../../../../GameMain/Scripts/Procedure/ProcedurePreload.cs)。
