using Lokas;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    [CustomPropertyDrawer(typeof(ShopItemViewEventRule))]
    public class ShopItemViewEventRuleDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight + Spacing;
            height += GetLineHeight(property.FindPropertyRelative("name"));
            height += GetLineHeight(property.FindPropertyRelative("matchMode"));

            SerializedProperty conditionProperty = GetConditionProperty(property);
            if (conditionProperty != null)
                height += GetLineHeight(conditionProperty);

            height += GetLineHeight(property.FindPropertyRelative("onMatched"));
            height += GetLineHeight(property.FindPropertyRelative("onUnmatched"));
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty nameProperty = property.FindPropertyRelative("name");
            string title = string.IsNullOrWhiteSpace(nameProperty.stringValue) ? label.text : nameProperty.stringValue;

            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, title, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            line.y += EditorGUIUtility.singleLineHeight + Spacing;
            DrawProperty(ref line, nameProperty, "Name");
            DrawProperty(ref line, property.FindPropertyRelative("matchMode"), "Match Mode");

            SerializedProperty conditionProperty = GetConditionProperty(property);
            if (conditionProperty != null)
                DrawProperty(ref line, conditionProperty, GetConditionLabel(property));

            DrawProperty(ref line, property.FindPropertyRelative("onMatched"), "On Matched");
            DrawProperty(ref line, property.FindPropertyRelative("onUnmatched"), "On Unmatched");
            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        private static void DrawProperty(ref Rect line, SerializedProperty property, string label)
        {
            float height = EditorGUI.GetPropertyHeight(property, true);
            line.height = height;
            EditorGUI.PropertyField(line, property, new GUIContent(label), true);
            line.y += height + Spacing;
        }

        private static float GetLineHeight(SerializedProperty property)
        {
            return EditorGUI.GetPropertyHeight(property, true) + Spacing;
        }

        private static SerializedProperty GetConditionProperty(SerializedProperty property)
        {
            ShopItemViewEventMatchMode mode =
                (ShopItemViewEventMatchMode)property.FindPropertyRelative("matchMode").enumValueIndex;

            switch (mode)
            {
                case ShopItemViewEventMatchMode.ViewTag:
                    return property.FindPropertyRelative("viewTag");
                case ShopItemViewEventMatchMode.PriceType:
                    return property.FindPropertyRelative("priceType");
                case ShopItemViewEventMatchMode.RewardTarget:
                    return property.FindPropertyRelative("rewardTarget");
                case ShopItemViewEventMatchMode.Style:
                    return property.FindPropertyRelative("style");
                case ShopItemViewEventMatchMode.ProductId:
                    return property.FindPropertyRelative("productId");
                case ShopItemViewEventMatchMode.ShowBonus:
                case ShopItemViewEventMatchMode.Visible:
                    return property.FindPropertyRelative("expectedBool");
                default:
                    return null;
            }
        }

        private static string GetConditionLabel(SerializedProperty property)
        {
            ShopItemViewEventMatchMode mode =
                (ShopItemViewEventMatchMode)property.FindPropertyRelative("matchMode").enumValueIndex;

            switch (mode)
            {
                case ShopItemViewEventMatchMode.ViewTag:
                    return "View Tag";
                case ShopItemViewEventMatchMode.PriceType:
                    return "Price Type";
                case ShopItemViewEventMatchMode.RewardTarget:
                    return "Reward Target";
                case ShopItemViewEventMatchMode.Style:
                    return "Style";
                case ShopItemViewEventMatchMode.ProductId:
                    return "Product Id";
                case ShopItemViewEventMatchMode.ShowBonus:
                    return "Expected Show Bonus";
                case ShopItemViewEventMatchMode.Visible:
                    return "Expected Visible";
                default:
                    return string.Empty;
            }
        }
    }
}

