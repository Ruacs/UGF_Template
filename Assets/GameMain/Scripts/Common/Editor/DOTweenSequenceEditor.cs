#if DOTWEEN
using DG.Tweening;
using DG.DOTweenEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static DOTweenSequence;

[CanEditMultipleObjects]
[CustomEditor(typeof(DOTweenSequence))]
public class DOTweeSequenceInspector : Editor
{
    SerializedProperty m_Sequence;
    ReorderableList m_SequenceList;

    GUIContent m_PlayBtnContent;
    GUIContent m_RewindBtnContent;
    GUIContent m_ResetBtnContent;
    private GUILayoutOption m_btnHeight;

    private void OnEnable()
    {
        m_PlayBtnContent = EditorGUIUtility.TrIconContent("d_PlayButton@2x", "播放");
        m_RewindBtnContent = EditorGUIUtility.TrIconContent("d_preAudioAutoPlayOff@2x", "倒放");
        m_ResetBtnContent = EditorGUIUtility.TrIconContent("d_preAudioLoopOff@2x", "重置");
        m_btnHeight = GUILayout.Height(35);
        m_Sequence = serializedObject.FindProperty("m_Sequence");
        m_SequenceList = new ReorderableList(serializedObject, m_Sequence);
        m_SequenceList.drawElementCallback = OnDrawSequenceItem;
        m_SequenceList.elementHeightCallback = index =>
        {
            var item = m_Sequence.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(item, GUIContent.none, true) + 4;
        };
        m_SequenceList.drawHeaderCallback = OnDrawSequenceHeader;
        m_SequenceList.drawNoneElementCallback = rect =>
        {
            EditorGUI.LabelField(rect, "No animation sequences. Use + to add one.");
        };
        m_SequenceList.onAddCallback = list =>
        {
            var index = m_Sequence.arraySize;
            m_Sequence.InsertArrayElementAtIndex(index);
            var item = m_Sequence.GetArrayElementAtIndex(index);
            item.isExpanded = true;
            list.index = index;
        };
    }

    public override void OnInspectorGUI()
    {
        DrawAnimationStateInfo();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(m_PlayBtnContent, m_btnHeight))
                {
                    if (DOTweenEditorPreview.isPreviewing)
                    {
                        DOTweenEditorPreview.Stop(true, true);
                        (target as DOTweenSequence).DOKill();
                    }
                    DOTweenEditorPreview.PrepareTweenForPreview((target as DOTweenSequence).DOPlay());
                    DOTweenEditorPreview.Start();
                }
                if (GUILayout.Button(m_RewindBtnContent, m_btnHeight))
                {
                    if (DOTweenEditorPreview.isPreviewing)
                    {
                        DOTweenEditorPreview.Stop(true, true);
                        (target as DOTweenSequence).DOKill();
                    }
                    DOTweenEditorPreview.PrepareTweenForPreview((target as DOTweenSequence).DORewind());
                    DOTweenEditorPreview.Start();
                }
                if (GUILayout.Button(m_ResetBtnContent, m_btnHeight))
                {
                    DOTweenEditorPreview.Stop(true, true);
                    (target as DOTweenSequence).DOKill();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        serializedObject.Update();
        m_SequenceList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }

    private void DrawAnimationStateInfo()
    {
        if (targets == null || targets.Length != 1)
        {
            return;
        }

        var sequence = target as DOTweenSequence;
        if (sequence == null)
        {
            return;
        }

        EditorGUILayout.HelpBox(
            "Animation State: " + DOTweenSequenceEditorUtility.GetAnimationStateLabel(sequence),
            MessageType.None);
    }

