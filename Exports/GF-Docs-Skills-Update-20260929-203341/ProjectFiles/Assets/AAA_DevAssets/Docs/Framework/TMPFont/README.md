# TMPFont 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. 职责与配置关系

TMPFont 负责为语言选择字体、配置字形回退，并按样式预设创建/复用材质。它不翻译文本、不保存语言选择、不重载字典，也不决定页面排版和按钮业务。

```text
TMPLanguageFontConfig
  ├─ languageProfiles → LanguageEntry（每种语言）
  │                      ├─ fontAssetName → Fonts 下的 TMP_FontAsset
  │                      ├─ fontProfile → TMPFontProfile
  │                      └─ displayName / displayNameSprite → 语言选择列表
  └─ defaultProfile                         │
                                           └─ styleKey → FontStylePreset

TMPFontComponent 加载字体、选择 Profile、缓存材质
  → OnFontProfileChanged
  → TMPStyleApplier 更新 TMP_Text.font 与 fontSharedMaterial
```

| 对象 | 管什么 | 不应混淆的内容 |
| --- | --- | --- |
| 源 `.ttf` / `.otf` | 生成字形的源字体 | 不是运行时 TMP SDF 资产 |
| 字符集 `.txt` | 需要纳入字体的字符输入 | 修改它不会自动更新现有 SDF |
| TMP_FontAsset `.asset` | 字符/字形表、Atlas 和基础材质 | 不是语言启用名单，也不是文案字典 |
| LanguageEntry | 语言与字体资源名、Profile、选择项外观 | 不会自动加入启动支持名单 |
| TMPFontProfile | 大小写敏感的 styleKey 到 Preset 的映射 | 不持有具体语言字体，不定义字号/锚点/换行布局 |
| FontStylePreset | Face、描边、阴影参数 | 是 SO，不是可直接赋给 TMP 的 Material |
| TMPStyleApplier | 对单个 TMP_Text 应用当前字体与样式 | 不解析 Key、不更改文案，也不自动完成语言切换 |

## 2. 当前资产与加载方式

### 2.1 文件位置与创建入口

| 内容 | 位置 / 菜单 |
| --- | --- |
| 字体模块脚本 | [TMPFontSystem](../../../../GameMain/CustomComponents/TMPFontSystem) |
| 当前语言配置 | [TMPLanguageFontConfig.asset](../../../../GameMain/ScriptableObjects/TMPLanguageFontConfig.asset) |
| 各语言配置项 | [TMPLanguageEntries](../../../../GameMain/ScriptableObjects/TMPLanguageEntries) |
| 运行时 SDF | [Assets/GameMain/Fonts](../../../../GameMain/Fonts) |
| 当前 Profile | [TMPFontProfile.asset](../../../../GameMain/Fonts/TMPFontProfile.asset) |
| 样式 Preset SO | [Fonts/MaterialPreset](../../../../GameMain/Fonts/MaterialPreset) |
| 源字体与字符集 | [AAA_DevAssets/Fonts/Fonts](../../../Fonts/Fonts) / [Charset](../../../Fonts/Charset) |
| 创建配置资产 | Create → TMP → `Language Font Config` / `Language Entry` / `Font Profile` / `FontStylePreset` |
| 制作字体图集 | `Window/TextMeshPro/Font Asset Creator`（当前本地 TMP 3.0.9 的菜单） |
| 统一维护字符集与图集 | `Game Framework/字体与字符集/打开窗口` |
| 编辑器制作配置 | [FontCharsetConfig.asset](../../../Fonts/Editor/FontCharsetConfig.asset)；不会替代运行时 LanguageEntry |

源码中的部分注释仍写 TMPFontManager，实际入口是 `GameEntry.TMPFont` / TMPFontComponent，不要另建同名 Manager。Fonts 根目录的旧 `.mat` 与 MaterialPreset 中的 Preset SO 也不是同一类资产。

### 2.2 当前语言映射

| 语言 | fontAssetName |
| --- | --- |
| English、French、German、Italian、Portuguese、Russian、Spanish、Turkish、Vietnamese | MFont_BASE |
| ChineseSimplified | MFont_CNS |
| ChineseTraditional | MFont_CNT |
| Japanese | MFont_JP |
| Korean | MFont_KR |

