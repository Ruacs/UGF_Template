using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lokas.Editor.Tests
{
    public sealed class RewardChestViewTests
    {
        private const string PrefabPath = "Assets/GameMain/UI/UIPrefabs/Reward/RewardChestView_Base.prefab";
        private GameObject m_Instance;
        private RewardChestView m_Chest;
        private RewardTooltipView m_Tooltip;
        private Button m_Button;
        private Sprite m_Icon;
        private readonly List<RewardDefinitionSO> m_Definitions = new List<RewardDefinitionSO>();

        [SetUp]
        public void SetUp()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            m_Instance = Object.Instantiate(prefab);
            m_Chest = m_Instance.GetComponent<RewardChestView>();
            m_Button = m_Instance.GetComponent<Button>();
            m_Tooltip = m_Instance.GetComponentInChildren<RewardTooltipView>(true);
            m_Icon = m_Instance.transform.Find("Root/Icon_Chest").GetComponent<Image>().sprite;
            Assert.That(m_Chest, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Instance != null)
                Object.DestroyImmediate(m_Instance);
            foreach (RewardDefinitionSO definition in m_Definitions) Object.DestroyImmediate(definition);
            m_Definitions.Clear();
        }

        [Test]
        public void PrefabBindsItsOwnButtonIconAndNestedTooltip()
        {
            var bindings = new SerializedObject(m_Chest);
            Assert.That(bindings.FindProperty("m_Button").objectReferenceValue, Is.SameAs(m_Button));
            Assert.That(bindings.FindProperty("m_ChestIcon").objectReferenceValue,
                Is.SameAs(m_Instance.transform.Find("Root/Icon_Chest").GetComponent<Image>()));
            Assert.That(bindings.FindProperty("m_Tooltip").objectReferenceValue, Is.SameAs(m_Tooltip));
            Assert.That(m_Tooltip.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(m_Instance.GetComponentsInChildren<Component>(true).Any(component => component == null), Is.False);
        }

        [Test]
        public void RebindingRefreshesPreviewAndRepeatedBindingDoesNotDuplicateClicks()
        {
            var rewards = new List<RewardEntry>
            {
                Entry("prop.hint", 2),
                Entry("prop.shuffle", 3)
            };
            for (int i = 0; i < 3; i++)
                m_Chest.Bind(m_Icon, rewards);

            rewards[0] = Entry("prop.hint", 99);
            rewards.Clear();
            Assert.That(m_Tooltip.IsVisible, Is.False);
            m_Button.onClick.Invoke();
            Assert.That(m_Tooltip.IsVisible, Is.True);
            Assert.That(VisibleAmounts(), Is.EqualTo(new[] { "×2", "×3" }));

            m_Button.onClick.Invoke();
            Assert.That(m_Tooltip.IsVisible, Is.False);
            m_Chest.Bind(m_Icon, new[] { Entry("prop.add_time", 7) });
            m_Button.onClick.Invoke();
            Assert.That(m_Tooltip.IsVisible, Is.True);
            Assert.That(VisibleAmounts(), Is.EqualTo(new[] { "×7" }));
            Assert.That(m_Tooltip.GetComponentsInChildren<RewardItemView>(true).Length, Is.EqualTo(2));

            // EditMode 下显式驱动 Unity 生命周期，验证回收/复用时的清理与重复订阅。
            InvokeLifecycle("OnDisable");
            Assert.That(m_Tooltip.IsVisible, Is.False);
            Assert.That(m_Tooltip.transform.localScale.x, Is.Zero);
            InvokeLifecycle("OnEnable");
            InvokeLifecycle("OnEnable");
            m_Button.onClick.Invoke();
            Assert.That(m_Tooltip.IsVisible, Is.True);
        }

        [Test]
        public void EmptyRewardsCannotPreviewAndTooltipConsumesItsOwnClick()
        {
            m_Chest.Bind(m_Icon, System.Array.Empty<RewardEntry>());
            Assert.That(m_Button.interactable, Is.False);
            m_Button.onClick.Invoke();
            m_Chest.ShowPreview();
            Assert.That(m_Tooltip.IsVisible, Is.False);

            m_Chest.SetRewards(new[] { Entry("prop.hint", 2) });
            m_Button.onClick.Invoke();
            Assert.That(m_Tooltip.IsVisible, Is.True);
            GameObject receiver = ExecuteEvents.ExecuteHierarchy(m_Tooltip.transform.Find("BG_1").gameObject,
                new PointerEventData(null), ExecuteEvents.pointerClickHandler);
            Assert.That(receiver, Is.SameAs(m_Tooltip.gameObject));
            Assert.That(m_Tooltip.IsVisible, Is.True);

            m_Chest.SetRewards(null);
            Assert.That(m_Tooltip.IsVisible, Is.False);
            Assert.That(m_Button.interactable, Is.False);
        }

        [Test]
        public void TooltipShowRefreshesAnAlreadyVisibleListAndClearsEmptyContent()
        {
            m_Tooltip.Show(new[] { Entry("prop.hint", 2), Entry("prop.shuffle", 3) });
            m_Tooltip.Show(new[] { Entry("prop.add_time", 9) });
            Assert.That(m_Tooltip.IsVisible, Is.True);
            Assert.That(VisibleAmounts(), Is.EqualTo(new[] { "×9" }));

            m_Tooltip.Show(null);
            Assert.That(m_Tooltip.IsVisible, Is.False);
            Assert.That(VisibleAmounts(), Is.Empty);
        }

        [Test]
        public void TooltipWidthExpandsByRewardItemWidthAfterTheSecondItem()
        {
            m_Tooltip.Show(new[] { Entry("prop.hint", 1), Entry("prop.shuffle", 1) });
            Assert.That(((RectTransform)m_Tooltip.transform).rect.width, Is.EqualTo(280f));

            m_Tooltip.Show(new[] { Entry("prop.hint", 1), Entry("prop.shuffle", 1), Entry("prop.add_time", 1) });
            Assert.That(((RectTransform)m_Tooltip.transform).rect.width, Is.EqualTo(360f));

            m_Tooltip.Show(new[] { Entry("prop.hint", 1), Entry("prop.shuffle", 1), Entry("prop.add_time", 1), Entry("prop.hint", 1) });
            Assert.That(((RectTransform)m_Tooltip.transform).rect.width, Is.EqualTo(440f));
        }

        private string[] VisibleAmounts()
        {
            return m_Tooltip.GetComponentsInChildren<RewardItemView>(true)
                .Where(item => item.gameObject.activeSelf)
                .Select(item => item.GetComponentInChildren<TMP_Text>(true).text)
                .ToArray();
        }

        private RewardEntry Entry(string key, int amount)
        {
            var definition = ScriptableObject.CreateInstance<RewardDefinitionSO>();
            definition.Configure("Game", key, key, m_Icon, RewardResourceKind.Item);
            m_Definitions.Add(definition);
            return new RewardEntry(definition, RewardGrantMode.AddQuantity, amount);
        }

        private void InvokeLifecycle(string methodName)
        {
            MethodInfo method = typeof(RewardChestView).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(m_Chest, null);
        }
    }
}
