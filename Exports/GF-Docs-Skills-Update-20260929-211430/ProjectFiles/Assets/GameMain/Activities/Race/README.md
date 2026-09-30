# Race activity · local MVP

一句话：一个五车、十关目标的本地 Race 活动，使用现有活动宿主接收 `Game` 的通关事实。

## 当前范围

- 报名、五车进度模拟、赛程倒计时、三档名次奖励与幂等领取。
- 页面键：`start`、`main`、`details`；结果面板嵌入赛道主页。
- 页面 Prefab 从 `Assets/GameMain/UI/Template/UIPanel_Template.prefab` 创建，保留全屏锚点、透明根 Image、CanvasGroup 与模板过渡组件；内部节点按 APK 分析文档的起始页、赛道页和规则页结构序列化。
- 菜单 `Tools/Activity/Race/Install Local MVP` 创建配置、奖励资产、页面 Prefab 和 UIForm 注册。它不会覆盖设计师后来维护的 Prefab 或已有数值；它会自动修复最早版本生成的空壳 Race Prefab。
- 若需重建三个 Race 页面层级，使用 `Tools/Activity/Race/Rebuild UI Prefabs From Template`。该菜单会修复不符合模板的根节点、重建文档中的内部层级，并保留现有 Prefab 内的静态可见文案；页面脚本仅绑定节点和刷新动态状态。

## APK 对照与待接入项

参考分析：`D:/AAA_AgentProject/APK分析/docs/2026-09-17-Hexa-Race-UI-Implementation-Analysis.md`。

- `Textures/SourceExport/` 保存 APK 导出的 Race 图片；导入设置和正式视觉绑定仍由设计阶段完成。
- 当前只启用 `Free` 报名。广告和无限体力报名、真实名次区间、礼盒/广告结算顺序尚未 Trace，不能据节点名推测后直接上线。
- 本地奖励接收器只是可替换的适配缝；生产环境须使用服务端报名、排行榜与领奖回执。