本次已核对清单所引用的 13 个 LanguageEntry，其对应 XML 和字体文件均存在；这不代表每个字形与所有文案已检查。Arabic、Thai 等字体资产虽然存在，目前没有因此自动成为已启用语言。

运行时 TMP 字体资产统一命名为 `MFont_<缩写>.asset`，`fontAssetName` 填同名、不带扩展名的资源键。缩写固定为 `BASE`（共用基础字体）、`CNS`（简中）、`CNT`（繁中）、`JP`（日文）、`KR`（韩文）、`AR`（阿拉伯文）、`BN`（孟加拉文）、`DEV`（天城文）、`TH`（泰文）、`MM`（缅甸文）。`DEV` 表示书写系统，不限定单一语言；`BASE` 是否覆盖目标语言仍需检查字形。

主资产名称与文件名一致，内嵌 Atlas / Material 使用相同前缀，例如 `MFont_CNS Atlas`、`MFont_CNS Atlas Material`。源 `.ttf/.otf` 保留字体家族名称，字符集文件沿用现有名称；不以运行时资源名替代真实字体来源信息。

改名须通过 Unity AssetDatabase 保留 GUID，并同步 LanguageEntry、TMPFontComponent 默认值及场景序列化配置。ResourceCollection 按 GUID 收集，但构建后的资源路径发生变化，发布前需重新构建资源。历史示例包的公共依赖同时校验 GUID 与路径；引用旧字体路径的导出包需要在新模板下重新导出，不应只改旧包清单来绕过导入检查。

### 2.3 运行时链路与回退

`ProcedurePreload` 加载配置后调用 `SetLanguageConfig(config, onComplete)`，其内部按当前 Localization.Language 调用 SwitchLanguage。

1. 找目标 LanguageEntry，找不到条目时尝试 English；目标条目存在但 fontAssetName 为空时直接报错，不再替它找英文资源。
2. 选择条目的 fontProfile，为空时用 defaultProfile。
3. 先加载 `_DefaultFontName`（脚本默认 MFont_BASE），再加载目标字体。通过 `AssetUtility.GetTMPFontAsset(name, true)` 解析为 `Assets/GameMain/Fonts/{name}.asset`，不自动加 `_CN` / `_EN` 后缀。
4. 若目标字体与基础字体不同，将基础字体追加到目标字体 fallback 列表。这是“目标字体缺字时找基础字体”，不是把所有语言字库都互设为 fallback。
5. ApplyProfile 在 Profile 非空时设置 UGuiForm 主字体并发出 OnFontProfileChanged；TMPStyleApplier 接收后更新文本。

字体加载失败会记录日志，不会自动换另一套字体重新加载。缺语言条目的 English 回退、defaultProfile 样式回退、基础字体字形回退分别处理不同问题。

## 3. 从效果图或缺字问题确定方案

先判断本次属于文案新增、补字、换字体、新语言还是样式调整，再决定改哪些资产。

| 场景 | 先调查 | 默认修改边界 |
| --- | --- | --- |
| 新页面沿用既有视觉 | 现有 styleKey、Profile、Preset 是否满足 | 优先绑定已有样式，不复制一套字体和材质 |
| 图中新增标题/按钮样式 | 字体、粗细、Face、描边、阴影、字号、对齐分别分析 | 字号/布局留 TMP_Text；可复用材质参数用 Preset |
| 修复少量缺字 | 真实显示字符、对应 SDF、fallback 和源字体覆盖 | 只补受影响字符集并更新对应 SDF，避免重做所有语言 |
| 替换字体外观 | 源字体、受影响语言/页面、尺寸与换行变化 | 先提供复用/替换范围；不能把全局换字体当成本页调样式 |
| 新增语言 | 能否共享 MFont_BASE、是否需要专用 SDF、语言名如何显示 | 与本地化清单一起接入，不只创建一个 FontAsset |

共享 Preset 变化会影响引用它的多个 styleKey、Profile 和页面。仅某个页面需要不同外观时，先考虑独立的样式键/预设，避免修改共享资源造成全局变化。方案给出具体引用和对照效果，用户已经明确授权的范围不重复确认。

