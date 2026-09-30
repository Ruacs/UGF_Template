# Procedure 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. Procedure、场景与子游戏路由

**功能**：Procedure 管启动和场景流程；SceneComponent 加载场景；GameManager 聚合已注册子游戏、主玩法和当前状态；每个 SubGameManager 管自己的玩法生命周期。

**新增步骤**：

1. 定义唯一 GameMode，实现所属游戏的 `SubGameManagerComponent`，提供 `SceneConfigKey`、`GameProcedureType` 和进度信息。
2. 在 Launcher 挂管理器并加入 GameManager 的显式列表；把游戏 Procedure 加入 Procedure 组件配置。
3. 同步 `DefaultConfig.txt` 的场景键、`Scene.txt → Scene.bytes`、场景资源域和 Build Settings。
4. 在游戏管理器内实现资源准备及释放，由 `ProcedureChangeScene` 在切场景期间调用 `InitializeResourcesAsync()`。
5. 游戏 Procedure 负责开始、打开玩法 UI、退出清理。完整操作与存档接入见 [子游戏接入](../SubGame/Integration.md)，注册位置见 [注册映射](../RegistrationMap.md)。

**使用**：主菜单通过已有 `ProcedureMenu.StartGame(mode)` 路由；查询管理器用 `GameEntry.SubGames.Get(mode)`，可为空。公共入口不用具体游戏类型写 switch。区分 `PrimaryGameMode`（公共解锁进度来源）与 `CurrentGameMode`（当前正在玩的游戏）。

**生命周期与验收**：切入 → 资源初始化 → 游戏开始 → 暂停/恢复 → 重启 → 返回 → 再次进入。退出时清理游戏拥有的 UI、事件、异步请求和池对象。直接调用 SceneComponent 加载场景不会补齐上述注册与玩法状态。

依据：[GameManagerComponent](../../../../GameMain/Scripts/Component/GameManagerComponent.cs)、[SubGameManagerComponent](../../../../GameMain/Scripts/Component/SubGameManagerComponent.cs)、[ProcedureChangeScene](../../../../GameMain/Scripts/Procedure/ProcedureChangeScene.cs)。
