# Optional sample games

使用 Unity 菜单 `Tools/Template/Sample SubGames` 导入、移除和重新导出。默认存放位置为 `Assets/AAA_DevAssets/SamplePackages~/`；末尾 `~` 让 Unity 忽略该目录，用工具中的“打开示例包目录”访问，不要在 Project 面板中寻找或去掉 `~`。

每个 `.unitypackage` 必须与同名 `.unitypackage.json` 放在一起。包依赖兼容的 GF 模板及 Unity `2022.3.62f3c1`，不适用于空白 Unity 工程，也不包含公共框架或第三方插件。

本次导出：

- `HexaAway-1.0.0-20260914-170724-723.unitypackage` + 同名 `.json`
- `RectMatch-1.0.0-20260914-170340-255.unitypackage` + 同名 `.json`

合集中保留这四个原始文件与说明文档，按 `Assets/AAA_DevAssets/` 目录结构存放；**它不是完整模板工程**。解压后通过工具导入，不要直接双击包跳过注册同步。包保留在磁盘不代表游戏已经安装，请查看工具内的安装状态。压缩完整工程时请通过文件系统包含这个被 Unity 忽略的目录。

导入不会自动切换主玩法；在 GameLauncher 的 `GameEntry/GameFramework/GameManager` 中显式设置。新产品应使用工具逐个移除两个示例，不要直接删文件夹。

完整操作与恢复说明：[Sample SubGames](../Docs/05_SAMPLE_SUBGAMES.md)。
