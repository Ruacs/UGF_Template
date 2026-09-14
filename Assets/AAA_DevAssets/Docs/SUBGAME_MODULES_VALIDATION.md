# 模块拆分验证记录 — 2026-09-14

> 此文保留第一阶段模块拆分的验收记录。示例包工具已在后续阶段实现，最新安装/移除状态以 [示例包验证记录](SAMPLE_PACKAGES_VALIDATION.md) 为准。

环境：Unity `2022.3.62f3c1`，当前工程 `GF_Template`。已通过本地 Unity MCP 实际编译、运行测试和场景；不是仅检查 C# 文本。

## 已通过

| 验证 | 结果 |
| --- | --- |
| TestModeModuleRegistryTests | 9 / 9 EditMode 通过 |
| SubGameServerConfigTests | 13 / 13 EditMode 通过；已按最新要求验证 7 个字段公共、不在副玩法接口中 |
| SubGameAssetRegistryTests | 10 / 10 EditMode 通过；包括当前磁盘清单与依赖 |
| Launcher → 主菜单 | EditorResourceMode 正常完成预加载，测试窗口只有 Common/Ads、Common/Runtime |
| 主菜单 → HexaAway | 资源就绪、关卡加载、Hexa 选关/皮肤/远程配置控件正常渲染；注册共 3 个模块 |
| HexaAway → 菜单 → RectMatch | Hexa 模块注销；RectMatch 页面替换，无残留 Hexa 页；公共两页保留 |
| HexaAway 再次进入/退出 | 再次进入仍 3 个模块；退出仅 2 个 Common 模块；旧上下文 IsActive=false，取消令牌已取消 |
| 主玩法来源 | 在 RectMatch 运行时仍读取 HexaAway 进度；临时将主玩法选为 RectMatch，读取随之改变，随后恢复 HexaAway |
| 测试后的状态 | 未推进两游戏关卡；测试入口恢复原禁用状态；退出 Play Mode，当前启动场景主玩法仍为 HexaAway |
| Console | 修复验证期间发现的问题后，完成场景验证时 Error 数为 0 |

服务器配置测试使用可控模拟数据源；这不代表所有移动平台 SDK 或真实服务端协议已联调通过。

## 验证中发现并处理

一次 EditMode 测试清理后，根索引的内存对象回滚到了旧内嵌条目，而磁盘仍保存新清单，导致首次 Play 停在预加载。核实当时为 EditorResourceMode，**不是旧资源包问题**。

已隔离迁移工具的 Undo 分组；逐项核对旧条目与现有清单完全相同后恢复内存引用并保存，再实际跑通上面的场景流程。全局资产和已移动脚本 GUID 保持不变。

RectMatch 新诊断页的长标签在截图中接近值区域，已缩短为 Resources / Tutorial，未改动布局或公共 Prefab。

场景验收截图保存在 `Assets/AAA_DevAssets/QA/SubGameModules/`：`SubGameModules-HexaAway.png` 与 `SubGameModules-RectMatch.png`（RectMatch 截图为缩短标签前）。最终复核：编译空闲、Console 无 Error，根索引包含 2 个清单且无旧内嵌条目；根资产与启动场景均无未保存修改，主玩法为 HexaAway。

## 尚未验证 / 未实现

- 真实远程服务器慢请求期间退出、断网与坏 JSON 的完整网络回归；当前仅验证上下文取消及迟到回调失效机制。
- 实际选关、切换皮肤、远程应用配置后的完整玩法回归；此次场景验收不通过这些操作推进或覆盖用户关卡。
- 本阶段当时仅有只读预检，未执行实际移除/回装；后续已实现受控包工具，此项最新结论见 [示例包验证记录](SAMPLE_PACKAGES_VALIDATION.md)。
- 非 EditorResourceMode、重新构建的 GF 资源包与移动端 Player。
- 当前 ResourceBuilder 输出目录仍指向旧工程路径 `D:/2026/GF_Block/AssetBundle`，本次未向该目录写入或覆盖文件。发布构建前应先改为本项目的明确输出目录再重新构建。

预检候选报告见 [依赖预检快照](SAMPLE_PREFLIGHT_20260914.md)，使用说明见 [SubGame Modules](04_SUBGAME_MODULES.md)。
