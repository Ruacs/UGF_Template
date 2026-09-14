# 示例子游戏包验证记录 — 2026-09-14

## 结论

两个原生 `.unitypackage` 已导出，配套文件/GUID/SHA256 清单已核对。受控移除、导入和备份恢复已经实际执行，不再是只读预检。工作工程仍保留两个示例，GameLauncher 的主玩法设置为 HexaAway。

验证环境为 Windows、Unity `2022.3.62f3c1`、EditorResourceMode。所有真实删除/回装操作都在 `Backups/SamplePackageValidation-20260914-1713/Project/` 隔离副本中执行。副本使用独立 Company/Product 名隔离存档，未清理工作工程玩家存档。启动副本使用交互编辑器；本项目的 batchmode Play 测试曾卡住，不据此声称 batchmode 兼容。

操作说明：[示例导出、移除与导入](05_SAMPLE_SUBGAMES.md)。

## 导出产物

| 游戏 | unitypackage | 所属条目数（含目录） | 字节数 |
| --- | --- | --- | --- |
| HexaAway | `HexaAway-1.0.0-20260914-170724-723.unitypackage` | 219 | 2,440,099 |
| RectMatch | `RectMatch-1.0.0-20260914-170340-255.unitypackage` | 118 | 2,378,182 |

每个包旁边必须保留同名 `.unitypackage.json`。本次验证时包位于工程根目录 `SamplePackages/`；随后迁至 `Assets/AAA_DevAssets/SamplePackages~/`，原包哈希未变，迁移复验见 [辅助目录迁移](DEV_ASSETS_LAYOUT_VALIDATION.md)。本报告保留原验证时点、任务 ID 和结果，不将旧运行记录当作迁移后的新记录。包仅包含对应玩法目录与独立场景目录；公共框架、音频、SDK 和插件不在包内。两包不是空白 Unity 工程可直接运行的独立游戏。

SHA256：

```text
HexaAway: ea0f9e85ef9d5b7fa0327accb3906b41782b7cc18c81d00e96a305e7549661f6
RectMatch: 722e9b92eab6555d8c362cc3dc0f06283d4b736b890e96d2d87b04106e330c60
```

## EditMode：88 / 88 通过

| 测试组 | 通过 / 总数 |
| --- | --- |
| SamplePackageTests | 31 / 31 |
| SubGameAssetRegistryTests | 10 / 10 |
| SubGameServerConfigTests | 8 / 8 |
| TestModeModuleRegistryTests | 9 / 9 |
| HexaAwayServerConfigTests | 3 / 3 |
| RectMatchServerConfigTests | 3 / 3 |
| TileMotionTests | 24 / 24 |

Unity 测试任务 `d8a105687ae9405ea70ddd063bfe943d`，Passed，Failed=0，Skipped=0。原始结果见 [EditMode JSON](validation/SamplePackages-EditMode-20260914.json)。

覆盖包括：危险路径拒绝、目录拥有权、真实 tar 成员/链接检查、注册冲突与幂等、孤立 meta/同名文件保护、空文件哈希的 JSON 往返、Build Settings 顺序/启用状态快照、显式运行时/存档注册，以及 7 个公共字段不进入子游戏配置接口。

## 隔离工程：实际运行与包操作

恢复实现定版后，在同一隔离副本完整重跑下列流程；开发期间失败记录保留在备份目录，没有当作通过证据。

| 状态 / 操作 | 结果 |
| --- | --- |
| 双游戏，主玩法 HexaAway | 编译与运行检查通过 |
| 移除 RectMatch → 仅 HexaAway | 原生资源删除、注册同步、编译与运行检查通过 |
| 再移除 HexaAway → 零游戏 | 编译通过，可进入菜单、公共设置和公共测试窗口 |
| 从零导入 RectMatch → 显式设为主玩法 | 导入、注册、编译与运行检查通过 |
| 再导入 HexaAway → 双游戏，主玩法仍为 RectMatch | 不自动切换主玩法；两个玩法运行检查通过 |
| 原样重复导入 HexaAway | 通过；无重复管理器/清单 |
| 恢复该次重复导入 | 通过；原本已存在的 HexaAway 未被误删 |
| 移除 RectMatch，再恢复最近操作 | 原资产 GUID、公共注册、原主玩法和场景列表恢复通过 |
| 关闭并重新启动隔离编辑器 | 注册、Build Settings 持久化及两包文件基线检查通过；337 个实际导入目标检查通过 |

