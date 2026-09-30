# 本地化模块：文本、语言接入与语言图片

[返回模块总览](../../README.md) · [TMPFont 手册](../TMPFont/README.md) · [UI 手册](../UI/README.md)

实现核对日期：2026-09-29。本文基于当前工作区代码与配置；操作验收清单不代表本次已经运行通过。字体与字符集由 TMPFont 手册详细说明，本页持有语言切换的跨模块流程。

## 1. 职责与协作边界

本地化负责“当前使用哪种语言，以及显示什么文本和图片”。字体模块负责对应字形与材质，页面负责把内容放进可用的布局中。

| 对象 | 职责 | 与其他模块的交接 |
| --- | --- | --- |
| LocalizationComponent | 保存当前语言、加载字典、按 Key 查文本、报告加载事件 | 不会仅因设置 Language 就自动加载新字典或字体 |
| XmlLocalizationHelper | 解析指定语言的 XML，添加 Key/Value | 语言名必须匹配；不翻译、不补缺失 Key |
| LocalizationExtension | 动态格式化文本、按路径发起字典读取 | LoadLanguage 返回不是加载完成；完成由 GF 事件报告 |
| SaveData.Language | 持久化选择，并设置 Localization.Language | 当前先保存选择，再由调用方发起加载；没有自动回滚 |
| UIStringKey / UGuiForm | 标记静态文本；页面初始化及显式刷新时填充文本 | UIStringKey 本身不订阅语言变化，动态内容由页面重新计算 |
| UILocalizedImageKey | 加载同一逻辑图片名在当前语言下的资源 | 图片通过 Resource 加载；失败只使用指定 FallbackSprite |
| LanguageUIPanel / LanguageItem | 展示可选语言、处理选择 | 列表来自 TMPFont 配置，语言选项不是自动扫描 XML 得到 |
| TMPFontComponent | 加载语言字体并通知样式组件 | 见 [TMPFont 手册](../TMPFont/README.md)，与字典加载是两条独立链路 |

建议页面只消费 Key、参数和图片标记；语言选择入口统一承担切换协调。当前协调代码位于设置页，尚无覆盖失败、并发及完整字体切换的统一切换 API；本次不新增 Manager 或改造运行时。

## 2. 当前配置与加载行为

### 2.1 文件与工具

| 内容 | 实际位置 / 入口 |
| --- | --- |
| 运行时字典 | `Assets/GameMain/Localization/{Language}/{Language}.xml` |
| 语言图片 | `Assets/GameMain/Localization/{Language}/Images/{ImageName}.png` |
| 生成的 Key 常量 | [LocalizationKeys.cs](../../../../GameMain/Scripts/Localization/LocalizationKeys.cs) |
| 动态拼接 Key 的手工常量 | [LocalizationKeys.Manual.cs](../../../../GameMain/Scripts/Localization/LocalizationKeys.Manual.cs) |
| 字典编辑工具 | `Game Framework/多语言编辑器`，源码为 [LocalizationEditor](../../../../Plugins/GFTools/Editor/LocalizationEditor/Scripts/LocalizationEditor.cs) |
| 语言选项及字体映射 | [TMPLanguageFontConfig.asset](../../../../GameMain/ScriptableObjects/TMPLanguageFontConfig.asset) |
| 启动支持名单 | [ProcedurePreload](../../../../GameMain/Scripts/Procedure/ProcedurePreload.cs) 的 `s_SupportedLanguages` |

当前 Launcher 的 DictionaryType 为 Xml，Helper 为 `Lokas.XmlLocalizationHelper`。已有 JSON 文件不表示运行时读取 JSON；本项目当前 XML 使用流程无需为每次文案变更生成语言 bytes。修改全局字典格式属于另一个任务。

启动白名单和字体配置当前覆盖 13 种语言：简体中文、繁体中文、英语、法语、西班牙语、葡萄牙语、意大利语、俄语、越南语、土耳其语、德语、日语、韩语。葡萄牙语巴西/葡萄牙枚举在启动时归并到 Portuguese；其他未支持语言回退 English。字体目录存在 Arabic 等资源，不代表对应语言已经完整接入。

