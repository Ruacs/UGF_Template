// Editor/TaskDataSOEditor.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TaskDataSO))]
public class TaskDataSOEditor : Editor
{
    // 缓存所有子类（只扫描一次）
    private static List<Type> _conditionTypes;
    private static List<Type> _rewardTypes;

    private SerializedProperty _taskId;
    private SerializedProperty _title;
    private SerializedProperty _description;
    private SerializedProperty _taskType;
    private SerializedProperty _isRepeatable;
    private SerializedProperty _prerequisiteIds;
    private SerializedProperty _conditions;
    private SerializedProperty _rewards;

    private void OnEnable()
    {
        _taskId          = serializedObject.FindProperty("taskId");
        _title           = serializedObject.FindProperty("title");
        _description     = serializedObject.FindProperty("description");
        _taskType        = serializedObject.FindProperty("taskType");
        _isRepeatable    = serializedObject.FindProperty("isRepeatable");
        _prerequisiteIds = serializedObject.FindProperty("prerequisiteIds");
        _conditions      = serializedObject.FindProperty("conditions");
        _rewards         = serializedObject.FindProperty("rewards");

        _conditionTypes ??= FindSubTypes<ITaskCondition, TaskConditionAttribute>();
        _rewardTypes    ??= FindSubTypes<ITaskReward, TaskRewardAttribute>();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── 基础信息 ──────────────────────────────
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_taskId);
        EditorGUILayout.PropertyField(_title);
        EditorGUILayout.PropertyField(_description);
        EditorGUILayout.PropertyField(_taskType);
        EditorGUILayout.PropertyField(_isRepeatable);
        EditorGUILayout.PropertyField(_prerequisiteIds, true);

        EditorGUILayout.Space(8);

        // ── 条件列表 ──────────────────────────────
        DrawPolymorphicList(
            _conditions,
            "任务条件",
            _conditionTypes,
            typeof(TaskConditionAttribute),
            t => ((TaskConditionAttribute)t.GetCustomAttribute(typeof(TaskConditionAttribute)))?.DisplayName ?? t.Name
        );

        EditorGUILayout.Space(8);

        // ── 奖励列表 ──────────────────────────────
        DrawPolymorphicList(
            _rewards,
            "任务奖励",
            _rewardTypes,
            typeof(TaskRewardAttribute),
            t => ((TaskRewardAttribute)t.GetCustomAttribute(typeof(TaskRewardAttribute)))?.DisplayName ?? t.Name
        );

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPolymorphicList(
        SerializedProperty listProp,
        string label,
        List<Type> types,
        Type attrType,
        Func<Type, string> nameGetter)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        // 展开每个元素
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elem = listProp.GetArrayElementAtIndex(i);
            var managedRef = elem.managedReferenceValue;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                // 标题行：类型名 + 删除按钮
                using (new EditorGUILayout.HorizontalScope())
                {
                    string typeName = managedRef != null
                        ? (nameGetter(managedRef.GetType()))
                        : "(null)";
                    EditorGUILayout.LabelField(typeName, EditorStyles.miniBoldLabel);

                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        listProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                // 展开字段
                if (managedRef != null)
                {
                    EditorGUI.indentLevel++;
                    var child = elem.Copy();
                    var end   = elem.GetEndProperty();
                    child.NextVisible(true);
                    while (!SerializedProperty.EqualContents(child, end))
                    {
                        EditorGUILayout.PropertyField(child, true);
                        child.NextVisible(false);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        EditorGUILayout.Space(4);

        // ── 添加按钮（下拉菜单）──────────────────
        if (GUILayout.Button($"+ 添加{label}"))
        {
            var menu = new GenericMenu();
            foreach (var t in types)
            {
                var type = t; // 闭包捕获
                menu.AddItem(new GUIContent(nameGetter(type)), false, () =>
                {
                    serializedObject.Update();
                    listProp.arraySize++;
                    var newElem = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                    newElem.managedReferenceValue = Activator.CreateInstance(type);
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }

    // 反射扫描所有打了指定 Attribute 的 IInterface 子类
    private static List<Type> FindSubTypes<TInterface, TAttr>() where TAttr : Attribute
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .Where(t => typeof(TInterface).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract
                     && t.GetCustomAttribute<TAttr>() != null)
            .ToList();
    }
}
#endif