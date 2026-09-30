# Resource 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. ResourceComponent 与资源域

**功能**：ResourceComponent 管实际加载/释放；AssetUtility 管路径规则；注册表声明逻辑名归属。资源域注册不等于资源构建收集，也不等于运行时玩法注册。

| 资源 | 当前路径解析 |
| --- | --- |
| UI | 活动页面表 → 子游戏 UI 清单 → 公共 UIPanel |
| 场景 | 子游戏 Scene 清单 → 公共 Scenes |
| SO | 子游戏 SO 清单（游戏 + Category）→ 公共 ScriptableObjects |
| Entity / 自定义 JSON | 支持已识别的子游戏限定资源名，否则走公共目录 |
| 音乐 / 音效 | 公共 `Audio/Music` / `Audio/Sound`，当前路径工具使用 `.ogg` |

**新增步骤**：先确定公共/游戏/活动归属，再创建资产；需要资源域解析的登记清单，按既有流程聚合；最后检查 GF Resource Editor/Builder 的收集和构建产物。不要把绝对 Assets 路径拼在业务代码里。

**使用**：加载 SO 可调用 `GameEntry.Resource.LoadSO(logicalName, callbacks)`，内部仍走 AssetUtility。需要显式类型时用 `LoadAsset(AssetUtility.GetScriptableObjectAsset(logicalName), typeof(...), callbacks)`。成功回调检查实际类型，失败回调报告资源键与错误。

**释放与验收**：直接加载的资源由拥有者管理生命周期；UI/Entity 管理的实例走各自关闭/隐藏接口。异步完成晚于页面/游戏退出时使结果失效并释放自己持有的资源。Editor 模式能加载不是构建模式通过，新增资源需从真实入口核对路径和收集。

依据：[AssetUtility](../../../../GameMain/Scripts/Utility/AssetUtility.cs)、[ResourceExtension](../../../../GameMain/Scripts/Utility/ResourceExtension.cs)、[资源清单接入](../SubGame/Extensions.md)。
