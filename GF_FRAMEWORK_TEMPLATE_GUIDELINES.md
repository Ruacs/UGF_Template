# GF 框架模板项目规范

> 本文档用于设计下一代 GF Unity 框架模板项目。
>
> 当前项目仅作为经验和问题来源，不要求按照本文档改造当前项目。新项目应在创建之初遵循本文档，避免依赖全局搜索历史代码。
>
> **适用阶段**：本文中的“必须”“禁止”描述目标模板的规范；当前样例已经实现的操作路径以 `Assets/AAA_DevAssets/Docs/02_NEW_SUBGAME_CURRENT.md` 和 `Assets/AAA_DevAssets/Docs/03_REGISTRATION_MAP.md` 为准。两者不一致时，必须显式标注为“待实现的目标能力”，不得把目标 API 当作当前可用 API。

## 1. 文档目标

模板项目应让开发者或 AI 能够根据固定流程完成以下工作：

- 新增一个子游戏。
- 新增一个子游戏管理器。
- 新增一个 UI 页面。
- 新增一个 ScriptableObject 配置。
- 新增关卡、图案、规则等游戏资源。
- 新增多语言文本和多语言图片。
- 更新字符集和 TMP SDF 字体资源。
- 注册场景、Procedure、配置表和运行时资源。

所有新增内容都应有明确的归属、路径、注册位置和验证方式。

### 1.1 新人阅读顺序

1. `README.md`：安装要求、首次打开和最小运行验证。
2. `Assets/AAA_DevAssets/Docs/01_GETTING_STARTED.md`：Unity 资源安全规则、生成文件和常见故障。
3. `Assets/AAA_DevAssets/Docs/02_NEW_SUBGAME_CURRENT.md`：维护当前样例时的真实新增子游戏流程。
4. `Assets/AAA_DevAssets/Docs/03_REGISTRATION_MAP.md`：查找注册点与生成链路。
5. `Assets/AAA_DevAssets/Docs/04_SUBGAME_MODULES.md`：接入测试页面、服务器配置和资源清单。
6. `Assets/AAA_DevAssets/Docs/05_SAMPLE_SUBGAMES.md`：将带示例玩法的工程初始化为产品基线时的边界与风险。
7. 本文档：新建模板或演进框架时的目标架构和强制约束。

`AGENTS.md` 是 Agent 的协作约束，不替代上述面向开发者的使用文档。

## 2. 当前项目提取出的主要痛点

### 2.1 代码和资源归属不清晰

同一个子游戏的代码、UI、配置、场景和资源可能分散在多个顶层目录中。AI 需要全局搜索才能判断某个文件是否属于某个游戏。

模板项目应优先按“通用系统”和“子游戏”划分，新增子游戏内容必须集中在对应目录中。

### 2.2 子游戏注册点过多

新增游戏通常需要分别修改游戏模式枚举、GameEntry、Procedure、场景配置、预加载逻辑和 UI 逻辑。注册点分散会导致遗漏和运行时错误。

模板项目应提供统一的子游戏注册机制，并尽量通过配置或注册表管理子游戏，而不是在多个文件中维护具体类型引用。

### 2.3 UI 页面新增步骤容易遗漏

新增 UI 页面不仅需要脚本和 Prefab，还可能需要同步更新：

- UIFormId.cs
- UIForm.txt
- UIForm.bytes
- UI 分组
- 页面暂停行为
- Prefab 资源路径
- 多语言文本
- 多语言图片

模板项目应提供 UI 页面生成工具或明确的自动化流程，并提供同步检查。

### 2.4 ScriptableObject 创建方式不统一

不同配置的继承类型、CreateAssetMenu 菜单路径、资源保存目录和 ID 规则不一致，容易造成配置类型误用和资源散落。

模板项目应明确配置类型选择、菜单命名、资源路径和 ID 分配规则。

### 2.5 多语言更新链路不完整

新增文本后，除了翻译表，还可能需要更新 Key、字符集、TMP SDF 字体和多语言图片。只修改 XML 或文本表会导致运行时缺字或图片语言错误。

模板项目应把多语言内容视为一个完整资源变更流程。

## 3. 总体架构原则

### 3.1 通用系统与子游戏分离

通用系统负责跨游戏复用的能力，例如：

- 玩家数据。
- 货币和奖励。
- 头像和头像框。
- 商店。
- 任务和红点。
- 通用 UI。
- 多语言。
- 音频和资源加载。
- 全局游戏状态和计时。

子游戏负责自身玩法，例如：

- 玩法逻辑。
- 关卡和图案数据。
- 游戏专用输入。
- 游戏专用视图。
- 游戏专用 Procedure。
- 游戏专用 UI。
- 游戏专用配置和资源。

通用系统不得依赖某一个具体子游戏的实现。子游戏可以使用通用系统，但不应反向污染通用系统。

### 3.2 优先使用注册表而不是具体类型硬编码

模板项目应提供统一的子游戏注册和查询能力，例如：

```csharp
GameEntry.SubGames.TryGet(GameMode.RectMatch, out SubGameManagerComponent manager);
var rectMatch = GameEntry.SubGames.Get<RectMatchGameManagerComponent>(GameMode.RectMatch);
```

新增子游戏不应默认要求在多个 `GameEntry` 属性中增加具体类型。只有确实需要高频、强类型访问时，才允许增加明确的快捷入口。

> **当前样例状态**：已实现 `GameEntry.SubGames`，由 `GameManagerComponent` 持有显式管理器列表；空列表合法。管理器提供 `SceneConfigKey`、`GameProcedureType`、存档注册与公共能力。公共路由不依赖具体游戏类型；仅游戏自己的 partial GameEntry 保留历史快捷入口。`SubGameAssetRegistry` 仍只负责资源域解析，两者职责不同。

### 3.3 保持生命周期一致

所有子游戏管理器应遵循统一生命周期：

