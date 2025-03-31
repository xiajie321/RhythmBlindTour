#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditorInternal;

[CustomEditor(typeof(TestTTS_ValueSpeakerBase), true)]
[CanEditMultipleObjects]
public class TestTTS_ValueSpeakerBaseEditor : Editor
{
    private ReorderableList customValuesList;
    private SerializedProperty customValuesProp;
    string _theLine = "————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————";

    void OnEnable()
    {
        // 本脚本针对 TestTTS_ValueSpeakerBase 中的 "ValueArray" 字段进行绘制
        customValuesProp = serializedObject.FindProperty("ValueArray");
        customValuesList = new ReorderableList(serializedObject, customValuesProp, true, true, true, true);

        customValuesList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Custom Values");
        };

        customValuesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = customValuesProp.GetArrayElementAtIndex(index);
            // 绘制按钮区域，不显示组名
            Rect buttonRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            SerializedProperty sourceProp = element.FindPropertyRelative("Source");
            float btnWidth = 40;
            float spacing = 5;
            float x = buttonRect.x;
            Color defaultBG = GUI.backgroundColor;

            // "Text" 按钮
            if (sourceProp.enumValueIndex == (int)CustomValueSource.Text)
                GUI.backgroundColor = Color.green;
            if (GUI.Button(new Rect(x, buttonRect.y, btnWidth, buttonRect.height), "Text"))
                sourceProp.enumValueIndex = (int)CustomValueSource.Text;
            GUI.backgroundColor = defaultBG;
            x += btnWidth + spacing;

            // "T-TMP" 按钮
            if (sourceProp.enumValueIndex == (int)CustomValueSource.T_TMP)
                GUI.backgroundColor = Color.green;
            if (GUI.Button(new Rect(x, buttonRect.y, btnWidth + 10, buttonRect.height), "T-TMP"))
                sourceProp.enumValueIndex = (int)CustomValueSource.T_TMP;
            GUI.backgroundColor = defaultBG;
            x += (btnWidth + 10) + spacing;

            // "Slider" 按钮
            if (sourceProp.enumValueIndex == (int)CustomValueSource.Slider)
                GUI.backgroundColor = Color.green;
            if (GUI.Button(new Rect(x, buttonRect.y, btnWidth + 10, buttonRect.height), "Slider"))
                sourceProp.enumValueIndex = (int)CustomValueSource.Slider;
            GUI.backgroundColor = defaultBG;
            x += (btnWidth + 10) + spacing;

            // "Default" 按钮
            if (sourceProp.enumValueIndex == (int)CustomValueSource.Default)
                GUI.backgroundColor = Color.green;
            if (GUI.Button(new Rect(x, buttonRect.y, btnWidth + 20, buttonRect.height), "Default"))
                sourceProp.enumValueIndex = (int)CustomValueSource.Default;
            GUI.backgroundColor = defaultBG;

            // 根据 Source 显示对应的变量（不显示标签）
            float yPos = rect.y + EditorGUIUtility.singleLineHeight;
            Rect fieldRect = new Rect(rect.x, yPos, rect.width, EditorGUIUtility.singleLineHeight);
            switch (sourceProp.enumValueIndex)
            {
                case (int)CustomValueSource.Text:
                    {
                        SerializedProperty targetTextProp = element.FindPropertyRelative("TargetText");
                        fieldRect.height = EditorGUI.GetPropertyHeight(targetTextProp, true);
                        EditorGUI.PropertyField(fieldRect, targetTextProp, GUIContent.none);
                    }
                    break;
                case (int)CustomValueSource.T_TMP:
                    {
                        SerializedProperty targetTMPProp = element.FindPropertyRelative("TargetTextTMP");
                        fieldRect.height = EditorGUI.GetPropertyHeight(targetTMPProp, true);
                        EditorGUI.PropertyField(fieldRect, targetTMPProp, GUIContent.none);
                    }
                    break;
                case (int)CustomValueSource.Slider:
                    {
                        SerializedProperty targetSliderProp = element.FindPropertyRelative("TargetSlider");
                        fieldRect.height = EditorGUI.GetPropertyHeight(targetSliderProp, true);
                        EditorGUI.PropertyField(fieldRect, targetSliderProp, GUIContent.none);
                    }
                    break;
                case (int)CustomValueSource.Default:
                    {
                        SerializedProperty defaultValueProp = element.FindPropertyRelative("DefaultValue");
                        fieldRect.height = EditorGUI.GetPropertyHeight(defaultValueProp, true);
                        EditorGUI.PropertyField(fieldRect, defaultValueProp, GUIContent.none);
                    }
                    break;
                default:
                    {
                        fieldRect.height = EditorGUIUtility.singleLineHeight;
                    }
                    break;
            }

            // redgin: 在 CustomValue 下方添加 MP3 朗读设置，将 toggle 和 newPath 合并到同一栏，并根据 toggle 状态调整背景颜色
            float mp3Y = yPos + fieldRect.height;
            Rect mp3Rect = new Rect(rect.x, mp3Y, rect.width, EditorGUIUtility.singleLineHeight);
            float clipWidth = 100;  // AudioClip 字段宽度
            Rect clipRect = new Rect(mp3Rect.x, mp3Rect.y, clipWidth, mp3Rect.height);
            EditorGUI.PropertyField(clipRect, element.FindPropertyRelative("Custom_AudioClip"), GUIContent.none);

