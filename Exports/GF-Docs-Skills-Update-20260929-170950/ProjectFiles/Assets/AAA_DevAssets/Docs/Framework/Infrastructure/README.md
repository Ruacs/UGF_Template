# Infrastructure 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. 其他 GF 基础设施

以下是项目 GameEntry 暴露的底层入口，不表示项目已封装完整的产品级接入流程。使用前核对 Launcher 对应组件、Helper 和业务拥有者；通常复用已有上层入口。

| 入口 | 功能与基本用法 | 注册 / 生命周期 / 验收重点 |
| --- | --- | --- |
| Base | GF 基础运行设置与时间状态 | 更改全局时间会影响其他模块；玩法暂停优先走已有游戏接口 |
| Fsm | 为特定拥有者创建状态机、驱动状态切换 | 状态注册后再启动，拥有者退出销毁自己的状态机；不另建一套全局 Procedure |
| DataNode | 按路径组织临时运行数据 | 约定路径和数据类型，退出移除自己节点；不是自动持久化存档 |
| Debugger | 运行状态和调试窗口 | 核对 Launcher 配置与发布可见性；自定义业务测试页走 TestMode |
| Download | `AddDownload(path, uri)` 下载到文件 | 记录返回 serialId，处理成功/失败事件，退出按需 RemoveDownload；验证本地结果和失败重试 |
| WebRequest | `AddWebRequest(uri, ...)` 发起 HTTP | 保存 serialId/上下文、过滤响应、处理失败/取消、对称解绑；鉴权/重试仍是调用方职责 |
| Network | 创建网络通道并配置协议 Helper | 先实现项目协议编解码和连接生命周期；不等于已有账号/联网对战服务 |
| FileSystem | 创建/加载/读写 GF 文件系统 | 明确读写路径、访问模式和关闭时机；不自动接管 SaveData |

这些组件源码均位于 [UnityGameFramework Runtime](../../../../Plugins/UnityGameFramework/Scripts/Runtime)。优先读所用组件的实际方法签名；不把其他 GF 版本的示例直接当成本项目 API。
