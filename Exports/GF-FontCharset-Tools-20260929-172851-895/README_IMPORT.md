# 字体与字符集工具：导入与首次配置

用于同源 GF Unity 工程的编辑器工具更新。功能包括语言字符扫描、TXT 追加、Static SDF 图集同步、自动采样字号、图集尺寸下拉选择、字体预览、相关资产自动保存、备份及失败恢复。

## 包内内容

- FontCharsetConfig、FontCharsetCollector、FontCharsetService、FontCharsetWindow。
- Tests/FontCharsetValidation：通过菜单在临时副本上验证，不更新正式字体。
- 本说明。脚本及文件 GUID 随 Unity 原生包保留。

本包不包含运行时框架、字体源文件、TMP 图集、已有字符集 TXT、翻译 XML、制作配置 SO 或场景。它不会在导入时自动重建字体。

## 导入条件

来源工程使用 Unity 2022.3.62f3c1、TextMeshPro 3.0.9。其他版本需在目标项目验证 API 兼容性。脚本保持在 Editor 目录下。

目标项目必须已有：

| 依赖 | 所需接口 |
| --- | --- |
| TextMeshPro / TextCore | TMP_FontAsset、TMP_EditorUtility、FontEngine、GlyphRenderMode |
| TMPLanguageFontConfig | languageProfiles 列表 |
| LanguageEntry | languageKey、fontAssetName；languageKey 使用 GameFramework.Localization.Language |
| Lokas.AssetUtility | GetTMPFontAsset(string, bool)，解析目标项目字体实际路径 |

如缺少上述框架类型或字段，先对照目标版本补齐/适配依赖。不要把同名旧脚本与新脚本并存，也不要为了消除编译错误复制整个来源项目的字体配置。

## 导入步骤

1. 在目标工程中通过 Assets → Import Package → Custom Package 选择本包。
2. 同源项目会按 GUID 识别脚本更新；已有不同 GUID 的同名工具时先比较并合并，避免重复类型，保留目标项目既有配置对脚本的引用。
3. 导入后等待编译完成；确认 Console 无新增错误，再打开 **Game Framework → 字体与字符集 → 打开窗口**。

## 首次配置

已有配置时继续使用目标工程的配置，不导入来源工程的配置资产。

目标项目沿用以下约定时，可点击“创建四语言配置”或菜单“创建默认配置”：

- 字体：Assets/GameMain/Fonts/MFont_CNS.asset、MFont_CNT.asset、MFont_JP.asset、MFont_KR.asset。
- 字符集：Assets/AAA_DevAssets/Fonts/Charset/unity_sdf_charset_CNS.txt、CNT、JP、KR 对应文件。
- XML：Assets/GameMain/Localization/{语言Key}/{语言Key}.xml。
- 运行时映射：Assets/GameMain/ScriptableObjects/TMPLanguageFontConfig.asset。
- 制作配置：Assets/AAA_DevAssets/Fonts/Editor/FontCharsetConfig.asset；先确保 Assets/AAA_DevAssets/Fonts 目录存在。

如果目录、字体命名或语言集合不同，请由目标项目 Agent/开发者通过 Unity 创建 `Lokas.Editor.FontCharset.FontCharsetConfig` 资产并配置条目。默认加载路径仍为上述制作配置路径；Inspector 可编辑 languageConfig、entries、源字体、目标字体、字符集和 XML 引用。使用 UnitySkills 时可先查询 `scriptableobject_create` 的实际参数并创建该类型，再通过序列化属性设置；不手写 SO YAML，也不为适配工具擅自重命名目标字体。

每个条目的 language 必须与运行时 LanguageEntry 对应；fontAssetName 经目标项目 AssetUtility 解析后应与 targetFont 路径相同。配置必须引用真实 TTF/OTF、UTF-8 TXT 和已保存 XML。已有 TMP 字体需为单图集 Static，默认材质和 Atlas 为字体子资产；工具不创建全新 TMP 字体资产。

## 使用与验证

1. 保存本地化 XML，勾选实际需要的语言。统一更新需全选；上次只选一种语言不会自动变成全部。
2. 点击“检查选中语言”，确认源字体覆盖和语言映射；检查不写资产。
3. 点击“更新字符集与图集”。工具自动保存本批次相关字体、材质和配置，再生成临时图集；全部成功后备份并写入，保留 GUID 和原有字符。
4. 已设置的 Auto Sizing 可调整采样字号；图集尺寸不会自动扩大。源字体缺字和图集容量不足分别处理。
5. 查看结果和字体预览，再在实际页面检查清晰度、换行和切语言。发布前按项目流程更新资源构建。

备份在 Backups/FontCharset/；更新报告在 Library/FontCharset/last-report.json。成功调用菜单不等于生成成功，应检查 success、每个请求语言的 status 和 backupPath。有待恢复事务时先执行“恢复未完成更新”。

目标工程配置就绪后，可运行 **Game Framework → 字体与字符集 → 运行工具验证**，结果在 Library/FontCharset/validation.txt。验证使用配置中的字体副本，因此也需要有效的目标工程语言配置和字体资产；导入成功不代表已经在目标项目运行验证通过。

更完整的模块手册与 Agent Skill 由配套“GF 文档与 Skill 更新包”提供。
