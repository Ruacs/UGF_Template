# Shop 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. ShopComponent 商店

**功能**：读取商品目录、过滤可见商品、查询价格、验证并发起购买、回调结果。公共解锁跟随主玩法进度。

**配置/新增**：在 ShopComponent 绑定 ShopCatalogSO 并启用 EnableShop；把 ShopItemConfigSO 加入目录，配置唯一商品 ID、排序、价格类型、奖励及 IAP 产品 ID。IAP 还需目标平台 SDK/商品配置有效，创建 SO 不会自动创建平台商品。

**使用顺序**：

1. 用 `IsAvailable` 判断入口；`GetVisibleItems()` 生成列表，用 `GetPriceText(item)` 显示价格。
2. UI 订阅 `OnBuyFinished`、`OnProductInfoUpdated`，需要时显示 `OnProductInfoUpdateFailed`；关闭时成对解绑。
3. 点击调用 `TryBuy(itemId)`。处理 Success / Pending / 失败结果；Pending 不表示奖励已经发放。
4. 商品价格更新可通过 `RefreshProductInfo(force)` 请求。购买发奖由 ShopComponent 处理，按钮回调不能再次发同一奖励。

**验收**：未解锁/禁用、空目录、金币不足、重复点击、等待 SDK 回调、失败再试、购买成功与页面重开。真实 IAP 成功链需在相应平台环境验证，Editor 显示商品不是支付通过。

依据：[ShopComponent](../../../../GameMain/CustomComponents/Shop/Scripts/ShopComponent.cs)、[ShopItemView 事件说明](../../../../GameMain/CustomComponents/Shop/Scripts/UI/ShopItemViewEventsUsage.md)。
