# Configuration 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. Config、DataTable、CustomConfig

**功能选择**：

| 入口 | 放什么 | 配置 / 读取 |
| --- | --- | --- |
| Config | 场景 ID 等基础键值 | `Configs/DefaultConfig.txt`；`GameEntry.Config.GetInt("Scene.Menu")` 等按实际键和类型读取 |
| DataTable | UIForm、Scene、Sound、Entity 等结构化行表 | `DataTables/*.txt → *.bytes` 与 `DR*.cs`；`GetDataTable<T>().GetDataRow(id)` |
| CustomConfig | 公共 SO 配置引用和 JSON 缓存 | 预加载设置 SO；JSON 先 `LoadJsonConfig<T>`，成功后 `TryGetJsonConfig<T>` |

**新增 DataTable**：

1. 按现有四行表头和 Tab 格式建立文本，列名/类型/ID 保持可生成。
2. 在 `ProcedurePreload.DataTableNames` 增加表名（当前生成菜单也遍历此列表）。
3. 执行 `Tools/DataTable/Generate DataTables`，检查 `.bytes` 和 `Lokas.DR{表名}` 代码；加入资源收集。
4. 等 `LoadDataTableSuccessEventArgs` 或对应就绪流程完成后读取；处理表不存在/行不存在。

UIForm 还要生成 `UIFormId.cs`，应使用 [UI 同步入口](../UI/PageWorkflow.md#3-添加页面与注册流程)。不要只运行普通表生成便认为枚举同步了。

```csharp
var table = GameEntry.DataTable.GetDataTable<DRUIForm>();
var row = table?.GetDataRow((int)UIFormId.SettingUIPanel);
if (row != null)
{
    UnityEngine.Debug.Log(row.AssetName);
}
```

**新增 SO/JSON**：SO 按字段职责选已有 IdOnlyConfigSO / DisplayConfigSO 类型并归入正确资源域；放进目录不会自动加载，需接入所属模块的加载入口。CustomConfig 的 JSON 缓存按“类型 + configName”区分，读取方法不会自动发起加载。数据校验由配置/业务实现负责，不把反序列化成功等同于配置有效。

**验收**：核对源文件、生成物、预加载名单、加载成功事件和业务读取值；修改源表后不要在旧 Play 会话的缓存中判断生成是否生效。

依据：[DataTableExtension](../../../../GameMain/Scripts/DataTable/DataTableExtension.cs)、[生成菜单](../../../../GameMain/Scripts/Editor/DataTableGenerator/DataTableGeneratorMenu.cs)、[CustomConfigComponent](../../../../GameMain/Scripts/Component/CustomConfigComponent.cs)、[配置规范](../../../../../GF_FRAMEWORK_TEMPLATE_GUIDELINES.md#8-配置场景和-datatable)。
