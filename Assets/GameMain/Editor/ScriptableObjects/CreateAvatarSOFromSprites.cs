using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Lokas;

public class CreateAvatarSOFromSprites : EditorWindow
{
    private const string AvatarOutputFolder = "Assets/GameMain/ScriptableObjects/Avatar/AvatarEntrys";
    private const string AvatarFrameOutputFolder = "Assets/GameMain/ScriptableObjects/Avatar/AvatarFrameEntry";

    private Type[] displayConfigTypes = Array.Empty<Type>();
    private string[] displayConfigTypeNames = Array.Empty<string>();
    private int selectedTypeIndex;
    private DefaultAsset outputFolderAsset;
    private string outputFolder = AvatarOutputFolder;
    private string assetNamePrefix = "AvatarEntry";
    private bool useSpriteNameAsDisplayName = true;
    private bool pingCreatedAssets = true;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Config/Display SO Batch Creator")]
    public static void Open()
    {
        GetWindow<CreateAvatarSOFromSprites>("Display SO Creator");
    }

    [MenuItem("Assets/Create Display SO/Batch Creator", false, 100)]
    public static void OpenFromAssets()
    {
        Open();
    }

    [MenuItem("Assets/Create Avatar SO/AvatarEntrySO from Selected Sprites", false, 100)]
    private static void CreateAvatarEntry() => CreateSOs(typeof(AvatarEntrySO), "AvatarEntry", AvatarOutputFolder, true);

    [MenuItem("Assets/Create Avatar SO/AvatarFrameEntrySO from Selected Sprites", false, 101)]
    private static void CreateAvatarFrameEntry() => CreateSOs(typeof(AvatarFrameEntrySO), "AvatarFrameEntry", AvatarFrameOutputFolder, true);

    [MenuItem("Assets/Create Display SO/Batch Creator", true)]
    [MenuItem("Assets/Create Avatar SO/AvatarEntrySO from Selected Sprites", true)]
    [MenuItem("Assets/Create Avatar SO/AvatarFrameEntrySO from Selected Sprites", true)]
    private static bool ValidateSelection() => GetSelectedSprites().Length > 0;

    private void OnEnable()
    {
        RefreshDisplayConfigTypes();
        ApplyTypePreset();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Display SO Batch Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("选择 Project 里的 Sprite 或 Sprite Mode 为 Multiple 的 Texture2D 图集，然后批量创建继承 DisplayConfigSO 的配置资产。", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        selectedTypeIndex = EditorGUILayout.Popup("SO Type", selectedTypeIndex, displayConfigTypeNames);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyTypePreset();
        }

        outputFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolderAsset, typeof(DefaultAsset), false);
        if (outputFolderAsset != null)
        {
            string selectedFolder = AssetDatabase.GetAssetPath(outputFolderAsset);
            if (AssetDatabase.IsValidFolder(selectedFolder))
            {
                outputFolder = selectedFolder;
            }
        }

        outputFolder = EditorGUILayout.TextField("Output Path", outputFolder);
        assetNamePrefix = EditorGUILayout.TextField("Asset Prefix", assetNamePrefix);
        useSpriteNameAsDisplayName = EditorGUILayout.Toggle("Use Sprite Name", useSpriteNameAsDisplayName);
        pingCreatedAssets = EditorGUILayout.Toggle("Select Created Assets", pingCreatedAssets);

        GUILayout.Space(8);
        DrawSelectedSpritesPreview();

