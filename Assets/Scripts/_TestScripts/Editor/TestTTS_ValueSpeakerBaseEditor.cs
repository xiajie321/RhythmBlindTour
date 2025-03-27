#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[CustomEditor(typeof(TestTTS_ValueSpeakerBase), true)]
[CanEditMultipleObjects]
public class TestTTS_ValueSpeakerBaseEditor : Editor
{
    // 用于存储各 CustomValue 元素的折叠状态
    private bool[] customValueFoldouts;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 定义样式
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
        // 用于折叠标题的样式（支持 RichText）
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            richText = true,
            fontStyle = FontStyle.Bold
        };
        // 预览显示样式（支持 RichText）
        GUIStyle previewStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            richText = true,
            normal = { textColor = Color.cyan },
            wordWrap = true
        };

        // 定义预览中符号与数值的颜色（使用16进制颜色值）
        string symbolColor = "#888888"; // 较暗的颜色（用于括号、逗号、提示语）
        string valueColor = "#00ffff";  // 较亮的颜色（用于具体数值）

        // 绘制分隔线
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));

        // ------ Read Mode 部分 使用 Toolbar 替换枚举下拉框 ------
        EditorGUILayout.LabelField("------ Read Mode ------", titleStyle);

        #region ------ OnSelected开关设置 ------
        // 获取 EnableOnSelected 属性
        SerializedProperty enableProp = serializedObject.FindProperty("EnableOnSelected");

        // 根据当前状态设置标签文本（亮绿色或暗红色）
        GUIContent toggleContent = new GUIContent(enableProp.boolValue
            ? "<color=lime>Enable OnSelected</color>"
            : "<color=#800000>Enable OnSelected</color>");

        // 创建自定义的 Label 样式，并启用 RichText
        GUIStyle toggleLabelStyle = new GUIStyle(EditorStyles.label) { richText = true };

        // 如果未启用（暗红色状态），设置背景为灰白色
        if (!enableProp.boolValue)
        {
            // 创建一个1x1的纹理，颜色设为灰白色（例如 RGB 0.9,0.9,0.9）
            Texture2D bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0.9f, 0.9f, 0.9f));
            bgTexture.Apply();
            toggleLabelStyle.normal.background = bgTexture;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(toggleContent, toggleLabelStyle, GUILayout.Width(150));
        bool newEnable = EditorGUILayout.Toggle(enableProp.boolValue, GUILayout.Width(20));
        EditorGUILayout.EndHorizontal();
        if (newEnable != enableProp.boolValue)
        {
            enableProp.boolValue = newEnable;
        }
        #endregion

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Read Target", propertyStyle, GUILayout.Width(100));
        // 获取当前枚举值
        SerializedProperty readTargetProp = serializedObject.FindProperty("ReadTarget");
        int current = readTargetProp.enumValueIndex;
        string[] options = new string[] { "Text", "Custom", "None" };
        // 在同一行绘制 Toolbar
        int newIndex = GUILayout.Toolbar(current, options);
        if (newIndex != current)
        {
            readTargetProp.enumValueIndex = newIndex;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("// 点击按钮选择朗读模式", messageStyle);

        EditorGUILayout.Space();

        // 根据当前选择的模式展示不同内容
        TestTTS_ValueSpeakerBase speakerBase = (TestTTS_ValueSpeakerBase)target;
        if (speakerBase.ReadTarget == TestTTS_ValueReadMode.Text)
        {
            EditorGUILayout.LabelField("------ Input Text ------", titleStyle);
            // 展示 Input Text 部分：显示 TargetTMPInputField 与 TargetInputField
            SerializedProperty targetTMPInputProp = serializedObject.FindProperty("TargetTMPInputField");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Test(TMP)", propertyStyle, GUILayout.Width(150));
            EditorGUILayout.PropertyField(targetTMPInputProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            SerializedProperty targetInputProp = serializedObject.FindProperty("TargetInputField");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Text", propertyStyle, GUILayout.Width(150));
            EditorGUILayout.PropertyField(targetInputProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("// 这里配置用于朗读的输入框文本", messageStyle);

            EditorGUILayout.Space();

            // ------ Speech Settings 部分，仅在 Text 模式下显示 ------
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

            // 添加 Text 模式下的预览
            string baseText = "";
            if (speakerBase.TargetTMPInputField != null)
                baseText = speakerBase.TargetTMPInputField.text;
            else if (speakerBase.TargetInputField != null)
                baseText = speakerBase.TargetInputField.text;
            // 构造预览：前缀和后缀使用 symbolColor，文本使用 valueColor
            string preview = "";
            if (!string.IsNullOrEmpty(speakerBase.ReadPrefix))
                preview += $"<color={valueColor}>{speakerBase.ReadPrefix}</color><color={symbolColor}>, </color>";
            preview += $"<color={valueColor}>{baseText}</color>";
            if (!string.IsNullOrEmpty(speakerBase.ReadSuffix))
                preview += $"<color={symbolColor}>, </color><color={valueColor}>{speakerBase.ReadSuffix}</color>";
            preview = $"<color={symbolColor}>[</color>" + preview + $"<color={symbolColor}>]</color>";
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview: " + preview, previewStyle);
        }
        else if (speakerBase.ReadTarget == TestTTS_ValueReadMode.Custom)
        {
            EditorGUILayout.LabelField("------ Custom Values ------", titleStyle);
            // 显示整个数组的折叠头
            SerializedProperty valueArrayProp = serializedObject.FindProperty("ValueArray");
            valueArrayProp.isExpanded = EditorGUILayout.Foldout(valueArrayProp.isExpanded, "Custom Values", true);
            if (valueArrayProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                // 添加按钮，点击后新元素默认折叠状态为展开
                if (GUILayout.Button("Add Custom Value"))
                {
                    int newSize = valueArrayProp.arraySize + 1;
                    valueArrayProp.arraySize = newSize;
                    // 重新初始化折叠状态数组，全部设为 true
                    customValueFoldouts = new bool[newSize];
                    for (int j = 0; j < newSize; j++)
                    {
                        customValueFoldouts[j] = true;
                    }
                }

                // 初始化折叠状态数组，如果长度不匹配，则全部设为 true
                if (customValueFoldouts == null || customValueFoldouts.Length != valueArrayProp.arraySize)
                {
                    customValueFoldouts = new bool[valueArrayProp.arraySize];
                    for (int j = 0; j < customValueFoldouts.Length; j++)
                    {
                        customValueFoldouts[j] = true;
                    }
                }
                for (int i = 0; i < valueArrayProp.arraySize; i++)
                {
                    SerializedProperty element = valueArrayProp.GetArrayElementAtIndex(i);
                    SerializedProperty targetTMPInput = element.FindPropertyRelative("TargetTMPInput");
                    SerializedProperty targetInput = element.FindPropertyRelative("TargetInput");
                    SerializedProperty defaultValue = element.FindPropertyRelative("DefaultValue");
                    SerializedProperty groupNameProp = element.FindPropertyRelative("GroupName");

                    string groupName = "";
                    bool nonEmpty = false;
                    // 优先判断：TargetInput > TargetTMPInput > DefaultValue
                    if (targetInput.objectReferenceValue != null)
                    {
                        groupName = $"Part {i}: UI Text";
                        Text uiText = targetInput.objectReferenceValue as Text;
                        nonEmpty = (uiText != null && !string.IsNullOrEmpty(uiText.text));
                    }
                    else if (targetTMPInput.objectReferenceValue != null)
                    {
                        groupName = $"Part {i}: UI Text (TMP)";
                        TMP_Text tmpText = targetTMPInput.objectReferenceValue as TMP_Text;
                        nonEmpty = (tmpText != null && !string.IsNullOrEmpty(tmpText.text));
                    }
                    else if (!string.IsNullOrEmpty(defaultValue.stringValue))
                    {
                        groupName = $"Part {i}: Default Value";
                        nonEmpty = true;
                    }
                    else
                    {
                        groupName = $"Part {i}: None";
                    }
                    // 更新内部 GroupName
                    groupNameProp.stringValue = groupName;
                    // 如果非空，则使用绿色 RichText
                    string headerLabel = nonEmpty ? $"<color=green>{groupName}</color>" : groupName;
                    customValueFoldouts[i] = EditorGUILayout.Foldout(customValueFoldouts[i], headerLabel, true, foldoutStyle);
                    if (customValueFoldouts[i])
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(targetTMPInput);
                        EditorGUILayout.PropertyField(targetInput);
                        EditorGUILayout.PropertyField(defaultValue);
                        if (GUILayout.Button("Remove"))
                        {
                            valueArrayProp.DeleteArrayElementAtIndex(i);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.LabelField("// 这里配置多个自定义文本字段，将依次组合成朗读内容（不应用前后缀）", messageStyle);

            // 添加自定义预览
            string preview = "";
            for (int i = 0; i < valueArrayProp.arraySize; i++)
            {
                SerializedProperty element = valueArrayProp.GetArrayElementAtIndex(i);
                SerializedProperty targetTMPInput = element.FindPropertyRelative("TargetTMPInput");
                SerializedProperty targetInput = element.FindPropertyRelative("TargetInput");
                SerializedProperty defaultValue = element.FindPropertyRelative("DefaultValue");
                string val = "";
                if (targetInput.objectReferenceValue != null)
                {
                    Text uiText = targetInput.objectReferenceValue as Text;
                    val = (uiText != null && string.IsNullOrEmpty(uiText.text)) ? "/text value/" : (uiText != null ? uiText.text : "");
                }
                else if (targetTMPInput.objectReferenceValue != null)
                {
                    TMP_Text tmpText = targetTMPInput.objectReferenceValue as TMP_Text;
                    val = (tmpText != null && string.IsNullOrEmpty(tmpText.text)) ? "/text value/" : (tmpText != null ? tmpText.text : "");
                }
                else
                {
                    val = defaultValue.stringValue;
                }
                // 每个值用 valueColor 包裹，而逗号用 symbolColor 包裹
                if (!string.IsNullOrEmpty(preview))
                    preview += $"<color={symbolColor}>, </color>";
                preview += $"<color={valueColor}>{val}</color>";
            }
            string previewText = $"<color={symbolColor}>[</color>" + preview + $"<color={symbolColor}>]</color>";
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview: " + previewText, previewStyle);
        }
        else if (speakerBase.ReadTarget == TestTTS_ValueReadMode.None)
        {
            EditorGUILayout.LabelField("当前选择的模式为 None，不展示输入内容部分，仅使用前后缀构造朗读文本。", messageStyle);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));

        serializedObject.ApplyModifiedProperties();
    }

    string _theLine = "————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————";
}
#endif