- 初始化。
- 开始游戏。
- 暂停。
- 恢复。
- 重启。
- 游戏结束。
- 返回主界面。
- 重置和清理。

子游戏管理器不应自行创建全局 Singleton，也不应绕过 GF 的 Procedure、Event、Resource 和 UI 流程。

### 3.4 固定根命名空间

模板项目的 C# 根命名空间固定为：

```csharp
namespace Lokas
```

编辑器代码统一使用：

```csharp
namespace Lokas.Editor
```

编辑器子模块可继续向下划分，例如：

```csharp
namespace Lokas.Editor.DataTableTools
```

该命名空间表示模板工程层，不代表具体游戏名称。使用模板创建新项目时，不应因为游戏名、包名或产品名变化而修改根命名空间。游戏名称、包名、显示名和资源归属应通过 ProjectSettings、配置资产、资源目录和子游戏注册信息表达。

数据表生成代码必须生成到 `Lokas` 命名空间，并与 `DataTableExtension.DataTableNamespace` 保持一致。新增代码生成器、模板文件或脚手架工具不得硬编码其他项目命名空间。

### 3.5 子游戏定义模块，公共层负责聚合

测试页面、广告/服务器配置和资源注册采用同一原则：**每个子游戏持有自己的实现与数据，公共层只提供契约、注册、查询和生命周期管理**。

| 扩展点 | 子游戏拥有 | 公共层负责 |
| --- | --- | --- |
| 测试页面 | 自己的页面模块、测试项、状态和操作回调 | `TestModeUI` 提供窗口、控件和模块调度；公共 Ads、Runtime 测试继续共用。 |
| 广告/服务器配置 | 自己的配置脚本、专用字段、默认值、远程 Key 映射和解析校验 | 保留 `AdsServerConfig` 作为统一管理入口，按游戏标识管理多份配置，不集中声明各游戏的专用字段。 |
| 资源注册 | 自己的资源清单及必要的注册适配代码 | `SubGameAssetRegistryConfig` 聚合已安装游戏的清单引用，`SubGameAssetRegistry` 负责校验和资源域查询。 |

推荐通过公共接口/基类及显式配置引用接入，由子游戏自己的模块入口提供实现。只把类拆到不同文件、却仍在公共类里硬编码具体类型或逐个 `new HexaAway...`，只能改善阅读，不能形成可移除的边界。

模块入口复用既有子游戏管理器、Procedure 和加载流程，不另建一套全局 Manager；游戏标识在三个扩展点之间必须一致。公共代码不能根据类名猜测归属，或通过未声明的运行时扫描自动装入所有玩法。当前两个示例已按此拆分，具体约束见第 5.6—5.8 节。

## 4. 标准目录结构

建议模板项目使用以下结构：

```text
Assets/GameMain/
├── Scripts/
│   ├── Base/
│   ├── Common/
│   ├── Component/
│   ├── Procedure/
│   ├── UI/
│   ├── Localization/
│   └── SubGame/                       # 子游戏公共运行时辅助，不放具体玩法代码
├── SubGame/
│   └── {GameName}/
│       ├── Scripts/
│       │   ├── Core/
│       │   ├── Data/
│       │   ├── Input/
│       │   ├── Runtime/
│       │   ├── View/
│       │   ├── Procedure/
│       │   ├── UI/
│       │   ├── Config/
│       │   ├── TestMode/                   # 该子游戏的测试页入口与辅助模块
│       │   └── {GameName}GameManagerComponent.cs
│       ├── Config/
│       ├── Level/
│       ├── Pattern/
│       ├── Rule/
│       ├── Prefabs/
│       ├── Texture/
│       ├── Material/
│       ├── Entity/
│       ├── UI/                            # 子游戏专用 UI Prefab
│       └── ScriptableObjects/
├── Audio/
│   ├── Sound/
│   └── Music/
├── ScriptableObjects/
│   ├── Common/
│   ├── Avatar/
│   ├── Reward/
│   └── Shop/
├── UI/
│   ├── Common/                            # 真正跨子游戏复用的 UI
│   ├── UIPanel/                           # 当前样例的通用 UI Prefab
│   └── Template/
├── Scenes/
│   └── SubGame/
│       └── {GameName}/
├── Configs/
├── DataTables/
└── Localization/
```

新子游戏的代码必须放在：

```text
Assets/GameMain/SubGame/{GameName}/Scripts/
```

不应把新子游戏代码直接放入通用的 `Scripts/UI`、`Scripts/Component`、`Scripts/SubGame` 或其他已有玩法目录中。`Assets/GameMain/Scripts/SubGame/` 仅用于跨子游戏复用的公共运行时辅助，例如子游戏资源加载器、子游戏注册基础设施等。

子游戏 UI 的唯一目标路径为：

```text
Assets/GameMain/SubGame/{GameName}/Scripts/UI/   # UI 逻辑
Assets/GameMain/SubGame/{GameName}/UI/           # UI Prefab 与 UI 专用资源
```

`Assets/GameMain/UI/` 只保留通用 UI；不要再创建 `Assets/GameMain/UI/SubGame/{GameName}/` 作为第二套子游戏 UI 路径。

### 4.1 开发辅助与交付目录

开发辅助内容集中在 `Assets/AAA_DevAssets/`：`Docs/` 存文档，`Tools/` 存外部辅助脚本，`Editor/` 存 Unity C# 编辑器工具，`SamplePackages~/` 存分发包及校验清单。

`SamplePackages~` 末尾的 `~` 不得省略：此目录由 Unity 忽略，不注册为游戏资源，也不会出现在 Project 面板；用示例工具的“打开示例包目录”访问，导入文件选择器默认定位这里。Assets 内仅此指定目录允许导出；操作恢复点仍在工程根目录 `Backups/`，不要迁入 Assets。

