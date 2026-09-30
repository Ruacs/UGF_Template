---
name: localization-sdf-charset
description: Maintain Unity TextMeshPro character sets and SDF font atlases from confirmed localization text. Use for missing-glyph scans, appending characters, configuring source fonts, or synchronizing charset TXT and font atlases through project editor tools. Translation is a separate task, not an implicit part of font maintenance.
---

# Localization SDF Charset

Use the project's shared editor tool for both human and AI workflows. Treat charset coverage, generated atlas coverage, and runtime loading as separate checks.

## Installation and project compatibility

This skill can live in a user's skill directory or in the target project's `.agents/skills/localization-sdf-charset/`. Resolve project-relative paths against the active Unity project, not against a previous project's location. The bundled Python helper remains relative to this skill.

Installing this skill or its manuals does not install the FontCharset editor tool, source fonts, TMP assets, XML data or Unity integration. Check the target implementation and supported menus before using the GF sequence below, including whether scoped auto-save is implemented. Preserve the target project's language mappings and generation settings; do not copy source-project GUIDs/configuration merely to match the documentation.

## Choose the task and preserve authorization

- **Design / scan:** read configuration and show differences; do not write charset or font assets.
- **Append / synchronize:** execute only the requested languages through the project tool. Existing explicit authorization is sufficient; do not repeatedly ask to begin.
- **Change source font:** show the affected language/font mapping and preserve existing characters. A changed typeface rebuilds glyphs and may change wrapping/layout.
- **Translate:** only when requested. Follow the project's localization process and preserve placeholders, markup, XML structure and keys. Consume saved, confirmed values for font generation; do not translate every language merely because a font is missing a character.

## Resolve language scope before running

The editor's saved checkboxes are UI state, not the user's requested scope. Never silently reduce an all-language task to the one language left selected in a previous session.

- A named language or font limits the update to that mapping. A request to fix the tool itself does not authorize rebuilding every font.
- “统一更新”, “全部更新”, or “把其他语言也补上” covers all entries in the current authoring configuration. In this GF project that normally means CNS / CNT / JP / KR; discover the actual entries instead of adding MFont_BASE or other runtime fonts automatically.
- With no explicit language in a general synchronization request, use the established conversation scope. If none exists, scan all configured entries, briefly state that scope and update those entries when synchronization is authorized. A request only to inspect remains read-only.
- Resolve the intended set by language key and target font, then set every entry's `selected` flag to match that set through the Editor and read it back before invoking the selected-language menu. Preserve source fonts, text sources and atlas settings. If the user explicitly asks to use their current selection, read and honor it instead.

## Discover the project capability

1. Locate the project's TMPFont/font-authoring manual and editor tool, then inspect its current configuration. Derive source fonts, XML inputs, charset TXT, target TMP assets and generation parameters from that configuration; do not guess from filenames or reuse paths from another project.
2. Keep the authoring mapping consistent with the runtime language mapping. Report mismatches rather than quietly redirecting an update.
3. For a GF Template project containing `Assets/AAA_DevAssets/Docs/Framework/TMPFont/README.md`, read `Assets/AAA_DevAssets/Docs/Framework/TMPFont/FontAuthoring.md` **1.2** first. The shared implementation is under `Assets/AAA_DevAssets/Editor/FontCharset/`, with default configuration `Assets/AAA_DevAssets/Fonts/Editor/FontCharsetConfig.asset`.
   - `Game Framework/字体与字符集/扫描全部` is read-only for project assets.
   - `Game Framework/字体与字符集/更新选中语言` uses the currently loaded configuration's selected rows. It automatically saves current edits to affected fonts, materials and authoring configuration before generation; these saved edits become the rollback baseline and survive cancellation. Unrelated assets are not saved. The report's `savedAssetPaths` lists those saved assets.
   - Read `Library/FontCharset/last-report.json` after execution: successful menu invocation is not successful font generation. `success`, per-language `status`, `message` and `backupPath` carry the result.
   - `创建默认配置` is an explicit first-time setup action, not part of a read-only scan. `打开窗口` separates daily checks/updates, per-language font settings and preview into tabs; select all languages for a unified update. `恢复未完成更新` restores an interrupted write before another update.
4. Use the available Unity Editor integration according to its connection and operation rules. If Unity or the project tool is unavailable, report the missing capability. Do not silently replace complete atlas synchronization with a TXT-only edit.

## GF automation sequence

