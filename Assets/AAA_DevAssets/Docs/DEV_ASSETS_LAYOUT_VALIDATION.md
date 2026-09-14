# AAA_DevAssets 辅助目录迁移 — 2026-09-14

## 范围与保留内容

按用户确认迁移根目录 `docs` → `Assets/AAA_DevAssets/Docs/`、`Tools` → `Assets/AAA_DevAssets/Tools/`、`SamplePackages` → `Assets/AAA_DevAssets/SamplePackages~/`。根目录 README、AGENTS、框架规范与 `Backups/` 不搬迁。`Docs.meta` 原 GUID `e2792ec5c78342041808ea5a4e51100e` 保留；工具脚本内容未变。

迁移前后主工程均只安装 RectMatch，主玩法为 RectMatch（3）。已核对 Launcher 与 `ProjectSettings/GFSamplePackages.json` 哈希一致，没有回装 HexaAway、重置主玩法或改动运行时代码。当前已完成操作记录中的 RectMatch 包路径同步指向新位置，备份目录和其余记录字段保留。

`RectMatchGameManagerComponent.cs` 存在相对原包的本地改动，本轮保留，没有通过重新导出更新安装基线来消除该提示。两个分发包仍为原始已验证版本，不包含该本地改动。

## 工具与文档

- 默认导出目录及导入文件选择器指向 `SamplePackages~/`。
- 依据 UI/UX 技能的可发现性规则，沿用原生控件新增“打开示例包目录”，用于访问 Project 面板中不可见的目录；未增加新工具菜单或界面框架。
- 仅放行 Assets 内这个精确目录；其他 Assets 路径、近似名称、子目录、路径穿越和可检测的链接路径仍拒绝，Assets 外部备份输出保留。
- 根目录入口文档和迁移后文档链接已同步，注明 `~` 不能删除、包文件和 JSON 必须配套，以及完整工程 ZIP 不等于示例包合集。

## 已验证

Unity 版本：`2022.3.62f3c1`。已恢复自动刷新，脚本编译完成，主工程错误日志查询为 0；示例管理窗口已打开且无新增 Console Error。

### 主工程 EditMode：73 / 73

任务 ID：`12c06ba5f60e49d5ab1495de2eca0f3d`；Passed，Failed=0，Skipped=0。原始结果见 [EditMode JSON](validation/DevAssetsLayout-EditMode-20260914.json)。

| 测试组 | 通过 / 总数 |
| --- | --- |
| SamplePackageTests | 43 / 43 |
| SubGameAssetRegistryTests | 10 / 10 |
| SubGameServerConfigTests | 8 / 8 |
| TestModeModuleRegistryTests | 9 / 9 |
| RectMatchServerConfigTests | 3 / 3 |

其中新增 12 个目录边界用例。主工程未安装 HexaAway，因此没有它的 27 项专用测试；不要将本次结果误写成双游戏环境的 100 项。

### 包移动、导出与忽略导入

两份原包移动前后 SHA256 不变，工具重新读取真实包成员并验证同名 JSON、GUID、路径、内容哈希与内嵌清单：HexaAway 219 条、RectMatch 118 条，均通过。

实际调用默认目录导出 RectMatch（`recordInstalled=false`）成功，得到 `RectMatch-1.0.0-20260914-182618-869.unitypackage`；其 118 条成员及清单回读通过。此包只用于验证当前工作副本的导出路径，已移入下述备份的 `MigrationArtifacts/`，不替换原始分发包，也不改写安装基线。

强制刷新后，Unity AssetDatabase 中 `Assets/AAA_DevAssets/SamplePackages~` 前缀的条目数为 0，目录未生成 `.meta`，确认示例包未参与 Unity 资源导入。Docs / Tools 已正常导入，且它们的 GUID 在 GF ResourceCollection 中引用数为 0。移动后的 PowerShell MCP 辅助脚本已用于实际连接与执行测试。

### 隔离运行矩阵

在 `Backups/SamplePackageValidation-20260914-1713/Project/` 隔离副本，用迁移后的工具代码完整重跑。结果为 step=13、phase=complete、15 条阶段记录、errors=0，四条停止播放时完全匹配的 `[DOTween]` 清理诊断单独保留。主工程没有执行移除/回装测试。

通过的状态包括：双游戏 → 仅 HexaAway → 零游戏 → 从新目录导入 RectMatch → 从新目录导入 HexaAway → 重复导入 → 恢复重复导入 → 移除 RectMatch 并恢复。各运行状态验证菜单、公共设置/测试页、进入玩法、暂停/继续、重启、退出与测试模块清理；运行副玩法不切换公共进度来源。

导入操作记录确认两份包实际来自 `Assets/AAA_DevAssets/SamplePackages~/`，不是旧根目录包。随后关闭并重启隔离编辑器，注册、Build Settings 持久化、两包的 337 个文件目标基线校验通过，最终共有 16 条通过记录。原始结果见 [迁移后矩阵 JSON](validation/DevAssetsLayout-Matrix-20260914.json)。日志为 `Backups/SamplePackageValidation-20260914-1713/unity-devassets-layout.log` 与 `unity-devassets-layout-restart.log`。

## 分发产物

示例合集仍命名为 `SampleSubGames-Unity2022.3.62f3c1-20260914.zip`，位于 `Assets/AAA_DevAssets/SamplePackages~/`。按新目录结构收录两个原包及 JSON、包 README、Docs（连同 Unity meta）以及根目录三份入口文档，不包含路径测试临时导出包、旧 ZIP、备份、Library 或运行时框架。它是**兼容模板的示例包与文档合集**，不是完整工程；不能解压后直接在 Unity Hub 作为工程打开。

## 备份与边界

迁移前备份：`Backups/DevAssetsLayout-20260914-181753/`。原目录完整备份、修改前的工具/文档、Docs.meta、Launcher、安装基线和当前操作记录均保留；旧合集 ZIP 与路径测试导出包归档到 `MigrationArtifacts/`，可恢复。未删除任何游戏资源或历史备份。

此次不验证移动平台、真实广告/支付/远程服务、非 EditorResourceMode 或全新电脑首次导入。既有停止播放时的 `[DOTween]` 清理诊断继续单独记录，不据此声称运行矩阵 Console 全程零错误，也不在目录迁移中修改玩法销毁生命周期。