根目录保留 `README.md`、`AGENTS.md` 和本文。完整工程交付用文件系统 ZIP 并确认包含 `SamplePackages~/`，不能靠 Unity Export Package 带出被忽略目录；示例包合集不等于完整工程。

## 5. 新增子游戏规范

### 5.1 必须明确的内容

新增子游戏前，应先确定：

- 子游戏名称和唯一标识。
- `GameMode` 或等价的游戏 ID。
- 是否使用独立场景。
- 是否需要独立 Procedure。
- 是否需要独立 UI。
- 是否需要关卡配置、图案配置或规则配置。
- 是否需要对象池。
- 是否需要存档和断点恢复。
- 是否需要计时、广告和数据统计。
- 是否需要专用音频、材质、字体或多语言图片。

### 5.2 子游戏管理器

每个子游戏必须有一个管理器组件，统一负责该子游戏的运行时状态和玩法入口。

管理器应继承模板提供的基础类型：

```text
SubGameManagerComponent
```

如果玩法需要对象池，应继承：

```text
PooledSubGameManagerComponent<T>
```

管理器至少需要明确处理：

- `GameStart`
- `PauseGame`
- `ResumeGame`
- `Restart`
- `GameOver`
- `ReturnMenu`
- `ResetGame`

对象池、输入、关卡加载、视图构建和存档恢复应由子游戏管理器或其内部模块负责，不应散落到多个全局组件中。

### 5.3 子游戏注册

子游戏完成后必须注册到统一子游戏注册器，至少包括：

- 游戏 ID。
- 管理器类型或实例。
- 对应 Procedure。
- 对应场景或场景 ID。
- 是否默认入口。
- 是否需要预加载配置。

子游戏首次进入所需资源必须由场景切换流程统一初始化。`ProcedureChangeScene` 的加载页生命周期应覆盖：

1. 旧场景卸载。
2. 新场景加载。
3. 当前目标子游戏的 `InitializeResourcesAsync()`。
4. 目标游戏 Procedure 进入前的必要资源准备。

不要在目标游戏 Procedure 进入后才开始加载首屏必需资源，否则会出现加载页已关闭、游戏首屏仍在等待配置或 Prefab 的问题。游戏 Procedure 可以保留幂等的资源初始化调用作为兜底，但正常路径下资源应已在切场景阶段完成。

注册完成后必须验证：

- 能否从主界面进入。
- 能否正常加载资源。
- 能否进入和退出游戏。
- 能否暂停、恢复、重启。
- 能否返回主界面。
- 是否正确清理对象、事件和对象池。

### 5.4 新增子游戏文件落点

新增子游戏时，脚本、资源和配置必须按归属落到固定目录：

```text
Assets/GameMain/SubGame/{GameName}/Scripts/             # 子游戏脚本
Assets/GameMain/SubGame/{GameName}/Config/              # 子游戏 JSON、bytes 等自定义配置
Assets/GameMain/SubGame/{GameName}/Prefabs/             # 子游戏玩法 Prefab
Assets/GameMain/SubGame/{GameName}/Entity/              # 子游戏实体 Prefab
Assets/GameMain/SubGame/{GameName}/Texture/             # 子游戏贴图
Assets/GameMain/SubGame/{GameName}/Material/            # 子游戏材质
Assets/GameMain/SubGame/{GameName}/ScriptableObjects/   # 子游戏专用 SO
Assets/GameMain/SubGame/{GameName}/UI/                  # 子游戏 UI Prefab
Assets/GameMain/Scenes/SubGame/{GameName}/              # 子游戏场景
Assets/GameMain/Audio/Sound/                            # 全局音效
Assets/GameMain/Audio/Music/                            # 全局音乐
```

全局框架配置和注册表放在：

```text
Assets/GameMain/ScriptableObjects/
```

例如 `SubGameAssetRegistryConfig.asset` 属于全局注册索引；目标架构中各游戏的资源注册明细应拆成子游戏目录内的独立配置资产，由全局索引引用（见第 5.8 节）。`RectMatch_LockStep.asset`、`RectMatchRegionSkinSchemeDataBase.asset` 这类玩法 SO 属于 `SubGame/RectMatch/ScriptableObjects/`。

新增子游戏后应在自己的资源清单中维护明细，再接入全局资源注册索引，至少登记：

- 子游戏名称。
- 子游戏 UI Form。
- 子游戏场景。
- 子游戏专用 SO 的 `AssetName` 和 `Category`。

业务代码不应通过硬编码 `GetSubGameAsset("GameName", ...)` 来声明归属；应通过注册表、资源键或带作用域的资源名让 `AssetUtility` 统一解析。

### 5.5 子游戏存档模块

全局存档入口使用 GF 自定义组件，而不是普通静态单例。业务层通过 `GameEntry.SaveData` 访问存档服务，底层再由 `SaveDataComponent` 使用 `GameEntry.Setting` 读写 KV 数据。

新增子游戏不得创建或恢复全局 `GameData.cs`，也不得在公共存档类中追加子游戏字段、事件或默认值。新代码必须直接访问对应子游戏存档模块。

子游戏专用存档必须放在：

```text
Assets/GameMain/SubGame/{GameName}/Scripts/Data/{GameName}GameData.cs
```

存档模块应实现：

```csharp
public sealed class HexaAwayGameData : IGameSaveData
{
    public void Load() { }
    public void Save() { }
}
```

新增流程：

1. 在 `SubGame/{GameName}/Scripts/Data/` 下创建 `{GameName}GameData.cs`。
2. 实现 `IGameSaveData`，在模块内部集中维护 PlayerPrefs/Setting key、默认值、属性写入和必要事件。
3. 在所属游戏管理器的 `InitializeModule(SaveDataStore store)` 中注册模块（不要修改公共 Store 构造函数）：

```csharp
if (store.Get<HexaAwayGameData>() == null)
    store.Register(new HexaAwayGameData());
```

4. 业务代码通过类型查询访问：

