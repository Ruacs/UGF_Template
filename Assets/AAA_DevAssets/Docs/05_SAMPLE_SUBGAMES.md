# 05 · 示例子游戏：导出、移除、导入

模板提供两个可选示例：**HexaAway**、**RectMatch**。新产品应先移除已安装的示例，再接入自己的玩法。模板初始主玩法为 HexaAway；经过移除、导入或手动配置后，以工具中的“已安装”状态及 GameManager 的主玩法配置为准，磁盘中保留示例包不代表该玩法已经安装。

## 1. 打开工具

Unity 菜单：`Tools/Template/Sample SubGames`  
Toolbar：子游戏 → 示例包导出、移除与导入  
实现：`Assets/AAA_DevAssets/Editor/SamplePackage*.cs`

先退出 Play Mode，保存场景与资产，等待编译结束。工具只支持这两个预定义示例，不是任意插件安装器。

## 2. 导出备份

点击对应游戏的“导出 unitypackage”。`Assets/AAA_DevAssets/SamplePackages~/` 中会得到：

- `{GameName}-{version}-{timestamp}.unitypackage`：原生 Unity 资源包，保留 GUID。
- 同名 `.unitypackage.json`：模板版本、注册片段、文件/GUID/SHA256 和公共依赖目录。

**两个文件必须一起保留。** 默认使用上述专用目录；末尾 `~` 让 Unity 忽略资源导入，不会在 Project 面板显示。通过“打开示例包目录”可在文件管理器中查看。不要去掉 `~`，也不要将包输出到 Assets 下其他位置。工具内部的操作备份仍写到 Assets 外的 `Backups/SamplePackages/`；外部目录导出仍受支持。只有包文件、没有校验清单时，受控导入会拒绝。

完整工程交付请使用文件系统压缩，并检查 ZIP 内包含该被忽略目录；Unity 的 Export Package 不会自动带出它。示例包合集 ZIP 只供兼容模板回装玩法，不等于完整工程。

导出只打包以下两个所属目录，使用 Recurse，不启用 IncludeDependencies：

| 示例 | 玩法目录 | 场景目录 |
| --- | --- | --- |
| HexaAway | `Assets/GameMain/SubGame/HexaAway/` | `Assets/GameMain/Scenes/SubGame/HexaAway/` |
| RectMatch | `Assets/GameMain/SubGame/RectMatch/` | `Assets/GameMain/Scenes/SubGame/RectMatch/` |

两个包依赖兼容的本 GF 模板及其 Packages/插件；工具要求使用导出时相同的 Unity 版本（当前 `2022.3.62f3c1`），升级需在副本中验证。**不能导入空白 Unity 工程后独立运行**。公共音频、SDK、公共 UI、测试框架、全局配置不复制进游戏包。校验清单用于完整性检查，不是数字签名；只导入可信来源的包，C# 代码会被 Unity 编译执行。

## 3. 新项目移除示例

1. 在新项目副本中打开 GameLauncher，确认 Console 无编译错误。
2. 为每个要移除的游戏导出最新包，建立文件基线；再点击“检查依赖与本地改动”。
3. 显式选择“移除后主玩法”：保留已安装游戏，或选 None。副玩法不会自动接管。
4. 点击“移除 {GameName}…”并核对确认框中的两个目录。
5. 等待最近操作状态变为“完成”，再操作下一个游戏。
6. 两个都移除后，将主玩法保留为 None。Launcher 可进入公共菜单；没有可用玩法的入口会隐藏。

工具先备份，再同步以下项目，最后通过 AssetDatabase 删除所属目录：

- GameLauncher 的管理器 Prefab 实例、GameManager 显式列表、可用 Procedure 类型和主玩法。
- UIForm.txt、UIFormId.cs、UIForm.bytes；Scene.txt、Scene.bytes；DefaultConfig 中自己的 Scene 键。
- 全局资源清单引用、ResourceCollection 的所属资源和资源组、Build Settings 场景。

不会删除公共测试窗口、Ads/Runtime 页、公共音频、7 个公共字段、玩家存档，也不会重排 GameMode/UI/Scene ID。游戏自己的测试页与配置脚本随包移除。

**不要在 Project/文件管理器直接删目录。** 只删目录不会同步上述序列化注册，仍可能出现 Missing Script 或失效引用。

## 4. 回装示例

