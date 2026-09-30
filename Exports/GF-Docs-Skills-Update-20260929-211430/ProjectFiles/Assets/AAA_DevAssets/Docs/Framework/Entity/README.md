# Entity 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. Entity 与 ObjectPool

**功能区别**：EntityComponent 管 GF 实体加载、显示/隐藏、附着；`GameEntry.ObjectPool` 是底层 GF 池；项目泛型 `ObjectPoolComponent<T>` 为已有 Prefab 的 Spawn/Recycle 包装，FlyItem/FloatingText 等使用它。

**Entity 接入**：

1. 参考现有 Effect / UIEffect 的逻辑与 EntityData；DREntity 的 TypeId 是配置 ID，data.Id 是本次实体 ID。
2. 创建 Prefab、登记 Entity 表并生成，配置实体组、资源路径和收集。
3. 通过已有 `ShowEffect` / `ShowUIEffect` / `ShowUIFlyEffect` 使用匹配的数据类型；新类型需要所属模块自己的调用入口。
4. 使用 `GameEntry.Entity.GenerateSerialId()` 分配当前项目的运行 ID，退出通过 HideEntity/所属组清理，不能直接 Destroy 框架拥有的实例。

当前 EntityExtension 的通用 ShowEntity 包装为 private，并用 `entityGroup + "/" + drEntity.AssetName` 解析路径。不要从 UI 资源规则推断 Entity 自动支持任意游戏路径；为新类型接入时沿着该包装确认实际解析结果。

**泛型池接入**：组件 Inspector 绑定 prefab、root、capacity、prewarmCount；T 实现 IPoolable，在 OnSpawn/OnRecycle 重置状态、解绑并停止自己的动画。池在 Start 建立，不能在其他对象 Awake 中直接 Spawn。此包装以 `typeof(T).Name` 命名池，同类型多套组件需先检查重名风险。

**验收**：重复生成/回收、场景离开、动画中途回收；ActiveCount 和回调应回到预期，无重复回收或复用旧状态。页面 Item 的池由 UGuiForm 管理，见 [UI 生命周期](../UI/README.md#2-调用方式与生命周期)。

依据：[EntityExtension](../../../../GameMain/Scripts/Entity/Core/EntityExtension.cs)、[ObjectPoolComponent<T>](../../../../GameMain/Scripts/Component/ObjectPool/Core/ObjectPoolComponent.cs)。
