# Sound 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. SoundComponent

**功能**：按 Sound 表 ID 播放 Music、Sound、UISound 三类资源，并按组静音/调音量。

**新增步骤**：把 `.ogg` 放公共音频目录，在 `Sound.txt` 增加唯一 ID、资源名和播放参数；运行 DataTable 生成；核对对应 Sound Group 和资源收集。使用 `SoundId` 的调用方还要核对该常量对应的表 ID。

```csharp
GameEntry.Sound.PlayUISound(SoundId.UI_Click);
GameEntry.Sound.Mute("Music", true);
GameEntry.SaveData.IsMusic = false; // 用户设置需同步存档。
```

**使用与清理**：背景音乐走 `PlayMusic(id)` / `StopMusic()`，PlayMusic 会先停止上一次音乐；常规音效走 `PlaySound(id)`。这些 ID 扩展依赖 DRSound 已加载。停止某次音效使用其播放 SerialId，不用表 ID。

**验收**：正常播放、静音、音量、切场景、重启设置保持；没有声音时先查表行、组名、音频路径/后缀和静音状态。

依据：[SoundExtension](../../../../GameMain/Scripts/Sound/SoundExtension.cs)。
