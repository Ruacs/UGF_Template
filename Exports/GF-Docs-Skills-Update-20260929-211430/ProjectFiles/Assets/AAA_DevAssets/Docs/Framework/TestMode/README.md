# TestMode 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. TestModeComponent 测试模式

**功能**：控制测试窗口与注册模块。游戏测试页由所属游戏提供；公共页只包含公共能力。

**新增/注册**：在游戏 `Scripts/TestMode/` 实现 ITestModeModule，在资源就绪后通过 `RegisterModule(instance)` 注册；退出 `UnregisterModule(instance)` 并释放该实例上下文。窗口 Prefab/根视图由 TestModeComponent 与 TestModeUI 配置；完整步骤见 [子游戏扩展接入](../SubGame/Extensions.md)。

**使用**：`SetEnabled(true/false)` 控制功能，`Show()` / `Hide()` / `ToggleVisible()` 控制窗口，读 `IsWindowVisible` 同步按钮状态。不要把 IsEnabled 与“窗口正在显示”混为一谈。

**验收**：启用/禁用、重复打开、注册/注销、切游戏、请求进行中退出，以及零/单/双示例配置。异步结果晚到要取消或失效，不能写入已退出游戏。

依据：[TestModeComponent](../../../../GameMain/CustomComponents/TestModeComponent/Scripts/TestModeComponent.cs)、[模块接入](../SubGame/Extensions.md)、[示例验证矩阵](../../Samples/SamplePackages.md)。