```csharp
var data = GameEntry.SaveData.Get<HexaAwayGameData>();
data.CurrentLevelIndex = 0;
```

5. 不允许为了省事增加全局兼容属性；调用方应显式选择 `GameEntry.SaveData` 或 `GameEntry.SaveData.Get<T>()`。
6. 新增存档字段后必须验证：首次启动默认值、已有存档读取、写入后重启仍能恢复、事件订阅不会重复触发。

存档 key 必须带子游戏前缀，例如：

```text
HexaAwayCurrentLevelIndex
HexaAwayHammerCount
RectMatchCurrentLevel
```

不要使用 `CurrentLevel`、`HintCount` 这类无作用域 key，避免多个子游戏写入同一个底层字段。

### 5.6 子游戏测试模式

#### 当前状态

当前已实现按游戏归属的测试模块生命周期：

- 通用组件持有 `TestModeModuleRegistry`，只创建 Common 下的 Ads、Runtime 模块；`TestModeUI.BindModules()` 负责显示，不引用具体玩法。
- HexaAway 和 RectMatch 各自的 `Scripts/TestMode/` 提供页面入口，由自己的 Procedure 在资源就绪后注册、退出时注销。
- 注册键为 `(OwnerId, ModuleName)`；注销使上下文失效并取消请求，旧实例不能注销重新进入后的新模块。
- HexaAway 远程配置模块已迁入其游戏目录，保留原 GUID。每次 await 后验证会话，退出后不应用迟到结果。

当前还提供示例包工具来同步其他公共注册，不能绕过工具直接删目录。新增模块操作见 [SubGame Modules](Assets/AAA_DevAssets/Docs/04_SUBGAME_MODULES.md)。

#### 归属与扩展方式

1. 通用测试框架保留在 `Assets/GameMain/CustomComponents/TestModeComponent/`：窗口开关、浮动入口、页面/控件 Prefab、模块接口及通用 Ads、Runtime 功能不属于 HexaAway 包。
2. 每个子游戏必须提供自己的测试页面入口模块，例如 `Assets/GameMain/SubGame/{GameName}/Scripts/TestMode/{GameName}TestModeModule.cs`，复用 `ITestModeModule`。暂时没有修改玩法状态的测试项时，也应提供所属游戏、当前状态等基础诊断信息，不复制其他游戏的按钮。
   一个页面入口可以组合多个同目录辅助模块，不要求把全部功能压进同一文件。HexaAway 的选关、皮肤切换、远程关卡配置均由 HexaAway 自己持有；专用 UI、SO 等仍放在对应子游戏资源分类下。
3. `Assets/AAA_DevAssets/Editor/` 只承载包检查、删除和导入等编辑器工具，不承载运行时测试模块。移除示例包不得顺带删除通用测试框架。
4. 复用现有模块接口和子游戏生命周期，补足模块归属、注册/注销和上下文绑定，不新增全局测试 Manager 或为每个子游戏复制一套窗口。
5. 公共测试组件不应引用具体子游戏类型、存档字段或专用事件名。由所属子游戏提供关卡范围、当前关卡和操作回调；不能默认使用 HexaAway 数据代表其他子游戏。
6. `TestModeModules.cs` 不再作为所有玩法模块的集中容器；最多保留真正通用的模块和辅助实现。各子游戏自行提供模块实例，公共 UI 不再逐个实例化具体玩法的测试类。

#### 可用性与生命周期

- 未安装的子游戏不注册测试模块；已安装但未进入或资源尚未就绪时，不允许重建关卡、切换皮肤或应用远程配置。若需要主菜单选关，应显式提供仅修改该游戏存档的能力，不调用未就绪的运行对象。
- 模块使用稳定的归属与标识，例如 `(GameName, ModuleName)`，重复初始化或再次进入不能重复注册。按游戏注销时只移除该游戏的模块，不能用 `ClearModules()` 误删 Ads、Runtime 等公共模块。
- 模块构建会随 UI 刷新重复执行；事件订阅、会话创建和远程请求不能无条件放在 `Build()` 中重复触发。
- 子游戏退出、切换或上下文失效时，必须解绑原存档/选关事件、撤销专用回调并释放运行对象引用。重新进入后使用新上下文，不沿用其他游戏的关卡范围、皮肤或选择状态。
- 远程关卡请求必须支持取消或失效会话检查；每次 `await` 后应用结果前重新确认所属游戏、会话及管理器仍然有效。不能只在请求发起前检查一次，再向退出后的游戏建关或刷新旧测试页。
- 未安装任何示例时，通用测试窗口在允许启用的条件下仍应可用，不出现 Hexa 专用页或读取示例存档。设置页不得依赖某个子游戏存在；测试组件/UI 缺失或未启用时入口应安全处理，且不得借导入示例绕过原有测试模式启用规则。

包工具的测试模式处理及验收矩阵见 [示例子游戏管理说明](Assets/AAA_DevAssets/Docs/05_SAMPLE_SUBGAMES.md)。

### 5.7 子游戏广告与服务器配置

#### 当前状态

当前已实现 `AdsServerConfig.Register/Unregister/TryGet` 和 `SubGameServerConfigRegistry`。两个游戏各自持有 `Scripts/Config/{GameName}ServerConfig.cs`，管理器 Awake 注册、销毁时按实例注销；预加载先完成、游戏后注册时仍会加载配置，SDK 就绪会刷新此前的缺省值。

以下 7 个字段明确属于 `AdsServerConfig.Common`，不放在子游戏接口或配置类中：`LevelTimerIdleStopSeconds`、`TaskUnlockLevel`、`RankUnlockLevel`、`ShopUnlockLevel`、`FirstWinUnlockLevel`、`ShowFirstWinClaim2Button`、`EnableShop`。广告不可用面板开关和全局每日奖励额度也保持公共。