### 2.2 启动和运行中切换

```text
启动：SaveData.Language
  → 未指定时取 EditorLanguage / SystemLanguage
  → ProcedurePreload 规范化支持语言并保存
  → ReadData 加载该语言 XML
  → 预加载语言字体配置，TMPFont.SetLanguageConfig → 加载字体/样式

当前设置页切换：LanguageItem
  → SaveData.Language = 目标语言（同时设置 Localization.Language）
  → 回调 SettingUIPanel.ReloadLanguage
  → 清除旧字典 → LoadLanguage(this)
  → LoadDictionarySuccessEventArgs
  → SettingUIPanel 调用 UI.UpdateLocalizationTexts()
```

当前切换链没有再次调用 TMPFont.SwitchLanguage；TMPFontComponent 也未订阅字典成功事件。选择一种语言后“文本变了、字体仍是原来的”是这两条链未衔接的具体风险。

SettingUIPanel 关闭时解绑字典成功事件；若请求随后完成，不能保证它仍负责刷新。LoadLanguage 虽声明为 async void，内部只是发起 ReadData，没有可等待的完成返回值。XmlLocalizationHelper 解析时读取的是全局当前语言，快速切换的旧请求也没有独立事务隔离。

## 3. 接到本地化任务时先分析什么

像 UI 制作一样，先基于已有信息提出具体方案；只有语言范围、业务含义或共享影响不明确时才询问，不让用户决定普通文件操作细节。

| 任务 | 先检查 | 必须明确的交付范围 |
| --- | --- | --- |
| 给新页面加文案 | 现有 Key 是否同义，静态/动态文本、占位符、图片文字 | 新增哪些 Key、复用哪些 Key、目标语言与页面刷新 |
| 修改现有翻译 | Key 的其他引用、是否只是翻译更正或业务含义变化 | 修改范围；不能为了一个页面改变共享 Key 在其他页面的含义 |
| 按效果图接入文字 | 图中文字、按钮空间、数字/富文本、标题是否图片 | 源文案和语境；不能从截图推断未展示状态的文案 |
| 接入新语言 | 字典、字体、启动名单、选择列表、语言图片、布局能力 | 语言名称、资源缺口、字体复用或新增、真实验证范围 |
| 处理缺字/样式 | 文本 Key 是否正确，再查字体和样式 | 是本地化内容问题还是转交 TMPFont 的字形/材质问题 |

从同义 Key 的实际使用场景判断复用，不能只因英文 Value 一样就共用。产品术语、按钮动作、奖励单位和格式参数先对齐；已有源语言与术语约定优先，新任务缺少约定时列出拟用源文本供确认。

## 4. 新增或修改文本

