using System;
using System.Collections.Generic;
using Lokas.Activities.Race.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Race.Editor
{
    internal static class RacePrefabLayoutBuilder
    {
        private const string UiPath = "Assets/GameMain/Activities/Race/UI/";
        private const string TexturePath = "Assets/GameMain/Activities/Race/Textures/SourceExport/";
        private const string BlueCarPath = TexturePath + "car_blue.png";
        private const string RedCarPath = TexturePath + "car_red.png";
        private const string AvatarPath = "Assets/GameMain/UI/UIPrefabs/Avatar/AvatarBox.prefab";
        private const string CirclePath = "Assets/GameMain/GuideMask/UI/circle.png";
        private const string AtlasPath = TexturePath + "sactx-0-2048x2048-ETC2-Race-e9e18204_660.png";
        private const int UiLayer = 5;

        public static void EnsureDocumentLayout(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            string marker = prefabPath == UiPath + "RaceMainUIPanel.prefab" ? "SafeArea" :
                prefabPath == UiPath + "RaceStartUIPanel.prefab" ? "Root/Content/RaceArt" : "Root/Content/Content_01";
            if (prefab.transform.Find(marker) == null) RebuildDocumentLayout(prefabPath);
        }

        public static void RebuildAll()
        {
            RebuildDocumentLayout(UiPath + "RaceStartUIPanel.prefab");
            RebuildDocumentLayout(UiPath + "RaceMainUIPanel.prefab");
            RebuildDocumentLayout(UiPath + "RaceDetailUIPanel.prefab");
        }

        private static void RebuildDocumentLayout(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Dictionary<string, string> textValues = CaptureTextValues(root.transform);
                if (prefabPath == UiPath + "RaceStartUIPanel.prefab") BuildStart(root);
                else if (prefabPath == UiPath + "RaceMainUIPanel.prefab") BuildMain(root);
                else if (prefabPath == UiPath + "RaceDetailUIPanel.prefab") BuildDetail(root);
                else throw new InvalidOperationException(prefabPath);
                RestoreTextValues(root.transform, textValues);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static RectTransform PreparePopup(GameObject root, Vector2 min, Vector2 max)
        {
            RectTransform panel = Required(root.transform, "Root");
            DisableLayout(panel);
            SetRect(panel, min, max);
            Transform title = panel.Find("TitleRoot");
            if (title != null) title.gameObject.SetActive(false);
            Transform groups = root.transform.Find("ButtonGroups");
            if (groups != null) groups.gameObject.SetActive(false);
            RectTransform content = Required(panel, "Content");
            DisableLayout(content);
            SetRect(content, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.95f));
            Image contentImage = content.GetComponent<Image>();
            if (contentImage != null) contentImage.color = Color.clear;
            ClearChildren(content);
            return content;
        }

        private static void BuildStart(GameObject root)
        {
            RectTransform content = PreparePopup(root, new Vector2(0.045f, 0.16f), new Vector2(0.955f, 0.80f));
            RectTransform panel = Required(root.transform, "Root");
            CreateArt(panel, "RaceLogo", "race_logo_1890.png", new Vector2(0.25f, 0.86f),
                new Vector2(0.75f, 1.10f));
            SetCloseButton(panel, new Vector2(0.90f, 0.94f), new Vector2(1.02f, 1.02f));

            CreateArt(content, "RaceArt", "race_pop_1400.png", new Vector2(0.08f, 0.29f),
                new Vector2(0.92f, 0.87f));
            RectTransform timer = CreateImage(content, "RemainTimeArea_back", new Vector2(0.33f, 0.82f),
                new Vector2(0.67f, 0.90f), new Color(0.25f, 0.39f, 0.73f, 1f));
            CreateNode(timer, "RemainTimeArea", Vector2.zero, Vector2.one);
            CreateText(timer.Find("RemainTimeArea"), "RemainTime", Vector2.zero, Vector2.one,
                34f, TextAlignmentOptions.Center, Color.white);

            RectTransform rewards = CreateImage(content, "RewardArea", new Vector2(0.22f, 0.25f),
                new Vector2(0.78f, 0.38f), new Color(0.63f, 0.37f, 0.91f, 1f));
            CreateArt(rewards, "GiftCluster", "race_step03_1989.png", new Vector2(0.06f, 0.02f),
                new Vector2(0.94f, 0.98f));
            string[] names = { "Top", "Second", "Third" };
            string[] ranks = { "rank_icon_1st_159.png", "rank_icon_2nd_71.png", "rank_icon_3rd_68.png" };
            for (int index = 0; index < 3; index++)
            {
                float left = index / 3f;
                RectTransform item = CreateNode(rewards, names[index], new Vector2(left, 0f),
                    new Vector2(left + 1f / 3f, 1f));
                CreateArt(item, "Medal", ranks[index], new Vector2(0.55f, 0.05f), new Vector2(0.88f, 0.42f));
                CreateText(item, "RewardValue", new Vector2(0.04f, 0.02f), new Vector2(0.55f, 0.30f),
                    17f, TextAlignmentOptions.Center, Color.white);
            }

            CreateText(content, "Description", new Vector2(0.13f, 0.16f), new Vector2(0.87f, 0.25f),
                34f, TextAlignmentOptions.Center, new Color(0.21f, 0.22f, 0.29f));
            RectTransform buttons = CreateNode(content, "BtnArea", new Vector2(0.22f, 0.02f),
                new Vector2(0.78f, 0.14f));
            Button start = CreateButton(buttons, "FreeStartBtn", Vector2.zero, Vector2.one,
                new Color(1f, 0.48f, 0.04f));
            CreateText(start.transform, "FreeStartLabel", Vector2.zero, Vector2.one, 55f,
                TextAlignmentOptions.Center, Color.white);
            Button normal = CreateButton(buttons, "NormalStartBtn", Vector2.zero, Vector2.one,
                new Color(1f, 0.48f, 0.04f));
            Button infinite = CreateButton(buttons, "InfiniteHeartStartBtn", Vector2.zero, Vector2.one,
                new Color(1f, 0.48f, 0.04f));
            normal.gameObject.SetActive(false);
            infinite.gameObject.SetActive(false);
            RectTransform preview = CreateNode(content, "Reward", new Vector2(0.18f, 0.23f),
                new Vector2(0.82f, 0.39f));
            preview.gameObject.SetActive(false);
        }

        private static void BuildMain(GameObject root)
        {
            for (int index = root.transform.childCount - 1; index >= 0; index--)
            {
                Transform child = root.transform.GetChild(index);
                if (child.name != "Btn_Close") UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            Image baseImage = root.GetComponent<Image>();
            if (baseImage != null) baseImage.color = Color.clear;
            RectTransform safe = CreateNode(root.transform, "SafeArea", Vector2.zero, Vector2.one);
            CreateArt(safe, "RaceTop", "race_top_1958.png", new Vector2(0f, 0.65f), Vector2.one, false);
            CreateArt(safe, "RaceLogo", "race_logo_1890.png", new Vector2(0.29f, 0.84f),
                new Vector2(0.71f, 0.965f));
            CreateImage(safe, "RoadBase", Vector2.zero, new Vector2(1f, 0.63f),
                new Color(0.96f, 0.38f, 0.39f));
            RectTransform road = CreateArt(safe, "Road", "race_road_1915.png", new Vector2(0.07f, 0f),
                new Vector2(0.93f, 0.63f), false);
            for (int lane = 0; lane < 5; lane++)
            {
                float center = (lane + 0.5f) / 5f;
                for (int dash = 0; dash < 5; dash++)
                {
                    float bottom = 0.09f + dash * 0.19f;
                    CreateArt(road, "rawLine_" + lane + "_" + dash, "race_line_1087.png",
                        new Vector2(center - 0.009f, bottom), new Vector2(center + 0.009f, bottom + 0.07f), false);
                }
            }
            RectTransform leftSide = CreateNode(safe, "WarSideLine", Vector2.zero, new Vector2(0.07f, 0.63f));
            RectTransform rightSide = CreateNode(safe, "WarSideLine_1", new Vector2(0.93f, 0f),
                new Vector2(1f, 0.63f));
            for (int index = 0; index < 8; index++)
            {
                Vector2 tileMin = new Vector2(0f, index / 8f);
                Vector2 tileMax = new Vector2(1f, (index + 1f) / 8f);
                CreateArt(leftSide, "SideTile_" + index, "race_sideline_1445.png", tileMin, tileMax, false);
                CreateArt(rightSide, "SideTile_" + index, "race_sideline_360.png", tileMin, tileMax, false);
            }
            RectTransform finish = CreateNode(safe, "GoalLine", new Vector2(0f, 0.63f),
                new Vector2(1f, 0.663f));
            for (int index = 0; index < 12; index++)
                CreateArt(finish, "Tile_" + index, "race_goalline_1374.png",
                    new Vector2(index / 12f, 0f), new Vector2((index + 1f) / 12f, 1f), false);
            RectTransform banner = CreateArt(safe, "RedLine", "race_obi_2196.png",
                new Vector2(0f, 0.663f), new Vector2(1f, 0.711f), false);
            CreateText(banner, "GoalPrefix", new Vector2(0.09f, 0f), new Vector2(0.29f, 1f),
                33f, TextAlignmentOptions.Right, Color.white);
            CreateText(banner, "GoalValue", new Vector2(0.30f, 0f), new Vector2(0.47f, 1f),
                36f, TextAlignmentOptions.Center, Color.white);
            CreateText(banner, "GoalSuffix", new Vector2(0.48f, 0f), new Vector2(0.93f, 1f),
                33f, TextAlignmentOptions.Left, Color.white);

            RectTransform rewards = CreateNode(safe, "Reward", new Vector2(0.14f, 0.74f),
                new Vector2(0.86f, 0.84f));
            CreateArt(rewards, "GiftCluster", "race_step03_1989.png", Vector2.zero, Vector2.one);
            float[] giftPositions = { 0.36f, 0.09f, 0.70f };
            for (int index = 0; index < 3; index++)
            {
                Button button = CreateButton(rewards, "Button" + (index + 1),
                    new Vector2(giftPositions[index], 0f), new Vector2(giftPositions[index] + 0.21f, 1f), Color.clear);
                CreateText(button.transform, "RewardValue", new Vector2(0f, 0f), new Vector2(1f, 0.22f),
                    18f, TextAlignmentOptions.Center, Color.white);
            }

            RectTransform timer = CreateImage(safe, "RemainTimeArea_back", new Vector2(0.38f, 0.703f),
                new Vector2(0.62f, 0.734f), new Color(0.28f, 0.43f, 0.75f));
            RectTransform timerArea = CreateNode(timer, "RemainTimeArea", Vector2.zero, Vector2.one);
            CreateText(timerArea, "RemainTime", Vector2.zero, Vector2.one, 34f,
                TextAlignmentOptions.Center, Color.white);

            RectTransform status = CreateNode(safe, "Status", new Vector2(0.17f, 0.60f),
                new Vector2(0.83f, 0.63f));
            CreateText(status, "RankValue", new Vector2(0f, 0f), new Vector2(0.13f, 1f), 22f,
                TextAlignmentOptions.Center, Color.white);
            CreateText(status, "ProgressValue", new Vector2(0.82f, 0f), Vector2.one, 22f,
                TextAlignmentOptions.Center, Color.white);
            RectTransform progressTrack = CreateImage(status, "ProgressTrack", new Vector2(0.15f, 0.30f),
                new Vector2(0.80f, 0.70f), new Color(0.17f, 0.26f, 0.38f));
            CreateImage(progressTrack, "Fill", Vector2.zero, new Vector2(0.01f, 1f),
                new Color(1f, 0.64f, 0.12f));

            RectTransform cars = CreateNode(safe, "Cars", new Vector2(0.07f, 0f), new Vector2(0.93f, 0.61f));
            for (int index = 0; index < 5; index++)
            {
                float x = index * 0.2f;
                BuildLane(cars, index, new Vector2(x, 0f), new Vector2(x + 0.2f, 1f));
            }

            RectTransform result = CreateImage(safe, "RaceResultPanel", new Vector2(0.12f, 0.25f),
                new Vector2(0.88f, 0.68f), new Color(0.17f, 0.12f, 0.40f, 0.98f));
            RectTransform resultBody = CreateNode(result, "Bg", Vector2.zero, Vector2.one);
            CreateArt(resultBody, "Gift", "race_step03_1989.png", new Vector2(0.15f, 0.40f),
                new Vector2(0.85f, 0.73f));
            CreateText(resultBody, "RankingText", new Vector2(0.17f, 0.74f), new Vector2(0.83f, 0.90f),
                48f, TextAlignmentOptions.Center, Color.white);
            CreateText(resultBody, "RewardValue", new Vector2(0.13f, 0.26f), new Vector2(0.87f, 0.39f),
                25f, TextAlignmentOptions.Center, Color.white);
            Button claim = CreateButton(resultBody, "NextBtn", new Vector2(0.25f, 0.08f),
                new Vector2(0.75f, 0.22f), new Color(1f, 0.48f, 0.04f));
            CreateText(claim.transform, "NextBtnText", Vector2.zero, Vector2.one, 40f,
                TextAlignmentOptions.Center, Color.white);
            result.gameObject.SetActive(false);

            Button detail = CreateButton(safe, "DetailBtn", new Vector2(0.87f, 0.925f),
                new Vector2(0.97f, 0.976f), new Color(0.30f, 0.40f, 0.65f));
            detail.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            CreateText(detail.transform, "DetailBtnText", Vector2.zero, Vector2.one, 52f,
                TextAlignmentOptions.Center, Color.white);
            SetCloseButton(root.transform, new Vector2(0.04f, 0.925f), new Vector2(0.15f, 0.976f));
            CreateNode(safe, "GIftBoxAnimV2", Vector2.zero, Vector2.one).gameObject.SetActive(false);
            CreateNode(safe, "ConfettiBlast", Vector2.zero, Vector2.one).gameObject.SetActive(false);
        }

        private static void BuildLane(Transform parent, int index, Vector2 min, Vector2 max)
        {
            RectTransform lane = CreateNode(parent, index == 0 ? "RaceCars" : "RaceCars_" + index, min, max);
            RaceLaneView laneView = lane.gameObject.AddComponent<RaceLaneView>();
            laneView.SetLaneIndex(index);
            RectTransform handle = CreateNode(lane, "Handle", Vector2.zero, Vector2.one);
            RectTransform raceCars = CreateNode(handle, "RaceCars", Vector2.zero, Vector2.one);
            RectTransform car = CreateCarImage(raceCars, index == 2 ? RedCarPath : BlueCarPath,
                new Vector2(0.24f, 0.17f), new Vector2(0.76f, 0.32f));
            RectTransform rank = CreateImage(raceCars, "Rank", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Color.white);
            rank.sizeDelta = new Vector2(80f, 80f);
            CreateText(rank, "RankValue", Vector2.zero, Vector2.one, 20f,
                TextAlignmentOptions.Center, Color.black);
            RectTransform info = CreateNode(lane, "Infos", Vector2.zero, new Vector2(1f, 0.15f));
            RectTransform frame = CreateNode(info, "PlayerFrame", new Vector2(0.20f, 0.46f),
                new Vector2(0.80f, 0.96f));
            GameObject avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);
            if (avatarPrefab == null) throw new InvalidOperationException(AvatarPath);
            GameObject avatar = (GameObject)PrefabUtility.InstantiatePrefab(avatarPrefab, frame);
            SetRect(avatar.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            RectTransform names = CreateNode(info, "Names", new Vector2(0f, 0.08f),
                new Vector2(1f, 0.31f));
            CreateText(names, "PlayerName", Vector2.zero, Vector2.one, 20f,
                TextAlignmentOptions.Center, Color.white);

            laneView.BindSerializedReferences(names.Find("PlayerName").GetComponent<TMP_Text>(),
                rank.Find("RankValue").GetComponent<TMP_Text>(), car.GetComponent<Image>(),
                avatar.GetComponent<UI_AvatarBox>(), rank, car);
        }

        private static RectTransform CreateCarImage(Transform parent, string spritePath, Vector2 min, Vector2 max)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) throw new InvalidOperationException(spritePath);
            RectTransform rect = CreateNode(parent, "RaceCar", min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return rect;
        }

        private static void BuildDetail(GameObject root)
        {
            RectTransform content = PreparePopup(root, new Vector2(0.055f, 0.04f),
                new Vector2(0.945f, 0.88f));
            RectTransform panel = Required(root.transform, "Root");
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null) panelImage.color = new Color(0.11f, 0.11f, 0.13f, 1f);
            CreateArt(content, "RaceLogo", "race_logo_1890.png", new Vector2(0.25f, 0.81f),
                new Vector2(0.75f, 0.98f));
            RectTransform first = CreateNode(content, "Content_01", new Vector2(0.06f, 0.60f),
                new Vector2(0.52f, 0.79f));
            CreateArt(first, "Peace01", "race_step01_1834.png", new Vector2(0f, 0.16f), Vector2.one);
            CreateText(first, "Text (TMP)", new Vector2(0f, 0f), new Vector2(1f, 0.20f),
                32f, TextAlignmentOptions.Center, Color.white);
            RectTransform second = CreateNode(content, "Content_02", new Vector2(0.45f, 0.39f),
                new Vector2(0.96f, 0.60f));
            CreateUvImage(second, "Peace01_1", AtlasPath, new Rect(0.405f, 0.31f, 0.245f, 0.19f),
                new Vector2(0.02f, 0.15f), new Vector2(0.98f, 1f));
            CreateText(second, "Text (TMP)_1", new Vector2(0f, 0f), new Vector2(1f, 0.20f),
                32f, TextAlignmentOptions.Center, Color.white);
            CreateArrow(content, "Arrow_01", new Vector2(0.48f, 0.55f),
                new Vector2(0.70f, 0.66f), 0f);
            CreateArrow(content, "Arrow_02", new Vector2(0.30f, 0.34f),
                new Vector2(0.52f, 0.45f), -25f);
            RectTransform third = CreateNode(content, "Content_03", new Vector2(0.06f, 0.13f),
                new Vector2(0.60f, 0.37f));
            CreateArt(third, "Peace01_2", "race_step03_1989.png", new Vector2(0f, 0.15f), Vector2.one);
            CreateText(third, "Text (TMP)_2", new Vector2(0f, 0f), new Vector2(1f, 0.20f),
                32f, TextAlignmentOptions.Center, Color.white);
            SetCloseButton(panel, new Vector2(0.23f, 0.03f), new Vector2(0.77f, 0.12f), true);
        }

        private static void SetCloseButton(Transform parent, Vector2 min, Vector2 max, bool transparent = false)
        {
            RectTransform close = Required(parent, "Btn_Close");
            DisableLayout(close);
            SetRect(close, min, max);
            close.SetAsLastSibling();
            Image image = close.GetComponent<Image>();
            if (image != null)
            {
                image.color = transparent ? Color.clear : new Color(0.26f, 0.37f, 0.61f, 1f);
                if (parent.GetComponent<RaceMainUIPanel>() != null)
                {
                    image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
                    image.type = Image.Type.Simple;
                }
            }
            ClearChildren(close);
            CreateText(close, transparent ? "ContinueText" : "CloseIcon", Vector2.zero, Vector2.one,
                transparent ? 42f : 70f, TextAlignmentOptions.Center, Color.white);
        }

        private static RectTransform Required(Transform parent, string path)
        {
            Transform found = parent.Find(path);
            if (found == null) throw new InvalidOperationException(parent.name + "/" + path);
            return (RectTransform)found;
        }

        private static void DisableLayout(Component component)
        {
            VerticalLayoutGroup vertical = component.GetComponent<VerticalLayoutGroup>();
            if (vertical != null) vertical.enabled = false;
            HorizontalLayoutGroup horizontal = component.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null) horizontal.enabled = false;
            ContentSizeFitter fitter = component.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            LayoutElement element = component.GetComponent<LayoutElement>();
            if (element != null) element.enabled = false;
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
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath + fileName);
            if (sprite == null) throw new InvalidOperationException(TexturePath + fileName);
            RectTransform rect = CreateImage(parent, name, min, max, Color.white);
            Image image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            return rect;
        }

        private static RectTransform CreateUvImage(Transform parent, string name, string texturePath,
            Rect uv, Vector2 min, Vector2 max)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) throw new InvalidOperationException(texturePath);
            RectTransform rect = CreateNode(parent, name, min, max);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.uvRect = uv;
            image.raycastTarget = false;
            return rect;
        }

        private static void CreateArrow(Transform parent, string name, Vector2 min, Vector2 max, float angle)
        {
            Color color = new Color(1f, 0.59f, 0.12f);
            RectTransform arrow = CreateNode(parent, name, min, max);
            arrow.localEulerAngles = new Vector3(0f, 0f, angle);
            CreateImage(arrow, "Stem", new Vector2(0.46f, 0.24f), new Vector2(0.54f, 0.95f), color);
            RectTransform left = CreateImage(arrow, "HeadLeft", new Vector2(0.28f, 0.15f),
                new Vector2(0.52f, 0.24f), color);
            left.localEulerAngles = new Vector3(0f, 0f, -45f);
            RectTransform right = CreateImage(arrow, "HeadRight", new Vector2(0.48f, 0.15f),
                new Vector2(0.72f, 0.24f), color);
            right.localEulerAngles = new Vector3(0f, 0f, 45f);
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 min, Vector2 max,
            float size, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateNode(parent, name, min, max);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = string.Empty;
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
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            rect.GetComponent<Image>().raycastTarget = true;
            return button;
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

        private static Dictionary<string, string> CaptureTextValues(Transform root)
        {
            var values = new Dictionary<string, string>();
            CaptureTextValues(root, root, values);
            return values;
        }

        private static void CaptureTextValues(Transform root, Transform current, Dictionary<string, string> values)
        {
            TMP_Text text = current.GetComponent<TMP_Text>();
            if (text != null && !string.IsNullOrEmpty(text.text))
                values[RelativePath(root, current)] = text.text;
            for (int index = 0; index < current.childCount; index++)
                CaptureTextValues(root, current.GetChild(index), values);
        }

        private static void RestoreTextValues(Transform root, IReadOnlyDictionary<string, string> values)
        {
            foreach (KeyValuePair<string, string> pair in values)
            {
                Transform target = root.Find(pair.Key);
                TMP_Text text = target == null ? null : target.GetComponent<TMP_Text>();
                if (text != null) text.text = pair.Value;
            }
        }

        private static string RelativePath(Transform root, Transform child)
        {
            var segments = new Stack<string>();
            for (Transform current = child; current != null && current != root; current = current.parent)
                segments.Push(current.name);
            return string.Join("/", segments);
        }
    }
}
