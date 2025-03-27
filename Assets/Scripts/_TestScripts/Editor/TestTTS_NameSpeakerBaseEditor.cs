#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[CustomEditor(typeof(TestTTS_NameSpeakerBase), true)]
[CanEditMultipleObjects]
public class TestTTS_NameSpeakerBaseEditor : Editor
{
    private bool _showMP3NewPath = false;
    string _theLine = "————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————";

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
        GUIStyle previewStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            richText = true,
            normal = { textColor = Color.cyan },
            wordWrap = true
        };

        // 用于折叠标题的样式（支持 RichText）
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            richText = true,
            fontStyle = FontStyle.Bold
        };

        // 定义预览中符号与数值的颜色
        string symbolColor = "#888888"; // 较暗的颜色
        string valueColor = "#00ffff";  // 较亮的颜色

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

        // 继续显示 Read Target 的 Toolbar
        SerializedProperty readTargetProp = serializedObject.FindProperty("ReadTarget");
        int currentIndex = readTargetProp.enumValueIndex;
        string[] options = new string[] { "UI", "Default", "Auto" };
        int newIndex = GUILayout.Toolbar(currentIndex, options);
        if (newIndex != currentIndex)
        {
            readTargetProp.enumValueIndex = newIndex;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("// 点击按钮选择朗读模式", messageStyle);

        EditorGUILayout.Space();

        TestTTS_NameSpeakerBase speakerBase = (TestTTS_NameSpeakerBase)target;
        TestTTS_ReadMode mode = (TestTTS_ReadMode)readTargetProp.enumValueIndex;
        if (mode == TestTTS_ReadMode.Default)
        {
            EditorGUILayout.LabelField("------ Default Text ------", titleStyle);
            SerializedProperty defaultTextProp = serializedObject.FindProperty("DefaultText");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Default Text", propertyStyle, GUILayout.Width(80));
            EditorGUILayout.PropertyField(defaultTextProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("// 朗读自定义字符串，通过UAP朗读", messageStyle);
        }
        else if (mode == TestTTS_ReadMode.UI)
        {
            EditorGUILayout.LabelField("------ UI Text ------", titleStyle);
            SerializedProperty targetTMPProp = serializedObject.FindProperty("TargetTMPText");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target TMP Text", propertyStyle, GUILayout.Width(100));
            EditorGUILayout.PropertyField(targetTMPProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            SerializedProperty targetUITextProp = serializedObject.FindProperty("TargetUIText");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target UI Text", propertyStyle, GUILayout.Width(100));
            EditorGUILayout.PropertyField(targetUITextProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("// 朗读指定的UI Text，通过UAP朗读", messageStyle);
        }
        else if (mode == TestTTS_ReadMode.Auto)
        {
            EditorGUILayout.LabelField("------ Auto Search ------", titleStyle);
            EditorGUILayout.LabelField("// 自动搜索朗读Text，通过UAP朗读", messageStyle);
            EditorGUILayout.LabelField("// 从当前UI的子级或者同级中查找。", messageStyle);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));

        // ------ Speech Settings 部分 ------
        SerializedProperty prefixProp = serializedObject.FindProperty("ReadPrefix");
        EditorGUILayout.LabelField("ReadPrefix", propertyStyle);
        EditorGUILayout.PropertyField(prefixProp, GUIContent.none);
        SerializedProperty suffixProp = serializedObject.FindProperty("ReadSuffix");
        EditorGUILayout.LabelField("ReadSuffix", propertyStyle);
        EditorGUILayout.PropertyField(suffixProp, GUIContent.none);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("// 文本的前缀和后缀，“前缀”+“UIText/stringText”+“后缀”。通过UAP朗读", messageStyle);

        // ------ 朗读预览部分 ------
        string mainText = "";
        if (mode == TestTTS_ReadMode.Default)
            mainText = speakerBase.DefaultText;
        else if (mode == TestTTS_ReadMode.UI)
        {
            if (speakerBase.TargetTMPText != null && !string.IsNullOrEmpty(speakerBase.TargetTMPText.text))
                mainText = speakerBase.TargetTMPText.text;
            else if (speakerBase.TargetUIText != null && !string.IsNullOrEmpty(speakerBase.TargetUIText.text))
                mainText = speakerBase.TargetUIText.text;
        }
        else if (mode == TestTTS_ReadMode.Auto)
            mainText = speakerBase.GetBaseText();

        string previewText = "";
        if (!string.IsNullOrEmpty(prefixProp.stringValue))
            previewText += $"<color={valueColor}>{prefixProp.stringValue}</color><color={symbolColor}>, </color>";
        previewText += $"<color={valueColor}>{mainText}</color>";
        if (!string.IsNullOrEmpty(suffixProp.stringValue))
            previewText += $"<color={symbolColor}>, </color><color={valueColor}>{suffixProp.stringValue}</color>";
        previewText = $"<color={symbolColor}>[</color>" + previewText + $"<color={symbolColor}>]</color>";
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview: " + previewText, previewStyle);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));

        // ------ MP3 朗读部分 ------
        EditorGUILayout.LabelField("****** ReadMode : MP3", titleStyle);
        EditorGUILayout.LabelField("MP3 Audio", propertyStyle);
        SerializedProperty audioClipProp = serializedObject.FindProperty("AudioClipToSpeak");
        EditorGUILayout.PropertyField(audioClipProp, GUIContent.none);

        SerializedProperty audioNewPath = serializedObject.FindProperty("AudioClipNewPath");
        if (!string.IsNullOrEmpty(audioNewPath.stringValue))
            foldoutStyle.normal.textColor = Color.green;
        else
            foldoutStyle.normal.textColor = Color.white;
        _showMP3NewPath = EditorGUILayout.Foldout(_showMP3NewPath, "MP3 NewPath", true, foldoutStyle);
        if (_showMP3NewPath)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("NewPath", propertyStyle, GUILayout.Width(60));
            EditorGUILayout.PropertyField(audioNewPath, GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));
        EditorGUILayout.LabelField(_theLine, separatorStyle, GUILayout.Height(2));

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
