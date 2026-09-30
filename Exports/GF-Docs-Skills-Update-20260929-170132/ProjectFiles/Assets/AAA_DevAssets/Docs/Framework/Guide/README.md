# Guide 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. 引导模块

**功能**：GuideMaskController 高亮/移动遮罩并显示提示，GuideRunner 按 GuideStep 列表推进流程；通过所属页面或场景持有引用，没有统一 `GameEntry.Guide` 入口。

**接入**：参考现有 GuideMask Prefab 和示例，配置控制器、目标相机、提示视图及步骤目标；UI 目标使用实际 RectTransform。步骤是否完成、何时记存档由业务定义，不能将遮罩关闭等同于新手引导永久完成。

**使用**：单目标使用 `controller.Show(target, ...)` / `Close(duration)`；序列使用 `runner.Play(steps)` / `Stop()`。离开所属页面/场景时停止流程和动画，避免引用已回收 Widget。

**验收**：目标不存在、移动目标、不同分辨率、提示点击、步骤中断、关闭重开以及引导完成存档。

依据：[GuideMaskController](../../../../GameMain/GuideMask/Script/GuideMaskController.cs)、[GuideRunner](../../../../GameMain/GuideMask/Script/GuideRunner.cs)、[流程示例](../../../../GameMain/GuideMask/Script/GuideMaskFlowExample.cs)。

以上为接入与验收说明；本次未执行 Unity 或外部服务运行验证。