主玩法在启动场景 `GameEntry/GameFramework/GameManager` 的 `GameManagerComponent` Inspector 中设置；当前保存为 HexaAway。公共排行、首胜等默认使用该主玩法进度，不能因为进入副玩法就切换进度来源；未配置/未注册主玩法时关卡判断失败。关卡广告策略仍按明确的所属游戏读取。首页主按钮也跟随此项；副按钮在 MainUIPanel Prefab 的 `Secondary Game Mode` 单独设置，缺失/相同模式不显示。

兼容边界及原有未接入规则见 [SubGame Modules](Assets/AAA_DevAssets/Docs/04_SUBGAME_MODULES.md)：任务系统未启用旧关卡门槛，商店仍沿用独立的旧入口阈值，本次不顺便改变这些玩法行为。

#### 目标约束

1. 每个子游戏使用独立脚本定义自己的配置字段、默认值、Key 映射和解析，例如 `Assets/GameMain/SubGame/{GameName}/Scripts/Config/{GameName}ServerConfig.cs`。采用 `ServerConfig` 命名是因为当前入口还承载非广告远程字段；如果该模块只负责广告，也可命名为 `{GameName}AdsConfig`，不要求重复维护两套相同字段。
2. `AdsServerConfig` 保留统一入口职责，通过公共配置契约及游戏标识管理多份实例，提供注册、加载、查询和注销。不得新增 `HexaAwayConfig`、`RectMatchConfig` 等具体类型静态属性，或在其中硬编码各玩法配置构造函数。
3. 上述 7 个公共功能/计时字段及全局广告不可用配置保持一份公共实例，副玩法无需重复实现。各游戏的广告策略仍独立保存；`RectMatchInfinitePropDisplayMaxLevel` 仅由 RectMatch 持有。不要把“子游戏配置独立”误解为所有公共功能必须复制到每个游戏。
4. 子游戏代码查询自己的强类型配置；公共广告逻辑按显式游戏标识读取策略，公共功能读取 `Common` 并使用已配置的主玩法进度。两者不能混用当前活动游戏与主玩法。公共层不直接引用具体玩法配置类，未知或未安装游戏必须明确查询失败，不允许自动回退到 HexaAway。
5. 沿用当前 SDK 配置来源和初始化流程，不新建广告系统。先注册已安装游戏的配置，再分发可用配置数据；若 SDK 数据先到或模块后注册，应能使用已获取的数据完成初始化，不能让全局 `s_IsLoaded` 使后注册游戏永久跳过加载。
6. 每个模块定义缺失、类型错误和越界值的处理方式。迁移时记录并核对现有字段初始化值、`Load()` 的缺省值及生效时机；不以拆分为由顺手统一不同默认值。`false`、`0` 等合法值不能被当成“未配置”。
7. 新增远程 Key 应有子游戏作用域；现有线上 Key 必须先记录原名称和归属。拆分脚本不自动授权修改服务端协议；需要新旧 Key 映射时，由所属模块显式兼容并验证，不让多个游戏误用同一旧 Key。
8. 测试页修改配置时只作用于自己的实例。公共字段的测试覆盖应走公共测试模块，不允许一个游戏的测试页无意改写另一个游戏的配置。退出后的临时覆盖如何还原必须明确；远程刷新与测试覆盖的优先级也必须固定。

### 5.8 子游戏资源注册清单

#### 当前状态

当前已完成序列化迁移：全局 `SubGameAssetRegistryConfig.asset` 通过 `m_Manifests` 引用两份游戏自有的 `SubGameAssetManifest` 资产；旧 `m_Entries` 仅保留为隐藏的迁移入口，当前列表为空，运行时遇到旧条目会报错提示迁移。

索引重建先完整验证，再替换映射。空列表合法，Missing 清单、重复游戏标识和同类重复资源键会报错，不再静默覆盖；按游戏注销会清除其 UI、Scene、SO 映射。资源名仍遵守现有全局唯一协议，尚未改为跨游戏同名复合键。

清单保存逻辑名而非玩法资源直接引用；当前与全局索引一起分配至 `ResourceCollection.xml` 的 `ScriptableObjects` 启动元数据资源组，避免根索引跨包依赖整个玩法包。构建发布资源前必须重新构建 GF 资源。

#### 目标约束

1. 每个子游戏持有一份独立资源清单，例如 `Assets/GameMain/SubGame/{GameName}/ScriptableObjects/Registry/{GameName}AssetRegistryConfig.asset`，声明游戏标识和该游戏 UI、场景、SO 的逻辑名/类别。若需要专用注册适配脚本，也应放在该游戏的 `Scripts/Config/` 内。
2. 资源清单结构相同时复用一个公共 SO 类型，不为了文件归属复制两份完全相同的 C# 字段结构。独立配置资产才是各游戏资源明细的唯一维护来源，不能同时在公共脚本或另一张总表里重复填写。
3. 全局 `Assets/GameMain/ScriptableObjects/SubGameAssetRegistryConfig.asset` 只维护已安装子游戏清单的引用；公共 `SubGameAssetRegistryConfig` 负责聚合注册，`SubGameAssetRegistry` 保留集中查询职责，不按游戏增加 `switch` 或专用分支。
4. 由既有预加载流程加载全局索引及其清单，再进入需要资源域解析的流程。索引和清单使用不依赖自身注册结果的明确启动加载入口，避免“先查注册表才能加载注册表”。清单优先保存逻辑名等元数据，不因直接引用全部玩法 Prefab/SO 而把所有游戏资源一起预加载。
5. 复用 `Tools/SubGame/Asset Registry Config` 维护索引和各游戏清单。迁移前备份原 `m_Entries`，逐项核对拆分结果，更新 Editor 显示与预加载链路；不能直接改序列化字段后让现有配置丢失。
6. 保留当前加载协议期间，各类资源的 `AssetName` 仍须满足现有全局唯一约束；聚合时检测同名、重复游戏标识及缺失清单，报告归属和冲突来源，禁止静默覆盖。若要改成带作用域的复合键，必须单独批准并迁移所有查询方，不能把“拆清单”当成已完成协议改造。
7. 移除包时先撤销全局清单引用，再移除专用资源，并重建运行时索引以清除旧映射；重复导入不能出现重复引用。根索引为空是合法的空模板状态，有残留的空引用或 Missing Script 则是配置错误。
8. 独立资源清单不能替代 UIForm、Scene DataTable、Procedure、Build Settings 或资源收集注册。音频仍按全局音频目录约定管理，不能随本次拆分改变音频目录规则。

