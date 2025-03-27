#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


/// <summary>
/// 简单设置关联脚本的图标
/// Speaker 朗读-名字-
/// Reader  朗读-数据-
/// Taker   复制了原项目中的功能来测试
/// </summary>
[InitializeOnLoad]
public static class TestTTS_EditorTools
{
    // 加载图标资源
    private static Texture2D speakerIcon;
    private static Texture2D readerIcon;
    private static Texture2D takerIcon;  // Taker 脚本的图标

    // 脚本名称列表（用于根据挂载的脚本类型设置图标）
    private static readonly string[] speakerScriptNames = new string[]
    {
        "TestTTS_UINameSpeaker",
        "TestTTS_InputFieldRealTimeSpeaker",
        "TestTTS_TMPTextSpeaker"
    };

    private static readonly string[] readerScriptNames = new string[]
    {
        "TestTTS_AudioBarReader",
        "TestTTS_AudioSliderReader",
        "TestTTS_AudioInputReader",
        "TestTTS_InputValueSpeaker",


        "TestTTS_ToolBarReader",
        "TestTTS_DropdownItemReader",
    };

    // Taker 脚本名称列表
    private static readonly string[] takerScriptNames = new string[]
    {
        "TestTTS_UFATaker"
    };

    static TestTTS_EditorTools()
    {
        #region Project/Inspector视图图标设置（加载图标资源）
        string iconPath_Speaker = "Assets/Scripts/_TestScripts/Editor/小熊贴纸.png";
        string iconPath_Reader = "Assets/Scripts/_TestScripts/Editor/耳机贴纸.png";
        string iconPath_Taker = "Assets/Scripts/_TestScripts/Editor/手机贴纸.png";

        speakerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath_Speaker);
        readerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath_Reader);
        takerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath_Taker);

        if (speakerIcon == null)
        {
            Debug.LogError($"无法加载图标，请确认路径是否正确：{iconPath_Speaker}");
        }
        if (readerIcon == null)
        {
            Debug.LogError($"无法加载图标，请确认路径是否正确：{iconPath_Reader}");
        }
        if (takerIcon == null)
        {
            Debug.LogError($"无法加载图标，请确认路径是否正确：{iconPath_Taker}");
        }
        #endregion

        #region Hierarchy视图图标设置
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        #endregion

        #region Project/Inspector视图图标注册（设置 MonoScript 图标）
        EditorApplication.delayCall += SetComponentInspectorIcon;
        #endregion
    }

    /// <summary>
    /// Hierarchy视图中绘制图标的部分
    /// （用于在 Hierarchy 视图中显示自定义图标）
    /// </summary>
    private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null)
            return;

        // 检查每个类别的组件是否存在，允许同时存在多个
        System.Collections.Generic.List<Texture2D> iconsToDisplay = new System.Collections.Generic.List<Texture2D>();

        bool hasTaker = false;
        foreach (string scriptName in takerScriptNames)
        {
            if (obj.GetComponent(scriptName) != null)
            {
                hasTaker = true;
                break;
            }
        }
        if (hasTaker)
            iconsToDisplay.Add(takerIcon);

        bool hasReader = false;
        foreach (string scriptName in readerScriptNames)
        {
            if (obj.GetComponent(scriptName) != null)
            {
                hasReader = true;
                break;
            }
        }
        if (hasReader)
            iconsToDisplay.Add(readerIcon);

        bool hasSpeaker = false;
        foreach (string scriptName in speakerScriptNames)
        {
            if (obj.GetComponent(scriptName) != null)
            {
                hasSpeaker = true;
                break;
            }
        }
        if (hasSpeaker)
            iconsToDisplay.Add(speakerIcon);

        // 绘制所有检测到的图标，依次横向偏移（这里每个图标宽度为 16 像素）
        if (iconsToDisplay.Count > 0)
        {
            for (int i = 0; i < iconsToDisplay.Count; i++)
            {
                Rect iconRect = new Rect(selectionRect.x - 30 - (16 * i), selectionRect.y, 16, 16);
                GUI.Label(iconRect, iconsToDisplay[i]);
            }
        }
    }

    /// <summary>
    /// 设置 Project/Inspector视图中显示的脚本图标（修改 MonoScript 图标）
    /// （这部分代码用于在 Project 视图中显示自定义图标，同时在 Inspector 中显示对应的脚本图标）
    /// </summary>
    private static void SetComponentInspectorIcon()
    {
        // 为 Speaker 脚本设置图标
        foreach (string scriptName in speakerScriptNames)
        {
            string[] guids = AssetDatabase.FindAssets(scriptName + " t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.name == scriptName)
                {
                    EditorGUIUtility.SetIconForObject(ms, speakerIcon);
                    break;
                }
            }
        }

        // 为 Reader 脚本设置图标
        foreach (string scriptName in readerScriptNames)
        {
            string[] guids = AssetDatabase.FindAssets(scriptName + " t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.name == scriptName)
                {
                    EditorGUIUtility.SetIconForObject(ms, readerIcon);
                    break;
                }
            }
        }

        // 为 Taker 脚本设置图标（即为 Taker 脚本设置图标）
        foreach (string scriptName in takerScriptNames)
        {
            string[] guids = AssetDatabase.FindAssets(scriptName + " t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.name == scriptName)
                {
                    EditorGUIUtility.SetIconForObject(ms, takerIcon);
                    break;
                }
            }
        }
    }
}
#endif