1. Read `FontCharsetConfig` entries and resolve the requested language set above. With UnitySkills, use `scriptableobject_get_serialized_properties` to obtain the actual `entries.Array.data[i].language`, `.targetFont` and `.selected` paths; indices are not stable language identifiers.
2. For an authorized update, use `scriptableobject_set_serialized_property_batch` on that configuration to set the flags. Its `items` parameter is a JSON **string** containing `{ "propertyPath": "entries.Array.data[i].selected", "value": "true" }` objects (use `"false"` for excluded entries). Fetch the tool schema/dry-run through the installed integration and read back all flags. Do not hand-edit asset YAML or create a new Editor script for selection.
3. Invoke `扫描全部`, inspect the requested rows in the fresh report, then invoke `更新选中语言`. A source-font error in an excluded language does not automatically block a valid requested subset; the update service rechecks that subset. Missing or mismatched requested mappings must be resolved before writing.
4. Normal unsaved-font warnings require no separate manual-save instruction: the current update service saves affected fonts/materials/configuration automatically. Preserve that scoped save behavior; do not substitute global `AssetDatabase.SaveAssets`, reimport to discard edits, or enlarge atlases to bypass errors. A save failure or pending recovery must be resolved before retrying.
5. Read the **new update report** immediately. Require `success == true` and exact coverage of the requested language/font mappings in `languages`; each must be `已更新` or `无需更新`. A successful menu response or one successful language cannot establish completion of the full request. Preserve the update summary and `backupPath` before a later scan overwrites the report.
6. Scan again and confirm the requested mappings have no TXT additions, atlas gaps, source gaps or pending rebuild. Report each requested language as updated / already current / failed / not run, rather than treating omitted languages as complete. Stop on unchanged failure; diagnose it before retrying.

## Collect and preview

- Preserve the union of existing charset content, existing font characters, confirmed per-language XML display text and explicit supplemental text. Do not prune or reorder historical TXT content as part of a normal update.
- Decode XML values before processing TMP markup and composite format placeholders. Handle escaped braces, `noparse`, case-transform tags and Unicode scalar values. Keep meaningful spaces and visible punctuation. Ambiguous markup or style expansions must be reported rather than discarded.
- Dynamic values cannot be inferred from `{0}`. Use explicit supplements for digits, time/currency formatting, dynamic names and other finite outputs. Arbitrary player input needs a separate coverage strategy.
- Show characters to append, atlas gaps, source-font gaps and origins. Source-font coverage does not guarantee atlas capacity; packing failure must be reported separately.
- The project's tool owns parsing/generation. Do not implement a second parser in an ad-hoc script to drive the actual update.

## Generate, commit and verify

- Generate in temporary objects before touching the target assets. Preserve GUIDs, existing atlas/material subasset identities, style settings and fallback references. Keep the configured Static/Dynamic policy.
- Honor the configured sampling policy: Auto Sizing may choose a smaller sampling size within its documented bounds; Custom Size stays fixed. Do not silently switch that policy, replace source fonts or enlarge atlases to make a failed batch pass. Give the concrete diagnostic and affected parameters; apply changes within existing authorization or obtain the missing choice.
- Commit TXT and font results together through the project's backup/recovery mechanism. A failed or canceled batch must not be reported as successful. If recovery fails, retain backups and report the exact recovery path; do not retry indefinitely.
- Verify persisted character coverage and references, then the real loading path when runtime resources changed. A font preview does not validate page layout; a successful load does not validate every glyph's appearance.
- Report which languages changed, added characters, generation/coverage outcome, backup location and any unperformed visual/build validation. If only TXT was authorized and changed, explicitly say **atlas not synchronized**.

## TXT-only fallback for projects without a unified tool

The bundled [scripts/update_charset.py](scripts/update_charset.py) remains a small append-only helper. It neither interprets XML/TMP markup nor generates atlases; pass already-resolved literal visible strings. It omits whitespace, so it cannot establish complete typography coverage on its own.

Run it without `--apply` first. Resolve the project's actual auxiliary folder and mapping; use `--charset-dir` or explicit file paths instead of assuming this project's layout. A temporary `phrases.json` maps charset names or file paths to arrays of literal strings:

```json
{"CNS": ["支付成功"], "CNT": ["支付成功"], "JP": ["支払い成功"], "KR": ["결제 성공"]}
```

```text
python <skill-dir>/scripts/update_charset.py --project <project> --charset-dir <charset-dir> --phrases <phrases.json>
python <skill-dir>/scripts/update_charset.py --project <project> --charset-dir <charset-dir> --phrases <phrases.json> --apply
```

Use `--apply` only for an authorized TXT update. Re-run dry-run to check `remaining_missing_count`, inspect UTF-8 output and touched-file diffs, and report the outstanding atlas step. Do not create fake success from a zero TXT missing count.