## 6. 新增 UI 页面规范

### 6.1 UI 文件归属

通用 UI 放入：

```text
Assets/GameMain/Scripts/UI/
Assets/GameMain/UI/
```

子游戏专用 UI 放入：

```text
Assets/GameMain/SubGame/{GameName}/Scripts/UI/
Assets/GameMain/SubGame/{GameName}/UI/
```

当前 `Tools/UI/UI Panel Manager` 仅会生成通用 UI 到 `Assets/GameMain/Scripts/UI/Panel/` 和 `Assets/GameMain/UI/UIPanel/`。在工具支持 `GameName` 参数前，它不能作为子游戏 UI 的脚手架；新增子游戏 UI 时应按上述目标路径创建，并手工完成注册。不要为了使用该工具而把子游戏 UI 放回公共目录。

### 6.2 新增页面必须同步的内容

新增 UI 页面必须检查：

1. UI Panel 脚本。
2. UI Prefab。
3. UIForm ID 定义。
4. UIForm 文本配置。
5. UIForm 二进制资源。
6. UI 分组。
7. 是否暂停被覆盖页面。
8. 是否允许多实例。
9. 页面打开和关闭逻辑。
10. 页面需要的多语言文本和图片。

如果项目提供 UI 自动生成工具，优先使用工具，不要手工修改生成文件。

工具生成的范围必须与资源归属一致。若工具不能生成到子游戏资源域，应先在文档中说明其限制，或扩展工具后再将其列为子游戏 UI 的标准流程。

### 6.3 UI ID 规划

模板项目应先确定 `UIFormId` 的底层数值类型，再发布固定的 ID 区间并提供冲突检查。

> **当前样例状态**：`UIFormId` 的底层类型是 `byte`，合法范围仅为 `0-255`；因此不能采用 `300-399`、`400-499` 一类区间。现有 RectMatch 和 HexaAway 位于 `200` 段。维护当前样例时应通过 UI Panel Manager 或 `UIForm.txt` 确认可用 ID，且不得超过 `255`。

如果下一代模板需要 `300+` 的区间，必须先把 ID 的底层类型及相关 DataTable/生成/序列化链路作为一项明确的框架变更完成验证；不要只修改 ID 规划文档或枚举值。

### 6.4 编辑器工具入口

所有新增编辑器工具的 Unity 菜单路径统一使用：

```text
Tools/{Category}/{ToolName}
```

例如：

```text
Tools/SubGame/Asset Registry Config
Tools/UI/UI Panel Manager
Tools/DataTable/Generate DataTables
```

新增编辑器工具除声明 `MenuItem` 外，还必须同步注册到：

```text
Assets/GameMain/Editor/CustomToolBars/CustomToolBars.cs
```

具体注册位置为 `ShowToolsMenu()`，保证常用工具能从顶部 Toolbar 的 `Tools` 下拉菜单进入。工具菜单分类名称应与 `Tools/{Category}/{ToolName}` 的 `Category` 保持一致，避免同一个工具在 Unity 菜单和 Toolbar 中使用不同归类。

> **当前样例状态**：`Tools/UI/UI Panel Manager` 已提供 Unity 菜单入口，但尚未出现在 `CustomToolBars.ShowToolsMenu()` 中。它是现有例外，不应被误判为工具不可用；新建工具仍应同时完成菜单与 Toolbar 注册。

## 7. ScriptableObject 规范

### 7.1 继承类型选择

使用 `IdOnlyConfigSO` 的情况：

- 只有逻辑 ID。
- 不需要通用显示名称和图片。
- 关卡、规则、数值、锁步或玩法配置。

使用 `DisplayConfigSO` 的情况：

- 配置需要显示名称。
- 配置需要展示图片。
- 配置会出现在头像、商店、奖励或物品 UI 中。

如果配置同时需要 ID、显示名称和图片，应优先继承 `DisplayConfigSO`，不要重复定义这些基础字段。

### 7.2 资源路径

子游戏专用 ScriptableObject 配置属于子游戏资源域，统一放入：

```text
Assets/GameMain/SubGame/{GameName}/ScriptableObjects/
```

子游戏关卡、图案、规则等资源应进一步分类：

```text
Assets/GameMain/SubGame/{GameName}/ScriptableObjects/Level/
Assets/GameMain/SubGame/{GameName}/ScriptableObjects/Pattern/
Assets/GameMain/SubGame/{GameName}/ScriptableObjects/Rule/
Assets/GameMain/SubGame/{GameName}/ScriptableObjects/Database/
```

全局通用 SO 和框架级配置不放入子游戏目录，例如：

```text
Assets/GameMain/ScriptableObjects/SubGameAssetRegistryConfig.asset
Assets/GameMain/ScriptableObjects/Avatar/
Assets/GameMain/ScriptableObjects/Reward/
Assets/GameMain/ScriptableObjects/Shop/
Assets/GameMain/ScriptableObjects/Common/
```

禁止将新增 SO 直接堆放在 `ScriptableObjects` 根目录或 `Create` 临时目录中。

### 7.3 CreateAssetMenu

`CreateAssetMenu` 的菜单路径应与资源分类一致，例如：