        GUILayout.Space(8);
        GUI.enabled = CanCreate();
        if (GUILayout.Button("Create Display SO Assets", GUILayout.Height(32)))
        {
            CreateSOs(displayConfigTypes[selectedTypeIndex], assetNamePrefix, outputFolder, pingCreatedAssets, useSpriteNameAsDisplayName);
        }
        GUI.enabled = true;
    }

    private void DrawSelectedSpritesPreview()
    {
        Sprite[] sprites = GetSelectedSprites();
        EditorGUILayout.LabelField($"Selected Sprites: {sprites.Length}", EditorStyles.boldLabel);

        if (sprites.Length == 0)
        {
            EditorGUILayout.HelpBox("请先在 Project 里选择 1 张或多张 Sprite/Texture2D。图集会自动读取所有子 Sprite。", MessageType.Warning);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(160));
        for (int i = 0; i < sprites.Length; i++)
        {
            EditorGUILayout.ObjectField($"{i + 1}. {sprites[i].name}", sprites[i], typeof(Sprite), false);
        }
        EditorGUILayout.EndScrollView();
    }

    private bool CanCreate()
    {
        return displayConfigTypes.Length > 0
            && selectedTypeIndex >= 0
            && selectedTypeIndex < displayConfigTypes.Length
            && !string.IsNullOrWhiteSpace(outputFolder)
            && !string.IsNullOrWhiteSpace(assetNamePrefix)
            && GetSelectedSprites().Length > 0;
    }

    private void RefreshDisplayConfigTypes()
    {
        displayConfigTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetAssemblyTypes)
            .Where(type => type.IsClass && !type.IsAbstract && typeof(DisplayConfigSO).IsAssignableFrom(type))
            .OrderBy(type => type.Name)
            .ToArray();

        displayConfigTypeNames = displayConfigTypes.Select(type => type.Name).ToArray();
        selectedTypeIndex = Mathf.Clamp(selectedTypeIndex, 0, Mathf.Max(0, displayConfigTypes.Length - 1));
    }

    private static IEnumerable<Type> GetAssemblyTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null);
        }
    }

    private void ApplyTypePreset()
    {
        if (displayConfigTypes.Length == 0 || selectedTypeIndex < 0 || selectedTypeIndex >= displayConfigTypes.Length)
        {
            return;
        }

        Type selectedType = displayConfigTypes[selectedTypeIndex];
        assetNamePrefix = selectedType.Name.EndsWith("SO", StringComparison.Ordinal)
            ? selectedType.Name.Substring(0, selectedType.Name.Length - 2)
            : selectedType.Name;

        if (selectedType == typeof(AvatarEntrySO))
        {
            outputFolder = AvatarOutputFolder;
        }
        else if (selectedType == typeof(AvatarFrameEntrySO))
        {
            outputFolder = AvatarFrameOutputFolder;
        }

        outputFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(outputFolder);
    }

    private static void CreateSOs(Type configType, string prefix, string folder, bool selectCreatedAssets, bool useSpriteNameAsDisplayName = true)
    {
        Sprite[] sprites = GetSelectedSprites();
        if (sprites.Length == 0)
        {
            EditorUtility.DisplayDialog("No Sprites Selected", "Please select one or more Sprite/Texture2D assets.", "OK");
            return;
        }

        EnsureFolder(folder);
        int nextId = GetNextId(configType, folder);
        var createdAssets = new List<UnityEngine.Object>();

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                var so = (DisplayConfigSO)ScriptableObject.CreateInstance(configType);
                so.id = nextId++;
                so.sprite = sprites[i];
                so.displayName = useSpriteNameAsDisplayName ? sprites[i].name : string.Empty;
                so.name = $"{prefix}_{so.id}";

                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{so.name}.asset");
                AssetDatabase.CreateAsset(so, assetPath);
                createdAssets.Add(so);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (selectCreatedAssets)
        {
            Selection.objects = createdAssets.ToArray();
        }

        Debug.Log($"[DisplaySOCreator] Created {createdAssets.Count} {configType.Name} assets in {folder}");
    }

    private static Sprite[] GetSelectedSprites()
    {
        var sprites = new List<Sprite>();

        foreach (UnityEngine.Object selectedObject in Selection.objects)
        {
            if (selectedObject is Sprite selectedSprite)
            {
                sprites.Add(selectedSprite);
                continue;
            }

            if (selectedObject is Texture2D texture)
            {
                string path = AssetDatabase.GetAssetPath(texture);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                {
                    continue;
                }

                if (importer.spriteImportMode == SpriteImportMode.Single)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        sprites.Add(sprite);
                    }

                    continue;
                }

                sprites.AddRange(
                    AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                        .OfType<Sprite>()
                );
            }
        }

        return sprites
            .Where(sprite => sprite != null)
            .Distinct()
            .OrderBy(sprite => AssetDatabase.GetAssetPath(sprite))
            .ThenBy(sprite => sprite.name)
            .ToArray();
    }

    private static int GetNextId(Type configType, string folder)
    {
        return AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<DisplayConfigSO>(path))
            .Where(asset => asset != null && configType.IsAssignableFrom(asset.GetType()))
            .Select(asset => asset.id)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parentFolder = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parentFolder))
        {
            EnsureFolder(parentFolder);
        }

        string folderName = Path.GetFileName(folder);
        AssetDatabase.CreateFolder(parentFolder, folderName);
    }
}