1. 点击“导入已有示例包…”，默认打开 `Assets/AAA_DevAssets/SamplePackages~/`，选择与旁边 `.json` 配套的 `.unitypackage`；也可选择解压在其他目录的可信包。
2. 工具先检查模板标识、包哈希、包内真实路径、GUID 冲突、公共依赖和注册冲突。
3. 先生成需要的 UI ID，再导入游戏脚本/资源；等待编译后挂载管理器和资源清单。
4. 状态显示“完成”后，再检查主玩法设置。**导入保持原主玩法不变**；从零示例回装后仍是 None，需要手动选择。
5. 若要显示第二个入口，在公共 MainUIPanel Prefab 的 `Secondary Game Mode` 中选择副玩法；缺失或与主玩法相同的副入口隐藏。

同版本原样重复导入是幂等检查，不会创建重复管理器或清单。有本地修改、缺失/额外文件、同 ID 不同注册、GUID 已存在于其他路径时，会停止，不能靠重复导入覆盖。

如自己改过游戏文件，先审查并重新导出当前版本；如改过资源/表注册，先同步自己的 `Editor/PackageManifest.json`。不要直接修改校验哈希来绕过检查。

普通回装使用包中管理器 Prefab 的配置；Launcher 上额外的实例 Override 不包含在玩法包内。工具会阻止带自定义 Override/新增子对象的管理器导出或移除，要求先审查并应用到所属 Prefab。需要精确恢复操作前场景时用下一节的备份恢复，不要把“回装默认示例”当成“还原任意场景改动”。

## 5. 备份与失败恢复

每次移除/导入在以下位置记录恢复点：

```text
Backups/SamplePackages/{timestamp}-{operation}-{GameName}/
├── operation.json
├── Shared/                 # 精确公共文件备份
└── *.unitypackage[.json]    # 移除前的游戏备份包
Library/GFSamplePackages/operation.json  # 当前跨编译阶段状态
ProjectSettings/GFSamplePackages.json   # 已安装文件基线
```

失败后先查看工具的最近操作和 Console。点击“恢复最近一次操作…”会核对备份和当前文件哈希；如果操作后又改了公共文件或游戏内容，则拒绝覆盖，要求手动合并。此前各次恢复点仍保留在 Backups，工具只自动恢复最近一次。

Build Settings 场景列表按 Unity 的实时状态单独保存在 `operation.json` 的 `originalBuildScenes` / `afterBuildScenes` 中，包含顺序和启用状态。恢复使用 Unity API，不直接用可能延迟落盘的旧 `EditorBuildSettings.asset` 覆盖内存；若操作后又改了场景列表，工具也会停止。公共 `m_configObjects` 不属于游戏包，不修改。

若编译错误导致工具暂时不可用，在版本控制/备份中先恢复导致错误的脚本，再用工具恢复；手工恢复则需要同一恢复点的 Shared、资源包和 `originalBuildScenes` 场景快照成套处理。不要只恢复 UI ID 或根资源索引中的一项。操作期间不要编辑相关配置；备份不替代整个项目的版本控制。

## 6. 三种清单不能混淆

| 职责 | 入口 |
| --- | --- |
| 运行时管理器、路由、存档与可选能力 | GameManager 显式列表 → `GameEntry.SubGames` |
| 运行时 UI/Scene/SO 资源域 | 游戏 `ScriptableObjects/Registry/*AssetRegistryConfig.asset` → 全局索引 |
| 编辑器安装/移除的磁盘注册 | 游戏 `Editor/PackageManifest.json` + 导出包旁的校验目录 |

所有具体玩法代码（包含 UI、配置、测试页、专用 Editor 测试）归属自己目录。公共设置、奖励、商店、排行通过公共契约读取所属/主玩法，不引用具体游戏类型。

## 7. 验证范围

包功能实际验证记录见 [Sample Package Validation](SAMPLE_PACKAGES_VALIDATION.md)，本次辅助目录迁移验证见 [Dev Assets Layout Validation](DEV_ASSETS_LAYOUT_VALIDATION.md)。此前模块拆分记录保留在 [SubGame Modules Validation](SUBGAME_MODULES_VALIDATION.md)，旧预检快照仅作为历史问题记录，不代表当前工具能力。

验收应覆盖：双示例、仅 HexaAway、仅 RectMatch、零示例、重复导入、移除后回装、备份恢复；每个运行状态验证 Launcher → 菜单、公共设置/测试窗口、玩法进入/退出和模块清理。

当前包是编辑器开发资源包，不是 GF AssetBundle。使用非 EditorResourceMode 前仍需重新生成资源包、版本清单和产品构建。ResourceBuilder 当前旧输出路径指向其他工程，必须先设置成本项目的输出路径，禁止直接覆盖其他项目产物。真实广告 SDK、支付、远程服务和移动平台完整回归需要独立验证。
