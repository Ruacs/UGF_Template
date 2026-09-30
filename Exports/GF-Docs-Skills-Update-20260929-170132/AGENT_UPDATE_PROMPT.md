请使用我提供的 GF 文档与 Skill 更新包，更新当前项目的开发文档和 AI 工作流。

先读取包内 README_UPDATE.md、MANIFEST.json 和 MigrationMap.json。ProjectFiles 的相对路径以当前工程根目录为目标位置。用户提供的更新范围授权文档和 Skill 的合并及对应文档迁移，不授权运行时代码、资源注册、字体图集或业务配置变更。

执行要求：

1. 检查当前项目与更新包的目录和实现差异；保留现有未提交修改、项目约定、ID、GUID、资源归属和模块配置。备份本次受影响文件，列出必要修改后执行，不重复询问是否开始普通文档更新。
2. 合并 Framework 模块手册、文档导航和 gf-workflow、ui-agent、localization-sdf-charset 三个 Skill。Skill 放在当前项目 .agents/skills 中。根目录 AGENTS.md、README.md、GF_FRAMEWORK_TEMPLATE_GUIDELINES.md 必须对照合并，不整份覆盖项目特有规则。打包的 gf-workflow 是待更新文件，不要求为了这次文档任务调用它。
3. 对照 MigrationMap.json 处理旧编号文档。章节迁移优先于整文件迁移，10/11 等聚合手册需要按专题拆分；旧目标缺失时跳过，不凭更新包创建假的原文件。使用 Unity AssetDatabase.MoveAsset 保留目标文档的 .meta 和 GUID；目标位置已存在时合并，不覆盖身份或强制删除。Unity 不可用时先整理合并内容和迁移清单，明确哪些资源移动未完成。
4. 不把 MetaReference 拷入 Assets。新增文档让 Unity 正常生成 .meta；对已有文档保留目标工程的 GUID。
5. 具体活动、玩法和组件文档只跟随实际存在的模块更新。未安装模块、示例和历史记录为可选参考；如果不引入，同步调整导航和案例链接，不能留下指向不存在文档的正式入口。不得为满足说明而新增业务代码或安装示例。
6. 核对 PROJECT_REFERENCES.json 中涉及的代码、组件、Prefab 和工具。UIAgent 共用目标工程的模块手册；复用目录必须列目标工程真实资产。字体 Skill 先确认 FontCharset 工具、菜单、配置、自动保存行为是否存在，不复制来源工程的字体 GUID 或四语言配置。需要改代码才能成立的差异单独报告，不自行扩大范围。
7. 同步 Markdown、Skill、项目 README、AGENTS 以及实际引用旧路径的工具中的必要文档链接。不要把历史“验证通过”改写成当前项目已经通过。保留现有 Skill 的有效策略和目标项目的特别约定。
8. 完成链接/章节、遗留旧路径、文档 GUID 和 Skill 结构检查。没有行为变更时不运行无关的 Unity 全量测试，不重建字体图集或资源包。交付时说明更新了什么、保留了哪些项目差异、缺少哪些可选功能和未完成事项。
