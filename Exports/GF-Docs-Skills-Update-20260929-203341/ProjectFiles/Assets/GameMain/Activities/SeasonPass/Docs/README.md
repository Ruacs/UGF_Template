# SeasonPass 通行证模块

[开发文档导航](../../../../AAA_DevAssets/Docs/README.md) · [活动框架](../../../../AAA_DevAssets/Docs/Framework/Activity/README.md)

通行证持有档位、免费/高级轨道、充能、领取状态、购买服务及页面数据；公共 Activity 负责宿主、作用域、事实和页面路由。通行证规则不进入 UIComponent 或通用 Reward View。

## 使用入口

- [UI Prefab 字段契约](UIPrefabContract.md)：主页、规则页、Gold Pass 购买页及奖励行绑定。
- [业务模块](../Scripts/SeasonPassActivityModule.cs)、[数值配置目录](../ScriptableObjects/Config/)、[模块注册定义](../ScriptableObjects/Registry/SeasonPassActivityDefinition.asset)。
- 安装与校验：`Tools/Activity/Season Pass/Install Config And Validate UI`。安装器维护配置、注册与绑定校验，设计师维护视觉 Prefab。

## 奖励配置

在 [SchoolPass202609](../ScriptableObjects/Config/SchoolPass202609.asset) 的 Tier 上配置 Free Reward / Premium Reward，引用各自奖励包。包位于[模块 Rewards](../ScriptableObjects/Rewards/)；字段语义与显示由[通用 Reward 手册](../../../../AAA_DevAssets/Docs/Framework/Reward/README.md)说明。

通行证的累计充能阈值、领取状态和购买结果由业务模块计算；UI 只绑定快照并提交请求，不能在预览宝箱、切换皮肤或动画结束时自行发奖。运行中的配置更换需考虑已有领取记录。

## 接入与验收

先按活动框架安装 Definition/Catalog 和页面注册，再按字段契约制作 Prefab。核对打开/关闭/重开、充能跨档、两条奖励轨道、购买成功/失败/等待、重复领取、页面关闭时请求处理及存档恢复。真实支付或服务端发奖需对应环境验证，静态配置存在不等于业务已完成。

历史奖励迁移与当时限制见[2026-09-16 记录](../../../../AAA_DevAssets/Docs/Archive/Implementation/RewardUI-20260916.md)，不以其中固定档数或旧名称约束后续配置。