```text
GF/SubGame/{GameName}/Level
GF/SubGame/{GameName}/Pattern
GF/Common/Avatar
GF/Common/Reward
```

代码菜单路径、实际资源目录、运行时加载路径必须保持一致或有明确映射。

### 7.4 子游戏资源自包含原则

子游戏专用资源应尽量与子游戏放在同一资源域中，包括：

- 玩法 Prefab。
- 子游戏实体 Prefab。
- 子游戏材质和贴图。
- 子游戏专用 UI Prefab。
- 子游戏专用粒子、动画和特效。

音效和音乐统一放在全局音频目录中，不按子游戏物理拆分目录。音频是否属于某个子游戏，应通过 `SoundId`、DataTable、资源注册表或等价配置表达，而不是通过目录位置推断。

公共目录只保留真正被多个系统或多个子游戏共享的资源；音频目录作为全局音频资源入口统一维护，例如：

```text
Assets/GameMain/Audio/Sound/
Assets/GameMain/Audio/Music/
Assets/GameMain/UI/Common/          # 通用 UI Prefab
Assets/GameMain/Entities/Common/    # 通用实体 Prefab
```

子游戏资源应放入：

```text
Assets/GameMain/SubGame/{GameName}/Prefabs/
Assets/GameMain/SubGame/{GameName}/Entity/
Assets/GameMain/SubGame/{GameName}/UI/
```

这样可以在不携带其他子游戏资源的情况下迁移、裁剪或扩展一个子游戏。

### 7.5 资源加载必须支持资源域

资源移动到子游戏目录后，不能继续假设所有 Prefab、Entity、材质、贴图等资源都位于公共根目录。模板项目必须在资源加载层引入资源域或资源作用域概念：

```text
Common
SubGame/{GameName}
```

资源引用应使用稳定的逻辑资源键或带作用域的资源路径，例如：

```text
Common/Audio/Sound/UI_Click
Common/Audio/Sound/RectMatch_LevelStart
SubGame/RectMatch/Prefabs/Cell
SubGame/RectMatch/Entity/Animal
```

业务代码不应自行拼接 `Assets/GameMain/...` 路径。应由统一的资源路径工具或资源注册表将逻辑键解析为实际资源路径。

资源表建议记录以下信息：

- 资源 ID 或资源键。
- 资源类型。
- 资源域：公共或具体子游戏。
- 资源相对路径。
- 资源组或加载优先级。

如果资源表暂时只能保存 `AssetName`，则需要增加 `Scope`、`GameName` 或完整 `AssetPath` 字段，不能通过调用者上下文猜测资源所属子游戏。

例如音效调用仍可以保持：

```csharp
GameEntry.Sound.PlaySound(SoundId.LevelStart);
```

但 `SoundId.LevelStart` 对应的配置应明确解析到：

```text
Assets/GameMain/Audio/Sound/RectMatch_LevelStart.ogg
```

公共 UI 调用的 `UI_Click` 则解析到：

```text
Common/Audio/Sound/UI_Click
```

### 7.6 资源加载层的改造边界

模板项目应优先修改集中式加载入口，而不是修改所有业务调用点：

1. 资源路径工具支持公共域和子游戏域。
2. 音效扩展根据资源表或音效配置解析全局音频目录中的资源名。
3. UI 扩展根据 UI 注册信息解析 Prefab 路径。
4. Entity 扩展根据实体注册信息解析 Prefab 路径。
5. ScriptableObject 和配置加载器支持子游戏路径。
6. 资源收集、Addressables 或 AssetBundle 分组使用相同的逻辑键。

这样调用方仍然通过 `SoundId`、`UIFormId`、Entity ID 或配置 ID 使用资源，具体目录变化不会扩散到所有业务代码。

### 7.7 资源迁移和裁剪验证

一个子游戏从模板项目中独立迁移或裁剪时，必须验证：

- 子游戏资源不存在对其他子游戏资源的隐式引用。
- 子游戏资源不存在对公共目录中非公共资源的引用。
- 所有音效、Prefabs、实体、材质和配置都能通过逻辑键加载。
- 资源收集配置不会把其他子游戏一并打包。
- 删除其他子游戏后，模板仍能编译并进入当前子游戏。

## 8. 配置、场景和 DataTable

新增子游戏时，应逐项确认：

- 是否新增场景。
- 是否新增场景 ID。
- 是否更新场景 DataTable。
- 是否新增 Procedure。
- 是否更新 Procedure 状态切换。
- 是否新增 Config 或 CustomConfig。
- 是否更新预加载列表。
- 是否需要生成 `.bytes` 文件。
- 是否需要加入资源收集或 Addressables。
- 是否需要新增音频、实体或 UI 资源配置。

文本配置和二进制配置必须同时更新，不能只修改源文件。

### 8.1 源文件与生成文件

模板必须明确每类生成物的来源、生成入口和是否提交版本库。当前样例中至少应遵循：

| 源文件 | 生成物 | 当前入口 | 规则 |
| --- | --- | --- | --- |
| `Assets/GameMain/DataTables/UIForm.txt` | `UIForm.bytes`、`Scripts/UI/Runtime/UIFormId.cs` | `Tools/UI/UI Panel Manager` | 不要只手改其中一个；修改后必须重新生成并检查 ID。 |
| DataTable 文本源 | 对应 `.bytes` 与生成代码（如适用） | `Tools/DataTable/Generate DataTables` | 以实际生成器输出为准，不手工伪造二进制内容。 |

场景加入 Build Settings、场景 DataTable、资源注册表和 Procedure 入口是独立步骤；其中任一项缺失，都不能视为“场景已注册”。

## 9. 多语言规范

### 9.1 文本 Key

新增文本必须：

- 使用稳定、具有业务含义的 Key。
- 先添加源语言文本。
- 同步检查所有目标语言。
- 保持各语言 Key 集合一致。
- 更新自动生成的 Key 文件或手工 Key 文件。
- 验证运行时能正确加载。

