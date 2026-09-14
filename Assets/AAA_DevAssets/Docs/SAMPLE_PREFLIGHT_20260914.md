# 示例依赖预检快照（2026-09-14）

仅是只读候选扫描，不代表可安全删除；文本命中中也包含注释和文案。清单变化后请重新运行 Tools/Template/Sample SubGames。

## HexaAway

```text
示例包只读预检：HexaAway
资源目录：Assets/GameMain/SubGame/HexaAway
场景目录：Assets/GameMain/Scenes/SubGame/HexaAway
安装：True；受管路径数：214；扫描文件数：1778

外部引用候选（需人工区分共享依赖、注册与普通文本，非完整 C# 依赖分析）：
- Assets/GameMain/CustomComponents/Shop/Scripts/ShopComponent.cs:51 [名称/类型候选]
- Assets/GameMain/Scenes/GameLauncher.unity:1084 [名称/类型候选]
- Assets/GameMain/DataTables/UIForm.txt:26 [名称/类型候选]
- Assets/GameMain/Scripts/Component/Save/SaveDataStore.cs:62 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Panel/RankingUIPanel.cs:146 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Panel/ClaimRewardsUIPanel.cs:447 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Panel/MainUIPanel.cs:58 [名称/类型候选]
- Assets/GameMain/Scripts/Component/Ads/AdsManager.cs:92 [名称/类型候选]
- Assets/GameMain/Scripts/Procedure/ProcedureChangeScene.cs:206 [名称/类型候选]
- Assets/GameMain/SubGame/RectMatch/Scripts/TestMode/RectMatchTestModeModule.cs:3 [名称/类型候选]
- Assets/AAA_DevAssets/Editor/SampleSubGamesWindow.cs:27 [名称/类型候选]
- Assets/GameMain/Scripts/Common/GameEnums.cs:25 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Panel/HexaAwayUIPanel.cs:11 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Panel/GameUIPanel.cs:40 [名称/类型候选]
- Assets/AAA_DevAssets/Editor/Tests/SubGameServerConfigTests.cs:26 [名称/类型候选]
- Assets/GameMain/Scripts/Procedure/ProcedurePreload.cs:106 [名称/类型候选]
- Assets/GameMain/Scripts/Component/GameManagerComponent.cs:158 [名称/类型候选]
- Assets/GameMain/UI/UIPanel/MainUIPanel.prefab:2231 [名称/类型候选]
- Assets/GameMain/ScriptableObjects/SubGameAssetRegistryConfig.asset:16 [GUID 引用]
- Assets/GameMain/Configs/ResourceCollection.xml:8 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Panel/SettingUIPanel.cs:16 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Runtime/UIFormId.cs:97 [名称/类型候选]
- Assets/GameMain/Configs/DefaultConfig.txt:7 [名称/类型候选]
- Assets/GameMain/Scripts/Component/SaveDataComponent.cs:29 [名称/类型候选]
- Assets/GameMain/DataTables/Scene.txt:8 [名称/类型候选]
- Assets/GameMain/Scripts/Base/GameEntry.Custom.cs:21 [名称/类型候选]
- Assets/GameMain/Scripts/Procedure/ProcedureMenu.cs:87 [名称/类型候选]
- Assets/Plugins/GFTools/Editor/JsonEncryptTool.cs:49 [名称/类型候选]
- Assets/GameMain/Scenes/Test.unity:225 [GUID 引用]

未扫描文件：0

结论：当前不开放移除/导入；命中 0 项也不代表安全。
还必须核对 GameEntry/SaveData/Procedure、UIForm/Scene 生成文件、主玩法选择、全局清单引用、ResourceCollection 和测试代码。
```

## RectMatch

