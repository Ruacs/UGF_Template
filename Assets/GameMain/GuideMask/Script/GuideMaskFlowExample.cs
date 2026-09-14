using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 完整示例：
/// PageA.BtnA -> PageA.BtnB -> PageA.OpenPageB -> PageB.BtnB。
/// OpenPageB 的点击监听负责打开 PageB，GuideRunner 只等待 PageB Ready。
/// </summary>
public class GuideMaskFlowExample : MonoBehaviour
{
    [SerializeField] private GuideRunner _runner;
    [SerializeField] private GameObject _pageAPrefab;
    [SerializeField] private GameObject _pageBPrefab;
    [SerializeField] private bool _playOnStart = true;

    private GameObject _pageA;
    private GameObject _pageB;

    private IEnumerator Start()
    {
        yield return null;
        SetupPageA();

        if (_playOnStart)
        {
            PlayExample();
        }
    }

    public void PlayExample()
    {
        if (_runner == null)
        {
            _runner = GetComponent<GuideRunner>();
        }

        List<GuideStep> steps = new List<GuideStep>
        {
            GuideStep.FocusTarget(
                "PageA", "BtnA", hintText: "先点击这里开始",
                hintPosition: GuideHintPosition.Bottom),
            GuideStep.FocusTarget(
                "PageA", "BtnB", hintText: "然后点击这里",
                hintPosition: GuideHintPosition.Bottom),
            GuideStep.FocusTarget(
                "PageA", "OpenPageB", hintText: "点击后进入下一页",
                hintPosition: GuideHintPosition.Top),
            GuideStep.WaitPage("PageB"),
            GuideStep.FocusTarget(
                "PageB", "BtnB", hintText: "在页面 B 中点击这个按钮",
                hintPosition: GuideHintPosition.Top),
            GuideStep.CompleteStep()
        };

        _runner.Play(steps);
    }

    private void SetupPageA()
    {
        if (_runner == null)
        {
            _runner = gameObject.GetComponent<GuideRunner>();
        }

        if (_pageAPrefab != null && _pageA == null)
        {
            Button[] oldButtons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < oldButtons.Length; i++)
            {
                oldButtons[i].gameObject.SetActive(false);
            }

            _pageA = Instantiate(_pageAPrefab, transform, false);
            _pageA.name = "PageA";
            _pageA.transform.SetAsFirstSibling();
        }

        GameObject pageRoot = _pageA != null ? _pageA : gameObject;
        GuidePage pageA = pageRoot.GetComponent<GuidePage>();
        if (pageA == null)
        {
            pageA = pageRoot.AddComponent<GuidePage>();
        }

        pageA.Configure("PageA");

        Button[] buttons = pageRoot.GetComponentsInChildren<Button>(true);
        if (buttons.Length < 2)
        {
            Debug.LogWarning("GuideMaskFlowExample: PageA 至少需要两个 Button。", this);
            return;
        }

        AddTarget(buttons[0], "PageA", "BtnA");
        AddTarget(buttons[1], "PageA", "BtnB");

        Button openPageButton = FindTargetButton(pageRoot, "OpenPageB");
        if (openPageButton == null)
        {
            openPageButton = CreateOpenPageButton(buttons[1].transform.parent as RectTransform);
        }

        AddTarget(openPageButton, "PageA", "OpenPageB");
        openPageButton.onClick.AddListener(OpenPageB);
    }

    private static Button FindTargetButton(GameObject root, string targetId)
    {
        GuideTarget[] targets = root.GetComponentsInChildren<GuideTarget>(true);
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i].TargetId == targetId)
            {
                return targets[i].GetComponent<Button>();
            }
        }

        return null;
    }

    private void OpenPageB()
    {
        if (_pageB == null)
        {
            _pageB = _pageBPrefab != null
                ? Instantiate(_pageBPrefab, transform, false)
                : CreatePageB();

            _pageB.name = "PageB";
            _pageB.transform.SetAsFirstSibling();
        }

        _pageB.SetActive(true);
    }

    private Button CreateOpenPageButton(RectTransform parent)
    {
        GameObject buttonObject = CreateButtonObject("OpenPageB", parent, new Vector2(0f, -260f));
        Text label = buttonObject.GetComponentInChildren<Text>();
        label.text = "Open Page B";
        return buttonObject.GetComponent<Button>();
    }

    private GameObject CreatePageB()
    {
        RectTransform parent = transform as RectTransform;
        GameObject page = new GameObject("PageB", typeof(RectTransform), typeof(Image), typeof(GuidePage));
        page.transform.SetParent(parent, false);
        page.transform.SetAsFirstSibling();

        RectTransform pageRect = page.GetComponent<RectTransform>();
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = Vector2.zero;
        pageRect.offsetMax = Vector2.zero;

        Image background = page.GetComponent<Image>();
        background.color = new Color(0.12f, 0.2f, 0.32f, 0.96f);
        page.GetComponent<GuidePage>().Configure("PageB");

        GameObject buttonObject = CreateButtonObject("PageB_BtnB", pageRect, Vector2.zero);
        Button button = buttonObject.GetComponent<Button>();
        buttonObject.GetComponentInChildren<Text>().text = "Page B - BtnB";
        AddTarget(button, "PageB", "BtnB");

        page.SetActive(false);
        return page;
    }

    private GameObject CreateButtonObject(string name, RectTransform parent, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
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
        text.text = name;

        return buttonObject;
    }

    private static GuideTarget AddTarget(Button button, string pageId, string targetId)
    {
        GuideTarget target = button.GetComponent<GuideTarget>();
        if (target == null)
        {
            target = button.gameObject.AddComponent<GuideTarget>();
        }

        target.Configure(pageId, targetId, button);
        return target;
    }
}