1. **查现有 Key 与引用**：确认是否直接复用；需要新增时按页面/业务语义命名，例如 `Settlement.RewardTitle`（示例，不是已存在 Key）。已有 Key 改名要同步代码、Prefab 和各语言，不随意重命名。
2. **确定源文案与参数**：记录语境、复数/数量表达、换行、`{0}` 等参数及其含义。所有翻译保持占位符索引和格式语义，允许按语序调整位置；不要靠逐段拼接句子实现不同语言语序。
3. **同步目标语言**：维护同一 Key 集合。未翻译文本和临时占位要显式标记，不能通过复制英文就宣称翻译完成。保留大小写、富文本标签、转义与已有条目顺序。
4. **保存并更新常量**：用编辑器保存当前/全部语言，在完整 Key 列表的当前语言上执行“导出Key常量”。检查生成差异；只修改 Value 时通常不需要改变 Key 常量。
5. **绑定显示**：静态文本用 UIStringKey；动态内容在页面刷新函数中按业务参数取值，不让静态刷新覆盖已格式化内容。
6. **交接字形与图片**：保存 XML 后，通过 `Game Framework/字体与字符集` 扫描并更新对应语言，按 [TMPFont §1.2](../TMPFont/FontAuthoring.md#12-统一更新工具日常操作) 同步 TXT 与字体图集；图片文字走 §7。XML 保存本身不会自动重建图集。
7. **验证**：XML 可解析、Key 唯一、语言集合和参数一致；需要生成代码时检查编译；真实打开页面、切换受影响语言并检查布局。

XML 示例格式可参考 [English.xml](../../../../GameMain/Localization/English/English.xml)：

```xml
<Dictionaries>
  <Dictionary Language="English">
    <String Key="LEVEL" Value="Level {0}" />
  </Dictionary>
</Dictionaries>
```

XML 属性中的 `&`、`<`、引号等要正确转义，使用 XML 写入方式保留 Unicode；对字符集检查应读取解析后的显示内容，而非把 `&amp;` 当作界面上的六个字符。

### 编辑器当前限制

- “同步所有关键词”按 **行索引** 将当前列表的 Key 覆盖到其他语言，不按 Key 合并，也不翻译 Value。顺序不同会错配文本，长度不同可能遗漏或越界；没有核实逐行对应时不要使用该操作。
- “导出Key常量”只读 **当前语言列表**，不是全语言并集。它只把点、短横线和空格替换为下划线；例如 `A.B` 和 `A-B` 会生成同名字段，其他非法标识符也不会自动修正。
- 保存当前的代码虽然构造了去重字典，实际写出的仍是原列表；不能依赖保存动作自动去重。保存全部/关闭时保存可能写入多种语言，编辑前核对这些选项。
- 工具内编辑状态与外部修改的 XML 要先同步，避免随后保存旧缓存覆盖新文本。
- 手工 partial 文件保留 `GameOver.Title{0}` 等运行时拼接模式；它不能代替字典中真实的 `GameOver.Title1` 等键。

这些限制应通过操作前检查和差异审查规避；改进工具本身另行处理，不在整理手册时修改。

## 5. 页面中的使用方式

静态文案：在同一 TextMeshProUGUI 或 Text 对象上挂 UIStringKey，Key 使用实际字典键。UGuiForm.InitLocalization 会遍历包含未激活子节点的静态标记；标记组件自身没有自动刷新行为。

动态文案：例如显示带关卡号的 LEVEL，调用实际存在的 API：

```csharp
// level 为本次页面数据；在页面刷新函数中执行。
string label = GameEntry.Localization.Get(LocalizationKeys.LEVEL, level);
// 将 label 赋给相应 TMP_Text。
```

切换完成后，页面必须重新计算动态文案；不能缓存旧语言字符串长期复用。动态拼接键先生成真实 Key，再查询其 Value，区分“Key 中的占位符”和“文案中的占位符”。

普通场景文本或独立 Widget 不一定被 UGuiForm 遍历：由拥有者在数据/语言就绪时刷新，并对称解绑通知。语言刷新不得顺便重复发奖、重新发起购买或推进任务。

缺 Key 时当前 GF 返回 `<NoKey>键名`，没有逐条英文回退。XmlLocalizationHelper 即使没有找到匹配 Language 节点也可能返回解析成功，因此还应检查期望 Key/词条，不能只看成功事件。

## 6. 新语言接入与完整切换要求

新增一种语言时按以下清单贯通，不只复制 XML：

1. 明确目标语言枚举及地区变体归并方式，核对当前已有枚举。
2. 创建匹配目录/文件名和 `Dictionary Language` 的 XML，完成 Key、翻译和参数检查。
3. 在 TMPLanguageFontConfig 增加唯一 LanguageEntry，填写显示名/显示名图片、字体资源名和 Profile，见 [TMPFont §4](../TMPFont/README.md#4-接入语言字体与选择列表)。
4. 同步启动支持名单/规范化逻辑；只加入列表而未加入启动支持名单，重启可能回退 English。
5. 更新字符集与 SDF，准备语言图片；核对 Resource 的收集。复杂书写语言还需单独验证排版方向、字形连接和混排，不能以“字体有字”作为完整支持证据。
6. 从真实入口切换、打开/重开页面，再重启检查保存选择。当前切换缺口见 §2.2，未处理和未验证部分要明确报告。

**下面是完整切换应达到的行为要求，当前代码尚未全部实现，也没有名为 ChangeLanguageAsync 的现成入口：**

- 同一次切换关联目标语言与请求代次，避免旧响应覆盖新选择。
- 字典与字体都就绪后刷新静态/动态文本、图片、样式和布局；图片加载结果另行检查。
- 明确失败时保留旧语言或回退语言的策略，使已保存语言与实际显示状态一致。
- 快速连选、设置页提前关闭、加载失败时仍由明确的拥有者完成或取消切换。

未来实现这条流程时，先确定协调入口和兼容范围，不让每个页面各自维护一套切换逻辑。

## 7. 多语言图片

1. 为同一含文字图片确定稳定逻辑名，各目标语言放同名 `.png`，例如已有 `ClaimRewards_title`。
2. Image 节点挂 UILocalizedImageKey，Key 填不带目录/扩展名的图片名，绑定必要 FallbackSprite。
3. 默认按 UI 布局控制尺寸；启用 SetNativeSize 时验证各语言图片尺寸变化不会挤坏页面。
4. UGuiForm.InitLocalization 会发起加载；独立图片拥有者需显式调用 `ApplyLocalization()`。仅改 Key 属性不会自动加载。
5. 核对 Sprite 导入和资源收集，测试加载失败及快速切换。

当前实现只回退到组件指定的 FallbackSprite，不会自动请求英文图片；无 fallback 时失败可能保留旧图。版本号可丢弃后发请求之前的旧响应，OnDestroy 会使请求失效，但 OnDisable 没有同样处理，也没有显式资源卸载流程；不能将它描述为“页面关闭就取消并释放全部图片”。

## 8. 排错与验收

| 现象 | 优先检查 |
| --- | --- |
| `<NoKey>` | 当前字典、真实 Key、Language 属性、是否仍在加载或加载失败 |
| 文本存在但内容错位 | 是否使用了按行覆盖 Key 的同步操作、翻译与参数是否对应 |
| 新语言不出现在列表 | LanguageEntry 是否加入实际加载的 TMPLanguageFontConfig |
| 切换后重启恢复英语 | 启动白名单与 NormalizeSupportedLanguage |
| 字体没变或缺字 | 是否发起字体切换、字体资源映射和 SDF 覆盖，转 [TMPFont](../TMPFont/README.md) |
| 动态数字说明没换语言 | 页面是否重新格式化，而非只刷新 UIStringKey |
| 图片仍是上一种语言 | 图片路径/收集、失败日志和 fallback；不要先判断为文本问题 |
| 关闭设置页后显示没刷新 | 语言请求完成时刷新订阅者是否仍存在 |

- [ ] 交付范围、源语义、目标语言、共享 Key 影响已确定。
- [ ] 受影响 XML、Key 集合、重复项、参数/标签及生成常量已检查。
- [ ] 字符集、SDF、图片、语言配置与资源收集按实际改动同步。
- [ ] 静态/动态文本、长文案、图片和字体在真实页面正确显示。
- [ ] 切换、快速连选、失败、关闭页面及重启行为有验证结果。

本次仅文档整理及静态核对，未执行上述运行验收。

依据：[AssetUtility](../../../../GameMain/Scripts/Utility/AssetUtility.cs)、[XmlLocalizationHelper](../../../../GameMain/Scripts/Localization/XmlLocalizationHelper.cs)、[LocalizationExtension](../../../../GameMain/Scripts/Localization/LocalizationExtension.cs)、[SaveDataStore](../../../../GameMain/Scripts/Component/Save/SaveDataStore.cs)、[SettingUIPanel](../../../../GameMain/Scripts/UI/Panel/SettingUIPanel.cs)、[LanguageItem](../../../../GameMain/Scripts/UI/Widget/LanguageItem.cs)、[UGuiForm](../../../../GameMain/Scripts/UI/Runtime/UGuiForm.cs)、[UILocalizedImageKey](../../../../GameMain/Scripts/UI/Runtime/UILocalizedImageKey.cs)、[GF LocalizationManager](../../../../Plugins/UnityGameFramework/GameFramework/Localization/LocalizationManager.cs)。
