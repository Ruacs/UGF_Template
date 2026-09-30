# 框架模块

[返回文档导航](../README.md)

本层说明可复用能力、扩展接口和接入条件。具体玩法和活动依赖这些能力；框架通过协议、配置接入具体模块。目录分类不表示现有代码已经完全解耦：各手册继续列出当前业务适配、初始化条件及实现限制。

| 模块 | 职责与说明 |
| --- | --- |
| [Bootstrap](Bootstrap/README.md) | 启动入口与加载视图 |
| [Procedure](Procedure/README.md) | 启动流程、场景切换与路由 |
| [UI](UI/README.md) | 页面与交互生命周期 |
| [Localization](Localization/README.md) | 字典、文本与语言切换 |
| [TMPFont](TMPFont/README.md) | 字体、样式与字符集 |
| [Activity](Activity/README.md) | 活动宿主与扩展协议 |
| [SubGame](SubGame/README.md) | 子游戏管理与接入 |
| [Resource](Resource/README.md) | 资源域、加载与释放 |
| [Configuration](Configuration/README.md) | Config、DataTable、SO 与 JSON |
| [Event](Event/README.md) | 事件与订阅 |
| [SaveData](SaveData/README.md) | 存档与设置 |
| [Sound](Sound/README.md) | 声音分组与播放 |
| [Entity](Entity/README.md) | 实体与对象池 |
| [Task](Task/README.md) | 任务能力与接收器限制 |
| [Shop](Shop/README.md) | 商品、购买与支付接入 |
| [Rank](Rank/README.md) | 排行能力与当前本地模拟限制 |
| [RedDot](RedDot/README.md) | 红点节点与订阅 |
| [Reward](Reward/README.md) | 奖励配置与展示 |
| [UIEffects](UIEffects/README.md) | 飞行、飘字与连击 |
| [Guide](Guide/README.md) | 局部引导 |
| [TestMode](TestMode/README.md) | 测试页面与模块注册 |
| [Ads](Ads/README.md) | 广告策略与 SDK 适配 |
| [Infrastructure](Infrastructure/README.md) | 其他 GF 基础设施 |

跨模块接入查[注册映射](RegistrationMap.md)，架构约束查[框架规范](../../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md)。

## 1. Localization 与 TMPFont

这两个模块已拆成独立手册；本节保留旧链接入口，详细规则只在专题中维护。

- [本地化](Localization/README.md)：语言与字典、文本 Key、编辑工具、静态/动态文本、新语言接入、语言图片、完整切换流程与当前缺口。
- [TMPFont](TMPFont/README.md)：语言字体映射、字符集/SDF、Profile/Preset/Applier、材质缓存、样式制作和缺字排错。

跨模块切换流程由本地化手册持有，字体制作细节由 TMPFont 手册持有；页面布局与视觉验收仍见 [UI 手册](UI/README.md)。
