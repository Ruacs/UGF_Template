# SaveData 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. SaveData 与 Setting

**功能**：SaveDataComponent 门面提供公共玩家状态和自定义模块访问；SaveDataStore 持有公共字段与按类型注册的 `IGameSaveData`。当前 Store 实现使用 PlayerPrefsManager，不应把 `GameEntry.Setting` 当成它的同一份缓存。

**新增步骤**：公共已有字段走原入口；子游戏数据放该游戏 `Scripts/Data/`，实现 IGameSaveData 并由管理器 `InitializeModule(SaveDataStore)` 注册。GameEntry 先装配游戏模块，再初始化存档。活动数据使用活动作用域存储，见 [活动接入 §4](../Activity/Integration.md#4-模块存储)。

**使用**：`GameEntry.SaveData.Get<MyGameGameData>()` 取已注册的游戏模块；`SetData<T>(key, data)` / `GetData<T>(key, defaultValue)` 用于当前支持的通用序列化数据；`SaveAll()` 保存公共状态及模块。MyGameGameData 为自己的类型，不是模板内固定类型。

**注意**：新增 Key 带游戏/模块前缀，保留已有 Key 与数值兼容；不把游戏字段加回公共 Store。不是每个属性都有相同保存时机，应读所属属性实现。不要把存档存在等同于云同步、事务或服务端可信。

**验收**：首次默认值 → 修改 → 退出重进 → 回读；同时验证缺模块、缺 Key 和旧数据兼容。测试工具重置数据前确认影响范围，不用整个 PlayerPrefs 清空来验证一个字段。

依据：[SaveDataComponent](../../../../GameMain/Scripts/Component/SaveDataComponent.cs)、[SaveDataStore](../../../../GameMain/Scripts/Component/Save/SaveDataStore.cs)、[子游戏存档流程](../SubGame/Integration.md)。