    private void OnDrawSequenceHeader(Rect rect)
    {
        var buttonWidth = 72f;
        var countWidth = 60f;
        var collapseRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
        var expandRect = new Rect(collapseRect.x - buttonWidth - 4, rect.y, buttonWidth, rect.height);
        var labelRect = new Rect(rect.x, rect.y, rect.width - buttonWidth * 2 - countWidth - 12, rect.height);
        var countRect = new Rect(labelRect.xMax, rect.y, countWidth, rect.height);

        EditorGUI.LabelField(labelRect, "Animation Sequences");
        EditorGUI.LabelField(countRect, string.Format("{0} items", m_Sequence.arraySize), EditorStyles.miniLabel);

        if (GUI.Button(expandRect, "Expand", EditorStyles.miniButtonLeft))
        {
            SetAllSequenceExpanded(true);
        }

        if (GUI.Button(collapseRect, "Collapse", EditorStyles.miniButtonRight))
        {
            SetAllSequenceExpanded(false);
        }
    }
    private void OnDrawSequenceItem(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = m_Sequence.GetArrayElementAtIndex(index);
        DrawRowBackground(rect, index, isActive);

        const float dragHandlePadding = 18f;
        rect.x += dragHandlePadding;
        rect.width -= dragHandlePadding;
        rect.y += 2;
        rect.height -= 4;
        EditorGUI.PropertyField(rect, element, true);
    }

    private static void DrawRowBackground(Rect rect, int index, bool isActive)
    {
        if (Event.current.type != EventType.Repaint) return;

        var color = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, index % 2 == 0 ? 0.035f : 0.075f)
            : new Color(0f, 0f, 0f, index % 2 == 0 ? 0.025f : 0.06f);

        if (isActive)
        {
            color = EditorGUIUtility.isProSkin
                ? new Color(0.25f, 0.45f, 0.75f, 0.28f)
                : new Color(0.24f, 0.48f, 0.9f, 0.22f);
        }

        EditorGUI.DrawRect(rect, color);
    }

    private void SetAllSequenceExpanded(bool expanded)
    {
        for (int i = 0; i < m_Sequence.arraySize; i++)
        {
            m_Sequence.GetArrayElementAtIndex(i).isExpanded = expanded;
        }
    }
}

[InitializeOnLoad]
internal static class DOTweenSequenceInspectorHeader
{
    static DOTweenSequenceInspectorHeader()
    {
        Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
    }

    private static void OnFinishedDefaultHeaderGUI(Editor editor)
    {
        if (editor == null || editor.targets == null || editor.targets.Length != 1)
        {
            return;
        }

        var sequence = editor.target as DOTweenSequence;
        if (sequence == null)
        {
            return;
        }

        EditorGUILayout.LabelField(
            "Animation State",
            DOTweenSequenceEditorUtility.GetAnimationStateLabel(sequence),
            EditorStyles.miniLabel);
    }
}

internal static class DOTweenSequenceEditorUtility
{
    public static string GetAnimationStateLabel(DOTweenSequence sequence)
    {
        if (sequence == null)
        {
            return "Unassigned";
        }

        var stateNames = new List<string>();
        var animators = sequence.GetComponents<DOTweenSequenceAnimator>();

        for (int i = 0; i < animators.Length; i++)
        {
            var animatorObject = new SerializedObject(animators[i]);
            animatorObject.Update();

            var states = animatorObject.FindProperty("m_States");
            if (states == null || !states.isArray)
            {
                continue;
            }

            for (int j = 0; j < states.arraySize; j++)
            {
                var state = states.GetArrayElementAtIndex(j);
                var stateSequence = state.FindPropertyRelative("m_Sequence");
                if (stateSequence == null || stateSequence.objectReferenceValue != sequence)
                {
                    continue;
                }

                var stateName = state.FindPropertyRelative("m_Name");
                var name = stateName == null ? string.Empty : stateName.stringValue;
                if (!string.IsNullOrEmpty(name) && !stateNames.Contains(name))
                {
                    stateNames.Add(name);
                }
            }
        }

        return stateNames.Count == 0
            ? "Unassigned"
            : string.Join(", ", stateNames);
    }
}

