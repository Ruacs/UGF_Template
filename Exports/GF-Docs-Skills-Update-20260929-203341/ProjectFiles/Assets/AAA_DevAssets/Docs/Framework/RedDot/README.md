# RedDot 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. RedDotComponent 红点

**功能**：按路径注册节点、维护计数和父子聚合，通过订阅更新视图。红点表示业务状态，页面按钮显示/隐藏不应成为奖励可领状态的唯一来源。

**先确认类型**：`GameEntry.RedDot` 使用 `Lokas.RedDotComponent`，位于 `CustomComponents/RedDotSystem/`。另有 `GF_Mahjong.RedDot.RedDotComponent` 位于 `CustomComponents/RedDot/Core/`，提供规则注册 API；两者不能混用。当前公共入口没有 `RegisterRule` 方法。

**新增/使用**：在当前组件 `_configs` 配路径/父路径，或运行时 `Register(path)` / `Register(parentPath, childPath)`；业务更新 `SetCount`、`AddCount`、`Clear`；视图进入时 Subscribe 并主动读取一次 GetCount，退出时用同一委托 Unsubscribe。

```csharp
GameEntry.RedDot.Register("Example.Reward"); // 示例键；实际项目按模块约定命名。
GameEntry.RedDot.SetCount("Example.Reward", 1);
int count = GameEntry.RedDot.GetCount("Example.Reward");
```

**注意**：`Changed` 与路径订阅不是同一种通知；包装层 Changed 是 SetCount/AddCount/Clear 时对指定路径比较后发出，不能假定 ClearAll 或所有父节点变更都会通过它逐项报告。需要哪个节点就对照实际 Manager 的订阅行为。

**验收**：首次显示、子节点变化、父节点聚合、清空、页面关闭重开和重复注册。红点状态恢复应从业务数据计算，不能只依赖上次 UI 留下的计数。

依据：[当前 RedDotComponent](../../../../GameMain/CustomComponents/RedDotSystem/RedDotComponent.cs)、[另一套同名实现](../../../../GameMain/CustomComponents/RedDot/Core/RedDotComponent.cs)。
