using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 页面 Prefab 的示例内容生成器。
/// 正式项目中可以直接把真实页面控件放进 Prefab，并在控件上挂 GuideTarget。
/// </summary>
public class GuidePagePrefabContent : MonoBehaviour
{
    [SerializeField] private string _pageId;

    private void Awake()
    {
        if (_pageId == "PageA")
        {
            CreateTargetButton("BtnA", new Vector2(-180f, 80f));
            CreateTargetButton("BtnB", new Vector2(180f, 80f));
            CreateTargetButton("OpenPageB", new Vector2(0f, -80f));
        }
        else if (_pageId == "PageB")
        {
            CreateTargetButton("BtnB", Vector2.zero);
        }
    }

    private void CreateTargetButton(string targetId, Vector2 position)
    {
        GameObject buttonObject = new GameObject(targetId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(GuideTarget));
        buttonObject.transform.SetParent(transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(260f, 80f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.65f, 1f, 1f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 24;
        text.text = _pageId + " - " + targetId;

        GuideTarget target = buttonObject.GetComponent<GuideTarget>();
        target.Configure(_pageId, targetId, buttonObject.GetComponent<Button>());
    }
}