```text
示例包只读预检：RectMatch
资源目录：Assets/GameMain/SubGame/RectMatch
场景目录：Assets/GameMain/Scenes/SubGame/RectMatch
安装：True；受管路径数：117；扫描文件数：1842

外部引用候选（需人工区分共享依赖、注册与普通文本，非完整 C# 依赖分析）：
- Assets/GameMain/Localization/German/German.xml:64 [名称/类型候选]
- Assets/GameMain/Localization/Korean/Korean.xml:64 [名称/类型候选]
- Assets/GameMain/Scenes/GameLauncher.unity:1108 [名称/类型候选]
- Assets/GameMain/DataTables/UIForm.txt:13 [名称/类型候选]
- Assets/GameMain/Localization/Portuguese/Portuguese.xml:64 [名称/类型候选]
- Assets/GameMain/Localization/ChineseSimplified/ChineseSimplified.xml:64 [名称/类型候选]
- Assets/GameMain/Scripts/Component/Save/SaveDataStore.cs:63 [名称/类型候选]
- Assets/GameMain/CustomComponents/RedDot/RedDotConfigDatabase.asset:72 [名称/类型候选]
- Assets/GameMain/Localization/English/English.xml:64 [名称/类型候选]
- Assets/GameMain/Localization/Vietnamese/Vietnamese.xml:64 [名称/类型候选]
- Assets/GameMain/Localization/Japanese/Japanese.xml:64 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Panel/ClaimRewardsUIPanel.cs:450 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Panel/MainUIPanel.cs:37 [名称/类型候选]
- Assets/GameMain/Localization/French/French.xml:64 [名称/类型候选]
- Assets/GameMain/Localization/Spanish/Spanish.xml:64 [名称/类型候选]
- Assets/GameMain/Scripts/Procedure/ProcedureChangeScene.cs:211 [名称/类型候选]
- Assets/AAA_DevAssets/Editor/SampleSubGamesWindow.cs:28 [名称/类型候选]
- Assets/GameMain/Localization/Russian/Russian.xml:64 [名称/类型候选]
- Assets/GameMain/Scripts/Common/GameEnums.cs:26 [名称/类型候选]
- Assets/AAA_DevAssets/Editor/Tests/SubGameServerConfigTests.cs:75 [名称/类型候选]
- Assets/GameMain/Scripts/Component/GameManagerComponent.cs:160 [名称/类型候选]
- Assets/GameMain/UI/UIPanel/MainUIPanel.prefab:240 [名称/类型候选]
- Assets/GameMain/ScriptableObjects/SubGameAssetRegistryConfig.asset:17 [GUID 引用]
- Assets/GameMain/Configs/ResourceCollection.xml:10 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Panel/SettingUIPanel.cs:17 [名称/类型候选]
- Assets/GameMain/Scripts/UI/Runtime/UIFormId.cs:39 [名称/类型候选]
- Assets/GameMain/Localization/Turkish/Turkish.xml:64 [名称/类型候选]
- Assets/GameMain/Configs/DefaultConfig.txt:3 [名称/类型候选]
- Assets/GameMain/Scripts/Component/SaveDataComponent.cs:32 [名称/类型候选]
- Assets/GameMain/Localization/Italian/Italian.xml:64 [名称/类型候选]
- Assets/GameMain/DataTables/Scene.txt:7 [名称/类型候选]
- Assets/GameMain/Scripts/Base/GameEntry.Custom.cs:24 [名称/类型候选]
- Assets/GameMain/Scripts/Procedure/ProcedureMenu.cs:100 [名称/类型候选]
- Assets/GameMain/Localization/ChineseTraditional/ChineseTraditional.xml:64 [名称/类型候选]
- Assets/GameMain/SubGame/HexaAway/UI/HexaAwayUIPanel.prefab:1011 [GUID 引用]

未扫描文件：0

结论：当前不开放移除/导入；命中 0 项也不代表安全。
还必须核对 GameEntry/SaveData/Procedure、UIForm/Scene 生成文件、主玩法选择、全局清单引用、ResourceCollection 和测试代码。
```