            // Combined区域用于显示 toggle 和 newPath
            Rect combinedRect = new Rect(mp3Rect.x + clipWidth + spacing, mp3Rect.y, mp3Rect.width - clipWidth - spacing, mp3Rect.height);
            SerializedProperty toggleProp = element.FindPropertyRelative("Toggle");
            SerializedProperty customAudioNewPathProp = element.FindPropertyRelative("Custom_AudioClipNewPath");
            bool toggleValue = toggleProp.boolValue;

            // 保存原始背景色并根据 toggle 状态设置背景色
            Color originalBG = GUI.backgroundColor;
            GUI.backgroundColor = toggleValue ? Color.green : originalBG;

            float toggleWidth = 20; // toggle的宽度
            Rect innerToggleRect = new Rect(combinedRect.x, combinedRect.y, toggleWidth, combinedRect.height);
            Rect innerNewPathRect = new Rect(combinedRect.x + toggleWidth + spacing, combinedRect.y, combinedRect.width - toggleWidth - spacing, combinedRect.height);
            EditorGUI.PropertyField(innerToggleRect, toggleProp, GUIContent.none);
            EditorGUI.PropertyField(innerNewPathRect, customAudioNewPathProp, GUIContent.none);

            // 重置背景色
            GUI.backgroundColor = originalBG;
        };

        customValuesList.elementHeightCallback = (int index) =>
        {
            SerializedProperty element = customValuesProp.GetArrayElementAtIndex(index);
            SerializedProperty sourceProp = element.FindPropertyRelative("Source");
            float propertyHeight = 0f;
            switch (sourceProp.enumValueIndex)
            {
                case (int)CustomValueSource.Text:
                    propertyHeight = EditorGUI.GetPropertyHeight(element.FindPropertyRelative("TargetText"), true);
                    break;
                case (int)CustomValueSource.T_TMP:
                    propertyHeight = EditorGUI.GetPropertyHeight(element.FindPropertyRelative("TargetTextTMP"), true);
                    break;
                case (int)CustomValueSource.Slider:
                    propertyHeight = EditorGUI.GetPropertyHeight(element.FindPropertyRelative("TargetSlider"), true);
                    break;
                case (int)CustomValueSource.Default:
                    propertyHeight = EditorGUI.GetPropertyHeight(element.FindPropertyRelative("DefaultValue"), true);
                    break;
                default:
                    propertyHeight = EditorGUIUtility.singleLineHeight;
                    break;
            }
            // 总高度 = 按钮行 + 变量行 + MP3设置行
            return EditorGUIUtility.singleLineHeight + propertyHeight + EditorGUIUtility.singleLineHeight;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 定义常用样式
        GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = EditorStyles.label.fontSize + 1,
            normal = { textColor = TestTTS_StaticActionUAP.titleColor }
        };
        GUIStyle propertyStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = EditorStyles.label.fontSize,
            normal = { textColor = Color.white }
        };
        GUIStyle separatorStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = EditorStyles.label.fontSize + 1,
            normal = { textColor = TestTTS_StaticActionUAP.separatorColor }
        };
        GUIStyle messageStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = EditorStyles.label.fontSize - 1,
            normal = { textColor = Color.gray },
            wordWrap = true
        };
        GUIStyle previewStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            richText = true,
            normal = { textColor = Color.cyan },
            wordWrap = true
        };

        // 绘制分隔线
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));

        // ------ Read Mode 部分 ------
        EditorGUILayout.LabelField("------ Read Mode ------", titleStyle);

        #region OnSelected 开关设置
        SerializedProperty enableProp = serializedObject.FindProperty("EnableOnSelected");
        GUIContent toggleContent = new GUIContent(enableProp.boolValue
            ? "<color=lime>Enable OnSelected</color>"
            : "<color=#800000>Enable OnSelected</color>");
        GUIStyle toggleLabelStyle = new GUIStyle(EditorStyles.label) { richText = true };
        if (!enableProp.boolValue)
        {
            toggleLabelStyle.normal.background = MakeTex(2, 2, new Color(0.9f, 0.9f, 0.9f));
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(toggleContent, toggleLabelStyle, GUILayout.Width(150));
        bool newEnable = EditorGUILayout.Toggle(enableProp.boolValue, GUILayout.Width(20));
        EditorGUILayout.EndHorizontal();
        if (newEnable != enableProp.boolValue)
            enableProp.boolValue = newEnable;
        #endregion

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Read Target", propertyStyle, GUILayout.Width(100));
        SerializedProperty readTargetProp = serializedObject.FindProperty("ReadTarget");
        int current = readTargetProp.enumValueIndex;
        string[] options = new string[] { "Text", "Custom", "None" };
        int newIndex = GUILayout.Toolbar(current, options);
        if (newIndex != current)
            readTargetProp.enumValueIndex = newIndex;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("// 点击按钮选择朗读模式", messageStyle);

        EditorGUILayout.Space();
        TestTTS_ValueSpeakerBase speakerBase = (TestTTS_ValueSpeakerBase)target;
        if (speakerBase.ReadTarget == TestTTS_ValueReadMode.Text)
        {
            EditorGUILayout.LabelField("------ Input Text ------", titleStyle);
            SerializedProperty targetTMPInputProp = serializedObject.FindProperty("TargetTMPText");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Test(TMP)", propertyStyle, GUILayout.Width(150));
            EditorGUILayout.PropertyField(targetTMPInputProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            SerializedProperty targetInputProp = serializedObject.FindProperty("TargetText");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Text", propertyStyle, GUILayout.Width(150));
            EditorGUILayout.PropertyField(targetInputProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("// 这里配置用于朗读的输入框文本", messageStyle);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("------ Speech Settings ------", titleStyle);
            SerializedProperty prefixProp = serializedObject.FindProperty("ReadPrefix");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefix", propertyStyle, GUILayout.Width(100));
            EditorGUILayout.PropertyField(prefixProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            SerializedProperty suffixProp = serializedObject.FindProperty("ReadSuffix");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Suffix", propertyStyle, GUILayout.Width(100));
            EditorGUILayout.PropertyField(suffixProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("// 前缀和后缀仅在 Text 模式下生效", messageStyle);

            string baseText = "";
            if (speakerBase.TargetTMPText != null)
                baseText = speakerBase.TargetTMPText.text;
            else if (speakerBase.TargetText != null)
                baseText = speakerBase.TargetText.text;
            string symbolColor = "#666666";
            string preview = string.IsNullOrEmpty(prefixProp.stringValue)
                ? baseText
                : $"{prefixProp.stringValue}<color={symbolColor}>,</color> {baseText}";
            if (!string.IsNullOrEmpty(suffixProp.stringValue))
                preview += $"<color={symbolColor}>,</color> {suffixProp.stringValue}";
            preview = $"<color={symbolColor}>[</color> {preview} <color={symbolColor}>]</color>";
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview: " + preview, previewStyle);

            // redgin: 添加 MP3 朗读设置，仅在 Text 模式下生效
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("------ MP3 Speech Settings ------", titleStyle);
            SerializedProperty ivAudioClipProp = serializedObject.FindProperty("IV_AudioClipToSpeak");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MP3 Audio", propertyStyle, GUILayout.Width(150));
            EditorGUILayout.PropertyField(ivAudioClipProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            SerializedProperty ivAudioNewPathProp = serializedObject.FindProperty("IV_AudioClipNewPath");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MP3 NewPath", propertyStyle, GUILayout.Width(150));
            EditorGUILayout.PropertyField(ivAudioNewPathProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("// 仅在 Text 模式下生效", messageStyle);
        }
        else if (speakerBase.ReadTarget == TestTTS_ValueReadMode.Custom)
        {
            customValuesList.DoLayoutList();
            EditorGUILayout.LabelField("// 这里配置多个自定义文本字段，将依次组合成朗读内容（不应用前后缀）", messageStyle);

            string symbolColor = "#666666";
            string preview = "";
            for (int i = 0; i < customValuesProp.arraySize; i++)
            {
                SerializedProperty element = customValuesProp.GetArrayElementAtIndex(i);
                SerializedProperty sourceProp = element.FindPropertyRelative("Source");
                string fieldText = "";
                switch (sourceProp.enumValueIndex)
                {
                    case (int)CustomValueSource.Text:
                        {
                            SerializedProperty p = element.FindPropertyRelative("TargetText");
                            fieldText = (p.objectReferenceValue != null) ? ((Text)p.objectReferenceValue).text : "";
                        }
                        break;
                    case (int)CustomValueSource.T_TMP:
                        {
                            SerializedProperty p = element.FindPropertyRelative("TargetTextTMP");
                            fieldText = (p.objectReferenceValue != null) ? ((TMP_Text)p.objectReferenceValue).text : "";
                        }
                        break;
                    case (int)CustomValueSource.Slider:
                        {
                            SerializedProperty p = element.FindPropertyRelative("TargetSlider");
                            fieldText = (p.objectReferenceValue != null) ? ((Slider)p.objectReferenceValue).value.ToString() : "";
                        }
                        break;
                    case (int)CustomValueSource.Default:
                        {
                            SerializedProperty p = element.FindPropertyRelative("DefaultValue");
                            fieldText = p.stringValue;
                        }
                        break;
                }
                if (!string.IsNullOrEmpty(preview))
                    preview += $"<color={symbolColor}>,</color> ";
                preview += fieldText;
            }
            preview = $"<color={symbolColor}>[</color> {preview} <color={symbolColor}>]</color>";
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview: " + preview, previewStyle);
        }
        else if (speakerBase.ReadTarget == TestTTS_ValueReadMode.None)
        {
            EditorGUILayout.LabelField("当前选择的模式为 None，不展示输入内容，仅使用前后缀构造朗读文本。", messageStyle);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));

        // ------ 延迟回调设置 已移除 ------

        serializedObject.ApplyModifiedProperties();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
#endif
