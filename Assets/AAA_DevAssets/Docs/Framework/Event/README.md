# Event 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. EventComponent

**功能**：让业务事实通知订阅者，避免 UI 和玩法互相持有不必要引用。事件不替代需要返回结果的直接方法调用。

**接入步骤**：优先复用已有事件；新增事件继承 GameEventArgs，提供唯一 EventId、Id、Create 和 Clear，按现有 ReferencePool 模式实现。订阅在拥有者进入有效期时建立，离开有效期时解除；页面可使用 UGuiForm 的成对钩子。

已有任务通关事件的调用片段：

```csharp
// 在真实通关业务点发送一次；levelId 为实际关卡标识。
GameEntry.Event.Fire(this, LevelPassedEventArgs.Create(levelId));

// 生命周期中成对调用；OnLevelPassed 签名为 (object, GameEventArgs)。
GameEntry.Event.Subscribe(LevelPassedEventArgs.EventId, OnLevelPassed);
GameEntry.Event.Unsubscribe(LevelPassedEventArgs.EventId, OnLevelPassed);
```

上面订阅/解绑两行展示配对关系，应分别放入进入/退出函数，不是连续执行。GF Fire 为排队分发，不能依赖接收者在调用返回前完成业务；使用池化事件时不要跨异步保存 EventArgs 引用，需要的数据复制出来。

**验收**：一次动作只推进一次；关闭页面/退出游戏后不再收到旧订阅；重复进入不会增长回调数。跨游戏事件必须能确定事实来源，活动事实协议见 [活动接入 §5](../Activity/Integration.md#5-游戏事实)。

依据：[GF EventComponent](../../../../Plugins/UnityGameFramework/Scripts/Runtime/Event/EventComponent.cs)、[LevelPassedEventArgs](../../../../GameMain/CustomComponents/TaskSystem/Scripts/Events/LevelPassedEventArgs.cs)。