### 9.2 字符集和 TMP SDF

新增或修改中文、繁体中文、日文、韩文等文本后，必须检查对应字符集：

```text
unity_sdf_charset_CNS.txt
unity_sdf_charset_CNT.txt
unity_sdf_charset_JP.txt
unity_sdf_charset_KR.txt
```

必要时还应检查 Arabic、Hindi、Thai 等字符集。

流程为：

1. 收集新增文本中的字符。
2. 与对应字符集比较。
3. 补充缺失字符。
4. 重新生成或更新 TMP SDF 字体资源。
5. 在目标语言下验证显示效果。

### 9.3 多语言图片

包含文字的图片必须按语言存放：

```text
Assets/GameMain/Localization/{Language}/Images/{ImageName}.png
```

新增多语言图片时必须检查：

- 图片名称是否在所有语言中一致。
- 是否所有目标语言都有资源。
- 是否允许使用默认语言回退。
- Prefab 是否通过多语言加载逻辑获取图片。
- 资源是否被正确收集和打包。

## 10. 资源路径工具规范

资源路径不能在业务代码中重复拼接。模板项目应集中维护路径工具，并按资源归属提供明确方法：

```csharp
GetCommonConfigAsset(...)
GetSubGameConfigAsset(gameName, ...)
GetSubGameScriptableObjectAsset(gameName, category, ...)
GetLocalizationImage(language, ...)
```

路径工具应避免把所有资源都假设在同一个根目录下。新增子游戏资源必须能够通过 `gameName` 区分。

## 11. 新增子游戏检查清单

### 设计

- [ ] 已定义游戏名称和唯一 ID。
- [ ] 已确定通用系统与子游戏系统边界。
- [ ] 已确定场景、Procedure 和 UI 需求。
- [ ] 已确定配置类型和资源分类。

### 代码

- [ ] 代码位于 `SubGame/{GameName}/Scripts`。
- [ ] 已实现子游戏管理器。
- [ ] 已创建 `{GameName}GameData` 并注册到 `SaveDataStore`。
- [ ] 已实现生命周期和清理逻辑。
- [ ] 已注册子游戏。
- [ ] 已注册 Procedure。
- [ ] 已处理输入、对象池、存档和事件。
- [ ] 已提供该游戏自己的测试页面入口模块，声明归属、可用条件和注册/注销生命周期；公共测试代码不依赖该游戏的具体类型。
- [ ] 已提供该游戏的广告/服务器配置脚本并接入统一入口，专用字段、默认值和 Key 映射不在公共类中堆叠。

### 资源

- [ ] 子游戏资源位于 `SubGame/{GameName}`。
- [ ] SO 位于对应子游戏目录。
- [ ] 音效和音乐位于 `Audio/Sound` 或 `Audio/Music`，并通过配置声明用途或归属。
- [ ] 通用资源没有错误放入子游戏目录。
- [ ] 场景和资源收集配置已更新。
- [ ] 资源注册明细由该游戏独立配置资产持有，全局索引只引用清单，没有重复明细或资源键覆盖。

### UI

- [ ] UI 脚本和 Prefab 已创建。
- [ ] UI ID 已分配且无冲突。
- [ ] UIForm 文本和二进制文件已同步。
- [ ] UI 分组和暂停行为已确认。

### 多语言

- [ ] 文本 Key 已同步。
- [ ] 所有目标语言已处理。
- [ ] 字符集已更新。
- [ ] TMP SDF 字体已验证。
- [ ] 多语言图片已处理。

### 验证

- [ ] 能从主界面进入子游戏。
- [ ] 能正常加载全部配置和资源。
- [ ] 能暂停、恢复、重启和退出。
- [ ] 退出后无残留事件、对象或对象池对象。
- [ ] 测试模式在未安装、单游戏及双游戏状态下均安全；重复进入无重复模块，退出后的远程测试结果不会继续应用。
- [ ] 广告/服务器配置在缺失、异常、SDK 先就绪及模块后注册时正确初始化；游戏之间的策略值和测试覆盖不会串用。
- [ ] 各目标语言下 UI 和字体显示正常。

## 12. 模板项目后续应实现的工具

本文档是规范基础，后续模板项目可以逐步提供以下工具：

1. 子游戏脚手架生成器。
2. 子游戏注册器。
3. 子游戏资源路径工具。
4. UI 页面生成和注册工具。
5. UI ID 冲突检查工具。
6. SO 分类创建工具。
7. 多语言 Key 一致性检查工具。
8. 字符集缺失字符检测工具。
9. 多语言图片完整性检查工具。
10. 新增子游戏完整性检查工具。

工具应优先服务于规范，不能通过工具继续制造新的隐式目录和隐式注册点。

## 13. 禁止事项

- 不要把新子游戏代码直接放入通用目录。
- 不要把子游戏配置直接放入 ScriptableObjects 根目录。
- 不要把通用头像、奖励、商店资源复制到子游戏目录。
- 不要只修改 XML 而不检查字符集和字体。
- 不要只新增 UI 脚本而不更新 UIForm 配置。
- 不要在多个业务脚本中重复硬编码资源路径。
- 不要新增没有注册流程的子游戏。
- 不要新增没有清理生命周期的全局管理器。
- 不要为了新增一个子游戏复制一套全局系统。
- 不要因为兼容历史代码而让新模板继续沿用不明确的目录规则。

## 14. 兼容旧项目的原则

本文档用于新模板项目，不要求当前项目立即迁移。

如果未来需要将旧项目逐步迁移到模板规范，应遵循：

1. 新功能优先使用新目录和新注册机制。
2. 旧资源路径保留兼容层。
3. 不为了统一目录一次性移动大量资源。
4. 每次迁移都要验证 Unity 引用、AssetBundle、Addressables 和运行时加载。
5. 迁移属于独立任务，不能混入普通功能开发。