各有游戏的运行检查包括 Launcher → 菜单 → 公共设置/测试窗口 → 进入游戏 → 资源就绪 → 暂停/继续 → 重启 → 返回菜单。退出后验证游戏测试页注销，旧模块上下文失效且取消令牌已取消；公共测试页保留。运行副玩法不改变公共进度来源。

QA 副本跳过首次新手引导以覆盖正常玩法进出；这不是首次引导或所有关卡的完整验收。原始阶段记录见 [运行矩阵 JSON](validation/SamplePackages-Matrix-20260914.json)。

## 本轮发现并修正

- HexaAway 专属 UI 脚本移入自己目录；公共按钮图片及 LevelBtn 动画提升为公共资产，保留 GUID，解除公共 UI 对 RectMatch 私有资源的引用。
- 公共路由、存档、设置、排行和奖励入口改为明确的注册/能力契约，不再直接引用两个具体游戏类型。
- Unity 原生导入会省略包内两个空目录：仅补回原目录及包中的原始 meta，不生成新 GUID，不自行解包脚本。
- 缺失文件哈希统一为空字符串，避免 JsonUtility 将 null 转为空串后误判备份被修改。
- Build Settings 使用实时场景快照恢复和冲突校验，不用延迟落盘的旧配置文件覆盖；不调用不适用于持久化对象的内部序列化保存 API。
- 导出/移除会阻止管理器自定义 Prefab Override；导入会拒绝本地改动、GUID/注册冲突及不匹配的孤立 meta。

## 已知问题与未覆盖范围

最终运行矩阵在双游戏回装后退出 Play Mode 时记录过一次 Unity Error：

```text
Some objects were not cleaned up when closing the scene. (Did you spawn new GameObjects from OnDestroy?)
The following scene GameObjects were found:
[DOTween]
```

开发验证初期、尚未移除任何游戏时也曾出现。未证明其根因或是否为原工程既有问题；本次没有扩大范围修改 DOTween/玩法销毁生命周期。测试仅将**停止播放阶段完全一致的这条信息**单独记入 `editorStopDiagnostics`，其余 Error 会使矩阵失败。因此不能把本轮描述为“所有 Console 全程零错误”。

尚未覆盖：移动平台 Player、非 EditorResourceMode、真实广告/支付/远程服务、所有新手引导与关卡、远程慢请求/断网/坏 JSON 的完整场景回归。ResourceBuilder 仍指向旧路径 `D:/2026/GF_Block/AssetBundle`；本次未构建或覆盖那里，发布前必须先设置本项目输出目录。

## 备份与复验

- 实施前备份：`Backups/SamplePackaging-20260914-164026/`。
- 原生操作恢复点：隔离副本中的 `Backups/SamplePackages/`；每次记录公共文件及实时场景快照。
- 最终流程日志：`Backups/SamplePackageValidation-20260914-1713/unity-final-matrix.log`。
- QA 入口：`Assets/AAA_DevAssets/Editor/Tests/SamplePackageValidation.cs`，仅接受带 `-samplePackageValidation` 标志且路径匹配隔离副本的编辑器，禁止在工作工程调用破坏性矩阵。
- 旧导出草稿移入实施备份的 `DraftExports/`，可恢复；测试副本和历史备份均未删除。

编辑器工具沿用 Unity 原生控件，按 UI/UX 规范补充了删除确认、操作阶段、冲突原因和恢复入口；没有引入额外界面框架。
