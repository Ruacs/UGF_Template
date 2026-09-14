# 示例包实施约定

## 1. 项目背景
将现有 HexaAway / RectMatch 示例变成可独立移除、回装的 GF 模板扩展包；用户已确认方案并要求导出。
## 2. 使用者
复用本模板的新开发者；包依赖兼容版本的 GF 模板，不是独立 Unity 项目。
## 3. 需求
两个独立 unitypackage；明确拥有权；受控移除/导入；零/单/双示例均可编译；保持现有双示例玩法和存档。
## 4. 架构
复用 GameManagerComponent、SubGameManagerComponent、SaveDataStore、GF Procedure 和现有资源索引。公共层通过显式配置的游戏管理器、公共契约分发，不依赖具体游戏类型。安装清单和编辑器工具负责磁盘注册，运行时资源清单继续只负责资源域。
## 5. 技术约束
Unity 2022.3.62f3c1；Lokas / Lokas.Editor；保留原 GUID、GameMode / UI / Scene ID、资源协议和 7 个公共配置字段。只用 Unity AssetDatabase 移动资产。
## 6. 安全要求
操作前精确路径/拥有权/冲突检查；备份公共配置和原包；记录跨编译阶段状态；导入不覆盖用户修改；移除不清玩家存档；失败明确给出恢复点。
## 7. 实施顺序
1. 补齐代码/资产边界并编译、验证双示例。
2. 增加安装清单、注册同步及导出/移除/回装操作，测试冲突与幂等。
3. 按明确清单导出；在隔离项目副本验证零/单/双以及回装，不在用户工作工程删除示例。
## 8. 输出
代码、安装清单、两个 unitypackage、使用说明与验证报告。最初导出在 Assets 外的 `SamplePackages/`；2026-09-14 经确认迁至 `Assets/AAA_DevAssets/SamplePackages~/`（保留 `~` 以阻止 Unity 导入），操作备份仍在根目录 `Backups/`。文档与外部脚本同步迁入 `Assets/AAA_DevAssets/Docs/`、`Tools/`，验证见 [辅助目录迁移](DEV_ASSETS_LAYOUT_VALIDATION.md)。
## 9. 验收
公共层无具体玩法类型依赖；无跨游戏私有资产引用；仅移除所属资产；注册源/生成物同步；回装无重复；公共测试功能保留；实际 Unity 测试区分通过与未执行。
## 10. 限制
不引入新全局 Manager；不迁移 UPM；不改线上 SDK Key；不清档；不修改其他项目构建产物；本轮包功能实施时主工程保留两个示例，之后用户安装/移除状态以当前工程为准。

修改前备份：`Backups/SamplePackaging-20260914-164026/`。
