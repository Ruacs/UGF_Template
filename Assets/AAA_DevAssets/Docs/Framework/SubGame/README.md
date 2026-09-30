# SubGame 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

子游戏框架负责已注册玩法的发现、路由、资源准备和生命周期协作。关卡规则、道具、游戏专属配置、存档及页面由具体游戏实现。HexaAway、RectMatch 是示例实现，不是框架成立的前提。

## 接入与使用

1. 按[新子游戏接入](Integration.md)确定资源域、GameMode、管理器、Procedure、场景、存档和页面注册。
2. 主菜单通过既有 ProcedureMenu 路由；运行时查询 `GameEntry.SubGames.Get(mode)`，处理未注册返回空的情况。
3. 游戏自己的测试页、服务器策略、资源清单按[扩展接入](Extensions.md)注册，不在公共组件里写具体玩法 switch。
4. 启动和场景切换由 [Procedure](../Procedure/README.md) 协调；实际加载释放见 [Resource](../Resource/README.md)。

## 生命周期与验收

资源初始化 → 进入 → 暂停/恢复 → 重启 → 返回 → 再次进入；退出时清理所属页面、事件、异步工作及池对象。区分主玩法和当前玩法，公共解锁与玩法行为不能混用进度来源。

示例的打包、移除、回装见[示例包工具](../../Samples/SamplePackages.md)。该工具当前只支持预定义示例，不是任意子游戏安装器。
