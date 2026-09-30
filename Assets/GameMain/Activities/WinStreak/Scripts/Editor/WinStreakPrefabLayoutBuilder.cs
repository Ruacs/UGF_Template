using System;
using Lokas;
using Lokas.Activities.WinStreak.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.WinStreak.Editor
{
    /// <summary>根据参考图和已提供素材生成 Win Streak 四个页面的可编辑 UGUI 布局。</summary>
    internal static class WinStreakPrefabLayoutBuilder
    {
        private const string UiPath = "Assets/GameMain/Activities/WinStreak/UI/";
        private const string TexturePath = "Assets/GameMain/Activities/WinStreak/Textures/";
        private const int UiLayer = 5;

        public static void EnsureAllLayouts()
        {
            EnsureLayout(UiPath + "WinStreakStartUIPanel.prefab", "WinStreakStartCard");
            EnsureLayout(UiPath + "WinStreakMainUIPanel.prefab", "WinStreakSafeArea");
            EnsureLayout(UiPath + "WinStreakDetailUIPanel.prefab", "WinStreakDetailCard");
            EnsureLayout(UiPath + "WinStreakEndUIPanel.prefab", "WinStreakEndCard");
        }

        public static void RebuildAll()
        {
            Rebuild(UiPath + "WinStreakStartUIPanel.prefab");
            Rebuild(UiPath + "WinStreakMainUIPanel.prefab");
            Rebuild(UiPath + "WinStreakDetailUIPanel.prefab");
            Rebuild(UiPath + "WinStreakEndUIPanel.prefab");
        }

        private static void EnsureLayout(string prefabPath, string marker)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new InvalidOperationException("Win Streak Prefab is missing: " + prefabPath);
            if (prefab.transform.Find(marker) == null) Rebuild(prefabPath);
        }

        private static void Rebuild(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (prefabPath.EndsWith("WinStreakStartUIPanel.prefab", StringComparison.Ordinal)) BuildStart(root);
                else if (prefabPath.EndsWith("WinStreakMainUIPanel.prefab", StringComparison.Ordinal)) BuildMain(root);
                else if (prefabPath.EndsWith("WinStreakDetailUIPanel.prefab", StringComparison.Ordinal)) BuildDetail(root);
                else if (prefabPath.EndsWith("WinStreakEndUIPanel.prefab", StringComparison.Ordinal)) BuildEnd(root);
                else throw new InvalidOperationException(prefabPath);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildMain(GameObject root)
        {
            ClearChildren(root.transform);
            Image background = EnsureImage(root);
            background.color = Color.white;
            background.sprite = LoadSprite("bg_main.png");
            background.preserveAspect = false;

            RectTransform safe = CreateNode(root.transform, "WinStreakSafeArea", Vector2.zero, Vector2.one);
            TMP_Text title = CreateText(safe, "Title", new Vector2(0.20f, 0.925f), new Vector2(0.80f, 0.975f),
                34f, TextAlignmentOptions.Center, Color.white, "HEXA STREAK");
            CreateArt(safe, "Logo", "Test_UI_05.png", new Vector2(0.27f, 0.815f),
                new Vector2(0.73f, 0.945f));
            Button close = CreateIconButton(safe, "Btn_Close", "Test_UI_37.png", new Vector2(0.035f, 0.91f),
                new Vector2(0.145f, 0.975f), Hex("2F5C9A"));
            Button details = CreateButton(safe, "DetailBtn", new Vector2(0.855f, 0.91f),
                new Vector2(0.965f, 0.975f), Hex("2F5C9A"));
            CreateText(details.transform, "Label", Vector2.zero, Vector2.one, 46f,
                TextAlignmentOptions.Center, Color.white, "?");

            RectTransform timerBg = CreateImage(safe, "CountdownBg", new Vector2(0.34f, 0.785f),
                new Vector2(0.66f, 0.825f), Hex("3767A2"));
            TMP_Text countdown = CreateText(timerBg, "Countdown", Vector2.zero, Vector2.one, 26f,
                TextAlignmentOptions.Center, Color.white, "00:00:00");
            TMP_Text progress = CreateText(safe, "Progress", new Vector2(0.06f, 0.78f),
                new Vector2(0.30f, 0.83f), 30f, TextAlignmentOptions.Center, Color.white, "0/70");
            TMP_Text next = CreateText(safe, "NextCheckpoint", new Vector2(0.70f, 0.78f),
                new Vector2(0.94f, 0.83f), 26f, TextAlignmentOptions.Center, Color.white, "NEXT 2");

            RectTransform scrollRect = CreateNode(safe, "TowerScroll", new Vector2(0.04f, 0.065f),
                new Vector2(0.96f, 0.78f));
            Image scrollBg = scrollRect.gameObject.AddComponent<Image>();
            scrollBg.color = new Color(1f, 1f, 1f, 0.025f);
            RectTransform viewport = CreateImage(scrollRect, "Viewport", Vector2.zero, Vector2.one,
                Color.white);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            RectTransform content = CreateNode(viewport, "CheckpointContent", new Vector2(0f, 1f),
                new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            // Tower artwork must meet at segment edges. Spacing used to let backgrounds and rewards
            // from separate rows overlap in the scrolling content.
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // These are layout siblings, rather than content-wide overlay artwork. Runtime clones
            // are inserted between the inactive template and TowerBottom.
            WinStreakCheckpointRowView towerTop = BuildTowerTop(content);
            WinStreakCheckpointRowView row = BuildCheckpointRow(content);
            row.gameObject.SetActive(false);
            WinStreakTowerBottomView towerBottom = BuildTowerBottom(content);
            ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 42f;

            CreateArt(safe, "Balloon", "Test_UI_31.png", new Vector2(0.72f, 0.08f),
                new Vector2(0.96f, 0.25f));
            WinStreakMainUIPanel panel = RequiredComponent<WinStreakMainUIPanel>(root);
            panel.BindSerializedReferences(title, countdown, progress, next, details, close, scroll, content,
                towerTop, row, towerBottom);
            EditorUtility.SetDirty(panel);
        }

        private static WinStreakCheckpointRowView BuildTowerTop(Transform parent)
        {
            RectTransform root = CreateArt(parent, "TowerTop", "tower_top.png", Vector2.zero, Vector2.one, false);
            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 500f;
            element.minHeight = 500f;
            WinStreakCheckpointRowView row = root.gameObject.AddComponent<WinStreakCheckpointRowView>();
            BuildRewardSegment(root, row, includeTowerMain: false, includeProgressTrack: false);
            return row;
        }

        private static WinStreakCheckpointRowView BuildCheckpointRow(Transform parent)
        {
            RectTransform root = CreateNode(parent, "CheckpointRowTemplate", Vector2.zero, Vector2.one);
            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 420f;
            element.minHeight = 420f;
            WinStreakCheckpointRowView row = root.gameObject.AddComponent<WinStreakCheckpointRowView>();
            BuildRewardSegment(root, row, includeTowerMain: true, includeProgressTrack: true);
            return row;
        }

        private static WinStreakTowerBottomView BuildTowerBottom(Transform parent)
        {
            RectTransform root = CreateArt(parent, "TowerBottom", "tower_bottom.png", Vector2.zero, Vector2.one,
                false);
            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 360f;
            element.minHeight = 360f;
            WinStreakTowerBottomView bottom = root.gameObject.AddComponent<WinStreakTowerBottomView>();

            Image progressFill = BuildProgressTrack(root, new Vector2(0.105f, 0.20f),
                new Vector2(0.150f, 0.94f));
            RectTransform claimed = CreateArt(root, "ClaimedState", "Test_UI_02.png",
                new Vector2(0.025f, 0.22f), new Vector2(0.225f, 0.53f));
            claimed.gameObject.SetActive(false);
            RectTransform badge = CreateArt(root, "CheckpointBadge", "Test_UI_33.png",
                new Vector2(0.025f, 0.22f), new Vector2(0.225f, 0.53f));
            TMP_Text checkpoint = CreateText(badge, "Checkpoint", Vector2.zero, Vector2.one, 34f,
                TextAlignmentOptions.Center, Color.white, "1");
            bottom.BindSerializedReferences(checkpoint, progressFill, claimed.gameObject, badge.gameObject);
            EditorUtility.SetDirty(bottom);
            return bottom;
        }

        private static void BuildRewardSegment(Transform root, WinStreakCheckpointRowView row,
            bool includeTowerMain, bool includeProgressTrack)
        {
            if (includeTowerMain)
                CreateArt(root, "TowerMain", "tower_main.png", new Vector2(0f, 0f),
                    new Vector2(0.42f, 1f), false);

            Image stage = CreateArt(root, "Stage", "stage_Blue.png", new Vector2(0.15f, 0.13f),
                new Vector2(0.72f, 0.83f)).GetComponent<Image>();
            Image progressFill = includeProgressTrack
                ? BuildProgressTrack(root, new Vector2(0.105f, 0.07f), new Vector2(0.150f, 0.96f))
                : null;
            // ClaimedState is deliberately beneath CheckpointBadge in the authored hierarchy.
            // The row view hides the badge when claimed, revealing this green check.
            RectTransform claimed = CreateArt(root, "ClaimedState", "Test_UI_02.png",
                new Vector2(0.025f, 0.22f), new Vector2(0.225f, 0.54f));
            claimed.gameObject.SetActive(false);
            RectTransform badge = CreateArt(root, "CheckpointBadge", "Test_UI_33.png",
                new Vector2(0.025f, 0.22f), new Vector2(0.225f, 0.54f));
            TMP_Text checkpoint = CreateText(badge, "Checkpoint", Vector2.zero, Vector2.one, 34f,
                TextAlignmentOptions.Center, Color.white, "2");
            Image chest = CreateArt(root, "Chest", "Chest/Chest_01.png", new Vector2(0.47f, 0.31f),
                new Vector2(0.75f, 0.70f)).GetComponent<Image>();
            chest.raycastTarget = true;
            Button claim = chest.gameObject.AddComponent<Button>();
            claim.targetGraphic = chest;

            RectTransform tooltip = CreateImage(root, "RewardTooltipView", new Vector2(0.52f, 0.70f),
                new Vector2(0.94f, 0.92f), new Color(1f, 1f, 1f, 0.96f));
            tooltip.gameObject.AddComponent<Canvas>();
            RewardTooltipView tooltipView = tooltip.gameObject.AddComponent<RewardTooltipView>();
            tooltipView.enabled = false;
            CreateImage(tooltip, "BG_1", Vector2.zero, Vector2.one, new Color(0.20f, 0.28f, 0.55f, 0.85f));
            CreateImage(tooltip, "BG_2", new Vector2(0.035f, 0.08f), new Vector2(0.965f, 0.92f),
                new Color(0.97f, 0.98f, 1f, 1f));
            RectTransform box = CreateNode(tooltip, "Box", new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.88f));
            HorizontalLayoutGroup boxLayout = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            boxLayout.childAlignment = TextAnchor.MiddleCenter;
            boxLayout.childControlWidth = true;
            boxLayout.childControlHeight = true;
            boxLayout.childForceExpandWidth = true;
            boxLayout.childForceExpandHeight = true;
            TMP_Text reward = CreateText(box, "Reward", Vector2.zero, Vector2.one, 18f,
                TextAlignmentOptions.Center, Hex("24385D"), "REWARD");
            reward.enableAutoSizing = true;
            reward.fontSizeMin = 11f;
            reward.fontSizeMax = 20f;
            LayoutElement rewardElement = reward.gameObject.AddComponent<LayoutElement>();
            rewardElement.flexibleWidth = 1f;

            Sprite[] chests =
            {
                LoadSprite("Chest/Chest_01.png"), LoadSprite("Chest/Chest_02.png"),
                LoadSprite("Chest/Chest_03.png"), LoadSprite("Chest/Chest_04.png"),
                LoadSprite("Chest/Chest_05.png")
            };
            Sprite[] stages =
            {
                LoadSprite("stage_Blue.png"), LoadSprite("stage_green.png"), LoadSprite("stage_red.png")
            };
            
            EditorUtility.SetDirty(row);
        }

        private static Image BuildProgressTrack(Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform track = CreateImage(parent, "ProgressTrack", min, max, Hex("24465F"));
            LayoutElement trackLayout = track.gameObject.AddComponent<LayoutElement>();
            // Segment roots use one fixed preferred height, and every track keeps this authored
            // height regardless of the number of levels needed to fill it.
            trackLayout.minHeight = 300f;
            trackLayout.preferredHeight = 300f;
            RectTransform fillRect = CreateImage(track, "Fill", Vector2.zero, Vector2.one, Hex("63E46D"));
            Image fill = fillRect.GetComponent<Image>();
            // Image adds Unity's default UISprite when created. Remove it so this remains the
            // requested pure-color rectangle, driven only by its anchors.
            fill.sprite = null;
            fill.type = Image.Type.Simple;
            fillRect.anchorMax = new Vector2(1f, 0f);
            return fill;
        }

        private static void BuildStart(GameObject root)
        {
            ClearChildren(root.transform);
            Image overlay = EnsureImage(root);
            overlay.sprite = null;
            overlay.color = new Color(0.02f, 0.09f, 0.18f, 0.78f);
            RectTransform card = CreateImage(root.transform, "WinStreakStartCard", new Vector2(0.065f, 0.13f),
                new Vector2(0.935f, 0.86f), Hex("E9F3FF"));
            CreateArt(card, "Header", "bg_main.png", new Vector2(0f, 0.63f), Vector2.one, false);
            CreateArt(card, "Logo", "Test_UI_05.png", new Vector2(0.22f, 0.78f),
                new Vector2(0.78f, 1.04f));
            TMP_Text title = CreateText(card, "Title", new Vector2(0.16f, 0.71f),
                new Vector2(0.84f, 0.79f), 30f, TextAlignmentOptions.Center, Hex("22466E"), "HEXA STREAK");
            CreateArt(card, "RulePreview", "Test_UI_16.png", new Vector2(0.14f, 0.34f),
                new Vector2(0.86f, 0.70f));
            RectTransform timerBg = CreateImage(card, "CountdownBg", new Vector2(0.30f, 0.275f),
                new Vector2(0.70f, 0.33f), Hex("356DA4"));
            TMP_Text countdown = CreateText(timerBg, "Countdown", Vector2.zero, Vector2.one, 25f,
                TextAlignmentOptions.Center, Color.white, "00:00:00");
            TMP_Text description = CreateText(card, "Description", new Vector2(0.09f, 0.14f),
                new Vector2(0.91f, 0.27f), 27f, TextAlignmentOptions.Center, Hex("294B6B"),
                "Win levels in a row to unlock rewards!");
            Button start = CreateButton(card, "StartButton", new Vector2(0.22f, 0.025f),
                new Vector2(0.78f, 0.13f), Hex("FF8A23"));
            CreateText(start.transform, "Label", Vector2.zero, Vector2.one, 38f,
                TextAlignmentOptions.Center, Color.white, "START");
            Button close = CreateIconButton(card, "Btn_Close", "Test_UI_37.png", new Vector2(0.88f, 0.90f),
                new Vector2(0.99f, 0.99f), Hex("2F5C9A"));
            WinStreakStartUIPanel panel = RequiredComponent<WinStreakStartUIPanel>(root);
            panel.BindSerializedReferences(title, countdown, description, start, close);
            EditorUtility.SetDirty(panel);
        }

        private static void BuildDetail(GameObject root)
        {
            ClearChildren(root.transform);
            Image overlay = EnsureImage(root);
            overlay.sprite = null;
            overlay.color = new Color(0.02f, 0.07f, 0.15f, 0.83f);
            RectTransform card = CreateImage(root.transform, "WinStreakDetailCard", new Vector2(0.045f, 0.045f),
                new Vector2(0.955f, 0.94f), Hex("EAF6FF"));
            CreateArt(card, "Logo", "Test_UI_05.png", new Vector2(0.23f, 0.84f),
                new Vector2(0.77f, 1.01f));
            TMP_Text heading = CreateText(card, "Heading", new Vector2(0.12f, 0.79f),
                new Vector2(0.88f, 0.86f), 32f, TextAlignmentOptions.Center, Hex("214D79"), "HOW TO PLAY");
            BuildRule(card, "Rule01", "Test_UI_16.png", new Vector2(0.07f, 0.55f), new Vector2(0.93f, 0.78f),
                "Beat levels without failing!");
            BuildRule(card, "Rule02", "Test_UI_31.png", new Vector2(0.07f, 0.31f), new Vector2(0.93f, 0.54f),
                "Maintain a streak!");
            BuildRule(card, "Rule03", "Test_UI_01.png", new Vector2(0.07f, 0.07f), new Vector2(0.93f, 0.30f),
                "Claim Rewards!");
            CreateArt(card, "Arrow01", "arrow.png", new Vector2(0.43f, 0.53f), new Vector2(0.57f, 0.59f));
            CreateArt(card, "Arrow02", "arrow.png", new Vector2(0.43f, 0.29f), new Vector2(0.57f, 0.35f));
            Button close = CreateIconButton(card, "Btn_Close", "Test_UI_37.png", new Vector2(0.88f, 0.91f),
                new Vector2(0.99f, 0.99f), Hex("2F5C9A"));
            Button continueButton = CreateButton(card, "ContinueButton", new Vector2(0.24f, 0.008f),
                new Vector2(0.76f, 0.068f), Hex("FF8A23"));
            CreateText(continueButton.transform, "Label", Vector2.zero, Vector2.one, 32f,
                TextAlignmentOptions.Center, Color.white, "TAP TO CONTINUE");
            WinStreakDetailUIPanel panel = RequiredComponent<WinStreakDetailUIPanel>(root);
            panel.BindSerializedReferences(close, continueButton);
            EditorUtility.SetDirty(panel);
        }

        private static void BuildRule(Transform parent, string name, string artName, Vector2 min, Vector2 max,
            string textValue)
        {
            RectTransform rule = CreateImage(parent, name, min, max, new Color(0.75f, 0.88f, 0.97f, 0.75f));
            CreateArt(rule, "Art", artName, new Vector2(0.03f, 0.12f), new Vector2(0.38f, 0.91f));
            CreateText(rule, "Text", new Vector2(0.40f, 0.08f), new Vector2(0.96f, 0.92f), 25f,
                TextAlignmentOptions.MidlineLeft, Hex("294B6B"), textValue);
        }

        private static void BuildEnd(GameObject root)
        {
            ClearChildren(root.transform);
            Image overlay = EnsureImage(root);
            overlay.sprite = null;
            overlay.color = new Color(0.02f, 0.08f, 0.17f, 0.80f);
            RectTransform card = CreateImage(root.transform, "WinStreakEndCard", new Vector2(0.07f, 0.17f),
                new Vector2(0.93f, 0.82f), Hex("EAF6FF"));
            CreateArt(card, "Sky", "bg_main.png", new Vector2(0f, 0.40f), Vector2.one, false);
            CreateArt(card, "Logo", "Test_UI_05.png", new Vector2(0.23f, 0.78f),
                new Vector2(0.77f, 1.03f));
            CreateArt(card, "TowerBottom", "tower_bottom.png", new Vector2(0.10f, 0.29f),
                new Vector2(0.90f, 0.66f));
            CreateArt(card, "Balloon", "Test_UI_31.png", new Vector2(0.69f, 0.48f),
                new Vector2(0.94f, 0.75f));
            TMP_Text title = CreateText(card, "Title", new Vector2(0.15f, 0.70f), new Vector2(0.85f, 0.79f),
                32f, TextAlignmentOptions.Center, Hex("214D79"), "HEXA STREAK");
            TMP_Text summary = CreateText(card, "Summary", new Vector2(0.12f, 0.14f),
                new Vector2(0.88f, 0.31f), 31f, TextAlignmentOptions.Center, Hex("294B6B"), "BEST STREAK 0");
            Button close = CreateButton(card, "Btn_Close", new Vector2(0.23f, 0.02f),
                new Vector2(0.77f, 0.13f), Hex("FF8A23"));
            CreateText(close.transform, "Label", Vector2.zero, Vector2.one, 36f,
                TextAlignmentOptions.Center, Color.white, "CONTINUE");
            WinStreakEndUIPanel panel = RequiredComponent<WinStreakEndUIPanel>(root);
            panel.BindSerializedReferences(title, summary, close);
            EditorUtility.SetDirty(panel);
        }

        private static T RequiredComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component == null) throw new InvalidOperationException(root.name + " is missing " + typeof(T).Name);
            return component;
        }

        private static Image EnsureImage(GameObject root)
        {
            Image image = root.GetComponent<Image>();
            return image != null ? image : root.AddComponent<Image>();
        }

        private static RectTransform CreateNode(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.layer = UiLayer;
            node.transform.SetParent(parent, false);
            RectTransform rect = node.GetComponent<RectTransform>();
            SetRect(rect, min, max);
            return rect;
        }

        private static RectTransform CreateImage(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            RectTransform rect = CreateNode(parent, name, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateArt(Transform parent, string name, string fileName, Vector2 min,
            Vector2 max, bool preserveAspect = true)
        {
            RectTransform rect = CreateImage(parent, name, min, max, Color.white);
            Image image = rect.GetComponent<Image>();
            image.sprite = LoadSprite(fileName);
            image.preserveAspect = preserveAspect;
            return rect;
        }

        private static Sprite LoadSprite(string fileName)
        {
            string path = TexturePath + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Win Streak sprite is missing: " + path);
            return sprite;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 min, Vector2 max,
            float size, TextAlignmentOptions alignment, Color color, string value)
        {
            RectTransform rect = CreateNode(parent, name, min, max);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value ?? string.Empty;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            RectTransform rect = CreateImage(parent, name, min, max, color);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Button CreateIconButton(Transform parent, string name, string iconName, Vector2 min,
            Vector2 max, Color color)
        {
            Button button = CreateButton(parent, name, min, max, color);
            CreateArt(button.transform, "Icon", iconName, new Vector2(0.18f, 0.18f),
                new Vector2(0.82f, 0.82f));
            return button;
        }

        private static void IgnoreLayout(Component component)
        {
            LayoutElement element = component.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(parent.GetChild(index).gameObject);
        }

        private static Color Hex(string value)
        {
            if (ColorUtility.TryParseHtmlString("#" + value, out Color color)) return color;
            return Color.white;
        }
    }
}
