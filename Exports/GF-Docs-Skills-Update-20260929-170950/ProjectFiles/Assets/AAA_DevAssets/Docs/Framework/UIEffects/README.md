# UIEffects 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. 飞行物品、飘字与连击

**功能**：FlyItemUIComponent / FloatingTextComponent 使用项目泛型对象池；ComboComponent 根据 ComboData 表现连击。它们负责视觉，不能用动画播放本身代替奖励入账。

**配置**：绑定池的 prefab/root/capacity/prewarmCount。FloatingText 还要绑定 Canvas 与画布 RectTransform；FlyItemUI 绑定 icon/rect 等视图引用。池在 Start 后可用，坐标转换使用当前 UI 相机。ComboComponent 不在 GameEntry.Custom 的静态入口列表中，使用场景/所属模块显式引用。

```csharp
// screenPosition 是已计算好的屏幕坐标，不是世界坐标。
GameEntry.FloatingText.Show(new FloatingTextData
{
    content = "+10",
    startScreenPos = screenPosition,
    duration = 0.8f
});
```

飞行物品基本顺序为 `GameEntry.FlyItem.Spawn()` → `item.Play(data, onComplete)` → 完成后 `GameEntry.FlyItem.Recycle(item)`；中途退出也要由拥有者回收。FloatingText 的 Show 包装已在完成时回收，不再手动重复 Recycle。对同一个回收动作只保留一个拥有者。

**验收**：屏幕/世界坐标转换、相机与 Overlay 层级、连续多个表现、页面关闭/暂停期间的动画、池 ActiveCount 恢复。

依据：[FlyItemUIComponent](../../../../GameMain/CustomComponents/FlyItemUI/FlyItemUIComponent.cs)、[FlyItemUI](../../../../GameMain/CustomComponents/FlyItemUI/FlyItemUI.cs)、[FloatingTextComponent](../../../../GameMain/CustomComponents/FloatingText/FloatingTextComponent.cs)、[ComboComponent](../../../../GameMain/CustomComponents/ComboSystem/ComboComponent.cs)。