[CustomPropertyDrawer(typeof(SequenceAnimation))]
public class SequenceTweenMoveDrawer : PropertyDrawer
{
    private static readonly Dictionary<string, bool> s_EventFoldouts = new Dictionary<string, bool>();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var lineHeight = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded)
        {
            return lineHeight + 6;
        }

        var onPlay = property.FindPropertyRelative("OnPlay");
        var onUpdate = property.FindPropertyRelative("OnUpdate");
        var onComplete = property.FindPropertyRelative("OnComplete");
        var height = lineHeight * 12 + 6;

        if (HasInvalidTarget(property))
        {
            height += lineHeight;
        }

        if (HasInvalidToTarget(property))
        {
            height += lineHeight;
        }

        if (IsEventsExpanded(property))
        {
            height += EditorGUI.GetPropertyHeight(onPlay);
            height += EditorGUI.GetPropertyHeight(onUpdate);
            height += EditorGUI.GetPropertyHeight(onComplete);
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var target = property.FindPropertyRelative("Target");
        var addType = property.FindPropertyRelative("AddType");
        var tweenType = property.FindPropertyRelative("AnimationType");
        var toValue = property.FindPropertyRelative("ToValue");
        var useToTarget = property.FindPropertyRelative("UseToTarget");
        var toTarget = property.FindPropertyRelative("ToTarget");
        var useFromValue = property.FindPropertyRelative("UseFromValue");
        var fromValue = property.FindPropertyRelative("FromValue");
        var duration = property.FindPropertyRelative("DurationOrSpeed");
        var speedBased = property.FindPropertyRelative("SpeedBased");
        var delay = property.FindPropertyRelative("Delay");
        var customEase = property.FindPropertyRelative("CustomEase");
        var ease = property.FindPropertyRelative("Ease");
        var easeCurve = property.FindPropertyRelative("EaseCurve");
        var loops = property.FindPropertyRelative("Loops");
        var loopType = property.FindPropertyRelative("LoopType");
        var updateType = property.FindPropertyRelative("UpdateType");
        var snapping = property.FindPropertyRelative("Snapping");
        var onPlay = property.FindPropertyRelative("OnPlay");
        var onUpdate = property.FindPropertyRelative("OnUpdate");
        var onComplete = property.FindPropertyRelative("OnComplete");

        var headerRect = new Rect(position.x, position.y + 2, position.width, EditorGUIUtility.singleLineHeight);
        DrawSequenceHeader(headerRect, property, addType, tweenType, target, duration, delay, customEase, ease);
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        var lastRect = new Rect(position.x, headerRect.yMax + 2, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(lastRect, addType);

        EditorGUI.BeginChangeCheck();
        lastRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(lastRect, target);
        lastRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(lastRect, tweenType);

        if (EditorGUI.EndChangeCheck())
        {
            var fixedComType = GetFixedComponentType(target.objectReferenceValue as Component, (DOTweenType)tweenType.enumValueIndex);
            if (fixedComType != null)
            {
                target.objectReferenceValue = fixedComType;
            }
        }

        if (target.objectReferenceValue != null && null == GetFixedComponentType(target.objectReferenceValue as Component, (DOTweenType)tweenType.enumValueIndex))
        {
            lastRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.HelpBox(lastRect, string.Format("{0}不支持{1}", target.objectReferenceValue == null ? "Target" : target.objectReferenceValue.GetType().Name, tweenType.enumDisplayNames[tweenType.enumValueIndex]), MessageType.Error);
        }
        const float itemWidth = 110;
        const float setBtnWidth = 30;
        //Delay, Snapping
        lastRect.y += EditorGUIUtility.singleLineHeight;
        var horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        EditorGUI.PropertyField(horizontalRect, delay);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        snapping.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Snapping", snapping.boolValue);

        //From Value
        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;



        //ToTarget
        lastRect.y += EditorGUIUtility.singleLineHeight;
        var toRect = lastRect;
        toRect.width -= setBtnWidth + itemWidth;

        //To Value
        var dotweenTp = (DOTweenType)tweenType.enumValueIndex;
        switch (dotweenTp)
        {
            case DOTweenType.DOMoveX:
            case DOTweenType.DOMoveY:
            case DOTweenType.DOMoveZ:
            case DOTweenType.DOLocalMoveX:
            case DOTweenType.DOLocalMoveY:
            case DOTweenType.DOLocalMoveZ:
            case DOTweenType.DOAnchorPosX:
            case DOTweenType.DOAnchorPosY:
            case DOTweenType.DOAnchorPosZ:
            case DOTweenType.DOFade:
            case DOTweenType.DOCanvasGroupFade:
            case DOTweenType.DOFillAmount:
            case DOTweenType.DOValue:
            case DOTweenType.DOScaleX:
            case DOTweenType.DOScaleY:
            case DOTweenType.DOScaleZ:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    var value = fromValue.vector4Value;
                    value.x = EditorGUI.FloatField(horizontalRect, "From", value.x);
                    fromValue.vector4Value = value;
                    EditorGUI.EndDisabledGroup();

                    if (!useToTarget.boolValue)
                    {
                        value = toValue.vector4Value;
                        value.x = EditorGUI.FloatField(toRect, "To", value.x);
                        toValue.vector4Value = value;
                    }
                }
                break;
            case DOTweenType.DOAnchorPos:
            case DOTweenType.DOFlexibleSize:
            case DOTweenType.DOMinSize:
            case DOTweenType.DOPreferredSize:
            case DOTweenType.DOSizeDelta:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.Vector2Field(horizontalRect, "From", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.Vector2Field(toRect, "To", toValue.vector4Value);
                }
                break;
            case DOTweenType.DOMove:
            case DOTweenType.DOLocalMove:
            case DOTweenType.DOAnchorPos3D:
            case DOTweenType.DOScale:
            case DOTweenType.DORotate:
            case DOTweenType.DOLocalRotate:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.Vector3Field(horizontalRect, "From", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.Vector3Field(toRect, "To", toValue.vector4Value);
                }
                break;
            case DOTweenType.DOColor:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.ColorField(horizontalRect, "From", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.ColorField(toRect, "To", toValue.vector4Value);
                }
                break;
        }
        if (useToTarget.boolValue)
        {
            toTarget.objectReferenceValue = EditorGUI.ObjectField(toRect, "To", toTarget.objectReferenceValue, target.objectReferenceValue != null ? target.objectReferenceValue.GetType() : typeof(Component), true);

            if (toTarget.objectReferenceValue == null)
            {
                lastRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.HelpBox(lastRect, "To target cannot be null.", MessageType.Error);
            }
        }
        horizontalRect.x += horizontalRect.width;
        horizontalRect.width = setBtnWidth;
        if (useFromValue.boolValue && GUI.Button(horizontalRect, "Set"))
        {
            SetValueFromTarget(dotweenTp, target, fromValue);
        }
        horizontalRect.x += setBtnWidth;
        horizontalRect.width = itemWidth;
        useFromValue.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Enable", useFromValue.boolValue);

        toRect.x += toRect.width;
        toRect.width = setBtnWidth;
        if (!useToTarget.boolValue && GUI.Button(toRect, "Set"))
        {
            SetValueFromTarget(dotweenTp, target, toValue);
        }
        toRect.x += setBtnWidth;
        toRect.width = itemWidth;
        useToTarget.boolValue = EditorGUI.ToggleLeft(toRect, "ToTarget", useToTarget.boolValue);

        //Duration
        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        EditorGUI.PropertyField(horizontalRect, duration);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        speedBased.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Use Speed", speedBased.boolValue);

        //Ease
        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        if (customEase.boolValue)
            EditorGUI.PropertyField(horizontalRect, easeCurve);
        else
            EditorGUI.PropertyField(horizontalRect, ease);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        customEase.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Use Curve", customEase.boolValue);

        //Loops
        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        EditorGUI.PropertyField(horizontalRect, loops);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        EditorGUI.BeginDisabledGroup(loops.intValue == 1);
        loopType.enumValueIndex = (int)(LoopType)EditorGUI.EnumPopup(horizontalRect, (LoopType)loopType.enumValueIndex);
        EditorGUI.EndDisabledGroup();
        //UpdateType
        lastRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(lastRect, updateType);

        //Events
        lastRect.y += EditorGUIUtility.singleLineHeight;
        var eventsExpanded = IsEventsExpanded(property);
        eventsExpanded = EditorGUI.Foldout(lastRect, eventsExpanded, "Animation Events");
        s_EventFoldouts[property.propertyPath] = eventsExpanded;
        if (eventsExpanded)
        {
            //OnPlay
            lastRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(lastRect, onPlay);

            //OnUpdate
            lastRect.y += EditorGUI.GetPropertyHeight(onPlay);
            EditorGUI.PropertyField(lastRect, onUpdate);

            //OnComplete
            lastRect.y += EditorGUI.GetPropertyHeight(onUpdate);
            EditorGUI.PropertyField(lastRect, onComplete);
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    private static void DrawSequenceHeader(Rect rect, SerializedProperty property, SerializedProperty addType, SerializedProperty tweenType, SerializedProperty target, SerializedProperty duration, SerializedProperty delay, SerializedProperty customEase, SerializedProperty ease)
    {
        var foldoutRect = new Rect(rect.x, rect.y, 18, rect.height);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none);

        var index = GetSequenceIndex(property.propertyPath);
        var targetName = target.objectReferenceValue == null ? "None" : target.objectReferenceValue.name;
        var tweenName = tweenType.enumDisplayNames[tweenType.enumValueIndex];
        var addName = addType.enumDisplayNames[addType.enumValueIndex];
        var easeName = customEase.boolValue ? "Curve" : ease.enumDisplayNames[ease.enumValueIndex];
        var summary = string.Format("#{0}  {1}  {2}  ->  {3}    D:{4:0.###}  Delay:{5:0.###}  Ease:{6}",
            index + 1,
            addName,
            tweenName,
            targetName,
            duration.floatValue,
            delay.floatValue,
            easeName);

        var labelRect = new Rect(rect.x + 18, rect.y, rect.width - 18, rect.height);
        EditorGUI.LabelField(labelRect, summary, property.isExpanded ? EditorStyles.boldLabel : EditorStyles.label);
    }

    private static int GetSequenceIndex(string propertyPath)
    {
        var start = propertyPath.LastIndexOf('[');
        var end = propertyPath.LastIndexOf(']');
        if (start < 0 || end <= start) return 0;

        var indexText = propertyPath.Substring(start + 1, end - start - 1);
        int index;
        return int.TryParse(indexText, out index) ? index : 0;
    }

    private static bool IsEventsExpanded(SerializedProperty property)
    {
        bool expanded;
        return s_EventFoldouts.TryGetValue(property.propertyPath, out expanded) && expanded;
    }

    private static bool HasInvalidTarget(SerializedProperty property)
    {
        var target = property.FindPropertyRelative("Target");
        var tweenType = property.FindPropertyRelative("AnimationType");
        return target.objectReferenceValue != null && GetFixedComponentType(target.objectReferenceValue as Component, (DOTweenType)tweenType.enumValueIndex) == null;
    }

    private static bool HasInvalidToTarget(SerializedProperty property)
    {
        var useToTarget = property.FindPropertyRelative("UseToTarget");
        if (!useToTarget.boolValue) return false;

        var toTarget = property.FindPropertyRelative("ToTarget");
        return toTarget.objectReferenceValue == null;
    }

    private void SetValueFromTarget(DOTweenType tweenType, SerializedProperty target, SerializedProperty value)
    {
        if (target.objectReferenceValue == null) return;
        var targetCom = target.objectReferenceValue;
        switch (tweenType)
        {
            case DOTweenType.DOMove:
                {
                    value.vector4Value = (targetCom as Transform).position;
                    break;
                }
            case DOTweenType.DOMoveX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).position.x;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOMoveY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).position.y;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOMoveZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).position.z;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOLocalMove:
                {
                    value.vector4Value = (targetCom as Transform).localPosition;
                    break;
                }
            case DOTweenType.DOLocalMoveX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localPosition.x;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOLocalMoveY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localPosition.y;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOLocalMoveZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localPosition.z;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOAnchorPos:
                {
                    value.vector4Value = (targetCom as RectTransform).anchoredPosition;
                    break;
                }
            case DOTweenType.DOAnchorPosX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as RectTransform).anchoredPosition.x;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOAnchorPosY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as RectTransform).anchoredPosition.y;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOAnchorPosZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as RectTransform).anchoredPosition3D.z;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOAnchorPos3D:
                {
                    value.vector4Value = (targetCom as RectTransform).anchoredPosition3D;
                    break;
                }
            case DOTweenType.DOColor:
                {
                    value.vector4Value = (targetCom as UnityEngine.UI.Graphic).color;
                    break;
                }
            case DOTweenType.DOFade:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Graphic).color.a;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOCanvasGroupFade:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.CanvasGroup).alpha;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOValue:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Slider).value;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOSizeDelta:
                {
                    value.vector4Value = (targetCom as RectTransform).sizeDelta;
                    break;
                }
            case DOTweenType.DOFillAmount:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Image).fillAmount;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOFlexibleSize:
                {
                    value.vector4Value = (targetCom as LayoutElement).GetFlexibleSize();
                    break;
                }
            case DOTweenType.DOMinSize:
                {
                    value.vector4Value = (targetCom as LayoutElement).GetMinSize();
                    break;
                }
            case DOTweenType.DOPreferredSize:
                {
                    value.vector4Value = (targetCom as LayoutElement).GetPreferredSize();
                    break;
                }
            case DOTweenType.DOScale:
                {
                    value.vector4Value = (targetCom as Transform).localScale;
                    break;
                }
            case DOTweenType.DOScaleX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localScale.x;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOScaleY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localScale.y;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DOScaleZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localScale.z;
                    value.vector4Value = tmpValue;
                    break;
                }
            case DOTweenType.DORotate:
                {
                    var t = targetCom as Transform;
                    value.vector4Value = t is RectTransform ? t.localEulerAngles : t.eulerAngles;
                    break;
                }
            case DOTweenType.DOLocalRotate:
                {
                    value.vector4Value = (targetCom as Transform).localEulerAngles;
                    break;
                }
        }
    }

    private static Component GetFixedComponentType(Component com, DOTweenType tweenType)
    {
        if (com == null) return null;
        switch (tweenType)
        {
            case DOTweenType.DOMove:
            case DOTweenType.DOMoveX:
            case DOTweenType.DOMoveY:
            case DOTweenType.DOMoveZ:
            case DOTweenType.DOLocalMove:
            case DOTweenType.DOLocalMoveX:
            case DOTweenType.DOLocalMoveY:
            case DOTweenType.DOLocalMoveZ:
            case DOTweenType.DOScale:
            case DOTweenType.DOScaleX:
            case DOTweenType.DOScaleY:
            case DOTweenType.DOScaleZ:
                return com.gameObject.GetComponent<Transform>();
            case DOTweenType.DOAnchorPos:
            case DOTweenType.DOAnchorPosX:
            case DOTweenType.DOAnchorPosY:
            case DOTweenType.DOAnchorPosZ:
            case DOTweenType.DOAnchorPos3D:
            case DOTweenType.DOSizeDelta:
                return com.gameObject.GetComponent<RectTransform>();
            case DOTweenType.DOColor:
            case DOTweenType.DOFade:
                return com.gameObject.GetComponent<UnityEngine.UI.Graphic>();
            case DOTweenType.DOCanvasGroupFade:
                return com.gameObject.GetComponent<UnityEngine.CanvasGroup>();
            case DOTweenType.DOFillAmount:
                return com.gameObject.GetComponent<UnityEngine.UI.Image>();
            case DOTweenType.DOFlexibleSize:
            case DOTweenType.DOMinSize:
            case DOTweenType.DOPreferredSize:
                return com.gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
            case DOTweenType.DOValue:
                return com.gameObject.GetComponent<UnityEngine.UI.Slider>();
            case DOTweenType.DORotate:
            case DOTweenType.DOLocalRotate:
                {
                    var rectTransform = com.gameObject.GetComponent<RectTransform>();
                    return rectTransform != null ? (Component)rectTransform : com.gameObject.GetComponent<Transform>();
                }
        }
        return null;
    }
}
#endif