## 4. 接入语言字体与选择列表

1. 在 LanguageEntry 设置正确 `languageKey`，每个启用语言保持唯一条目；当前 GetEntry 按列表找到第一个匹配，不自动报告重复。
2. 填写不带扩展名的 fontAssetName，确认运行时路径可加载。优先复用已有字体；确需新 SDF 时先完成[字体制作流程](FontAuthoring.md)。
3. 选择 fontProfile，或确保 defaultProfile 可用。两者都空时不会完成正常的主字体设置和通知，即使字体资源已加载也不能判定接入成功。
4. 配置语言选择项。LanguageEntry.OnValidate 根据 languageKey 重写 displayName；任意手填名称可能被覆盖。displayNameSprite 有值时，LanguageItem 优先用图片显示语言名并隐藏文本，便于展示当前字体未覆盖的语言名称。
5. 把条目加入运行时配置的 languageProfiles，检查资源收集和预加载；GetAvailableLanguages 返回该列表的副本，不代表条目已通过有效性校验。
6. 交回 [本地化 §6](../Localization/README.md#6-新语言接入与完整切换要求) 补齐 XML、启动名单、语言图片及切换验收。

启动字体装配与运行中选择语言是不同路径。现有设置页不调用 SwitchLanguage，不能靠添加 LanguageEntry 就宣称动态换字体已接通。

## 5. 制作与使用字体样式

### 5.1 样式的接入步骤

1. 查当前 Profile 中已有样式键，并查看其 Preset；先判断能否复用。
2. 新样式通过 Create → TMP → FontStylePreset 创建 SO，设置 Face、Outline、Underlay。参数为当前 Shader 支持的属性，缺少的 Shader 属性不会因创建 Preset 自动出现。
3. 在目标 Profile 的 styles 中添加唯一 key 与 preset；跨语言共用或分别调样式按实际外观决定。
4. 在 TMP_Text 节点挂 TMPStyleApplier，填写完全一致的 styleKey。组件负责字体/材质，文本内容通过本地化设置，字号、对齐、换行和 RectTransform 由页面布局控制。
5. 保存 Prefab 后读回字段；Play 中等字体/配置就绪后检查，与编辑态直接指定材质的外观分别对照。

```csharp
// applier 是已绑定的 TMPStyleApplier；当前 Profile 有 Default 键。
applier.SetStyleKey("Default");
// 若暂不刷新，可用 SetStyleKey("Default", false)，随后由明确时机应用。
```

空 styleKey 或找不到对应 Preset 时使用当前字体的原始材质。空键不会自动选择名为 Default 的条目；要用 Default 预设必须填写该键。

### 5.2 当前样式与共享规则

当前 Profile 包含 `Default`、`Tilte`、`Outline_Black` 等键。`Tilte` 是已有实际拼写，不要未经引用检查就改为 Title；当前 `Outline_Shadow_Blue` 出现两次，GetPreset 使用第一条匹配。这是已观察到的配置问题，本次只记录，不变更其引用或行为。

材质按 `(TMP_FontAsset, FontStylePreset)` 缓存并共享，不能为一个文本直接修改返回材质的颜色等参数来实现局部差异。Preset 为 null 时直接返回字体原始材质；这也不是供页面随意改写的私有副本。

运行时修改 Preset 后，仅再次获取缓存不会重新套用全部参数。调用 `GameEntry.TMPFont.InvalidateCache(preset)` 可销毁相关缓存并通知 Applier 重新应用；当前没有通用 Inspector 自动调用此方法的接入。改 Preset 的制作验收应重新启动或明确触发缓存刷新。

## 6. 生命周期与 API 的实际保证

| API / 时机 | 当前行为与限制 |
| --- | --- |
| SetLanguageConfig | 保存配置并开始当前语言字体加载；用于预加载装配 |
| SwitchLanguage(language, onComplete) | 切字体/样式，不改字典或保存语言。失败路径也可能调用 onComplete，回调不是成功证明 |
| LoadFontAsset(name, success, failure) | 按资源名缓存字体，合并同名并发请求；只加载，不自动设置当前语言/当前字体 |
| ApplyProfile | Profile 非空才设置 UGuiForm 主字体并广播，不重新加载字形 |
| TMPStyleApplier.OnEnable | 订阅 Profile 变化，并安排下一帧应用当前 Profile |
| TMPStyleApplier.OnDisable | 取消 Profile 变化订阅；重新激活时需检查最新配置已应用 |
| InvalidateCache | 使某个 Preset 的材质缓存失效并触发重新应用 |
| TMPFontComponent.OnDestroy | 销毁其缓存生成的材质；当前没有显式逐项卸载字体资源的对称逻辑 |

内部 `_loadVersion` 阻止旧版本成功响应改变当前字体；被忽略的旧成功回调也可能不再触发原 onComplete，失败回调没有相同的版本判断。不要把它简单包装为“每个请求一定完成一次”的 await，也不要把它当作已经完善的语言切换取消协议。

UGuiForm.InitLocalization 会遍历子 TMP 文本设置主字体，并将 fontStyle 改为 Normal；TMPStyleApplier 另负责材质。手工设置粗体/专用字体的节点应检查真实刷新顺序和显示结果，当前基类没有通用“排除此文本”的配置。静态文本刷新、动态文案刷新和样式刷新应按同一次语言操作验证，完整要求见 [本地化 §6](../Localization/README.md#6-新语言接入与完整切换要求)。

## 7. 排错与验收

| 现象 | 优先检查 |
| --- | --- |
| `<NoKey>` | 先查本地化 Key/字典，不能靠换字体解决 |
| 字符正确但显示方框 | 当前实际 fontAsset、字符表、fallback、源字体及图集生成结果 |
| 文本切换了但字体未换 | 当前入口有没有调用 SwitchLanguage，字体加载是否成功 |
| 样式不生效 | Applier 的 key、大小写、Profile 是否为空、Preset 及重复键顺序 |
| 修改预设无变化 | 是否复用了旧材质缓存、是否明确 InvalidateCache/重新进入 |
| 多个页面一起变色 | 是否修改了共享 Preset、共享缓存材质或字体原始材质 |
| 语言列表显示不全或语言名缺字 | LanguageEntry 列表、displayNameSprite 和回退文本覆盖 |
| 描边/阴影裁切 | 图集留边、Shader 支持、文本 RectTransform/Mask 与效果参数 |

- [ ] 需求属于补字、换字体或样式修改已明确，共享影响已说明。
- [ ] LanguageEntry、字体名、Profile、styleKey 唯一且引用正确。
- [ ] 源字体、字符集、SDF、Atlas、材质及收集按实际改动同步。
- [ ] 新增字符和典型格式化输出都已检查，生成缺失结果已处理。
- [ ] 编辑态与运行时样式、切语言、关闭重开、不同屏幕比例及失败路径已验证。
- [ ] 字典成功、字体成功、视觉通过分别有证据；没有把文件存在或 onComplete 当作完整通过。

本次只核对配置/代码及文档，不代表上述运行验收已完成。

依据：[TMPFontComponent](../../../../GameMain/CustomComponents/TMPFontSystem/TMPFontComponent.cs)、[TMPLanguageFontConfig](../../../../GameMain/CustomComponents/TMPFontSystem/TMPLanguageFontConfig.cs)、[LanguageEntry](../../../../GameMain/CustomComponents/TMPFontSystem/LanguageEntry.cs)、[TMPFontProfile](../../../../GameMain/CustomComponents/TMPFontSystem/TMPFontProfile.cs)、[FontStylePreset](../../../../GameMain/CustomComponents/TMPFontSystem/FontStylePreset.cs)、[TMPStyleApplier](../../../../GameMain/CustomComponents/TMPFontSystem/TMPStyleApplier.cs)、[TMPFontProfileEditor](../../../../GameMain/Editor/TMPFontProfileEditor.cs)。

## 制作与更新入口

[字符集与图集统一更新](FontAuthoring.md#12-统一更新工具日常操作)。更新全部语言时明确选中全部已配置条目，不沿用上次的单语言选择。
