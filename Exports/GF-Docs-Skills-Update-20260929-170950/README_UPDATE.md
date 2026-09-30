# GF 文档与 Skill 更新包

用于更新同源或相近的 GF Unity 项目。解压在目标工程 Assets 外，再把下方“给 AI 的更新指令”交给目标项目 Agent；推荐先由它对照现有文件合并。

## 包含什么

| 内容 | 包内位置 | 处理方式 |
| --- | --- | --- |
| 框架模块手册、UI 制作/复用/验收、字体制作 | ProjectFiles/Assets/AAA_DevAssets/Docs/Framework/ | 本次主要更新内容 |
| 文档导航与首次运行 | ProjectFiles/Assets/AAA_DevAssets/Docs/ | 按目标项目存在的模块调整导航 |
| 三个 Skill | ProjectFiles/.agents/skills/ | gf-workflow、ui-agent、localization-sdf-charset |
| 根目录规范、AGENTS、项目 README | ProjectFiles/ 根目录 | 对比合并，不直接覆盖目标项目已有约定 |
| 具体活动/玩法/组件的说明 | ProjectFiles/Assets/GameMain/ | 可选；只同步目标项目实际拥有的模块 |
| 示例、设计、历史记录 | Docs/Samples、Design、Archive | 可选；作为来源工程参考，不代表目标工程已实现或已验收 |
| 来源文档的 Unity GUID | MetaReference/ | 仅供识别来源，不覆盖目标项目的 .meta |

三个 Skill 都可安装在目标项目的 `.agents/skills/`。字体 Skill 原来是个人 Skill，本包已提供项目级副本，便于随项目分发；不要同时维护两份不同版本却不说明使用哪份。

## 推荐更新步骤

1. 打开 [AGENT_UPDATE_PROMPT.md](AGENT_UPDATE_PROMPT.md)，将内容交给目标项目 Agent，并告知本更新包的实际解压目录。
2. Agent 核对同名文件和目标项目差异，备份本次会修改的内容。若仍使用旧数字文档目录，按 [MigrationMap.json](MigrationMap.json) 迁移。
3. 更新框架说明和三个 Skill，保留目标项目的具体规则、目录、ID 和注册方式。没有安装的活动/玩法只保留参考说明或从导航移除，不创建空业务模块来凑齐目录。
4. 检查 Markdown/Skill 路径和章节链接、Unity 文档 GUID，并验证 Skill 结构。仅更新文档无需重建字体或运行玩法。

## Unity 文档身份

ProjectFiles 中刻意不放 .meta，避免覆盖目标工程已有 GUID。已有旧文档通过 Unity AssetDatabase 移到新位置，保留目标的 .meta；目标位置已存在时比较后合并内容。新文档交给 Unity 导入生成 .meta。MetaReference 只供比对，不直接拷入 Assets，不复制来源文件夹的 .meta。

## 能力与版本边界

本包是当前工作区的文档/Skill 快照，包含尚未提交的整理。文档中的 ID、类名、菜单、模板、注册方式和样式目录需要与目标项目核对。历史记录里的机器路径属于当时环境，不能用作目标项目的配置。

未包含 UnitySkills 插件、GF 运行时代码、FontCharset 编辑器工具、字体/图集、配置 SO、Prefab、场景、翻译 XML 或生成数据表。已有这些工具时可使用对应 Skill；缺少字体更新工具时，复制 Skill 不会获得自动图集功能，应报告缺失，不能自动改为只更新 TXT 后声称完成。必要工具代码迁移是另一项工作。

完整字体流程依赖目标项目的 FontCharset 工具和 TMP 相关类型。来源工程工具目录为 `Assets/AAA_DevAssets/Editor/FontCharset/`；原四语言配置的 GUID 和源字体设置不随文档复制。

## 验证文件

- [MANIFEST.json](MANIFEST.json)：包内文件、目标路径、类别及 SHA-256；不包含清单自身的哈希。
- [VALIDATION.json](VALIDATION.json)：包内链接、迁移映射和三个 Skill 的校验结果。
- [PROJECT_REFERENCES.json](PROJECT_REFERENCES.json)：手册引用、但包内未包含的目标工程文件/目录，供目标 Agent 核对；它是文档引用清单，不是完整代码依赖分析。

更新包不含一键覆盖脚本，解压本身不会修改目标工程。
