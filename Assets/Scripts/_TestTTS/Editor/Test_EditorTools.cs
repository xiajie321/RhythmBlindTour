#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 简单设置关联脚本的图标
/// Speaker 朗读-名字-
/// Reader 朗读-数据-
/// Taker 复制了原项目中的功能来测试
/// 新增：Revolver —— 显示“选择组件.png”图标
/// 
/// 修改说明：
/// 1. 将图标尺寸改为较小（例如：12x12像素）
/// 2. 提供菜单项切换开关，允许用户在 Unity 菜单中控制是否显示自定义图标
/// </summary>
[InitializeOnLoad]
public static class Test_EditorTools
{
    // 图标资源
    private static Texture2D speakerIcon;
    private static Texture2D readerIcon;
    private static Texture2D takerIcon;
    private static Texture2D revolverIcon; // 新增：Revolver 图标

    // 脚本名称列表，用于设置图标
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

    private static readonly string[] takerScriptNames = new string[]
    {
        "TestTTS_UFATaker"
    };

    // 新增：Revolver 脚本名称列表
    private static readonly string[] revolverScriptNames = new string[]
    {
        "TestSelect_Revolver"
    };

    // 自定义设置：图标尺寸和是否显示图标
    private const int iconSize = 12;  // 使用 12x12 像素的较小尺寸
    private static bool showCustomIcons;

    static Test_EditorTools()
    {
        // 从 EditorPrefs 中读取开关状态，默认开启
        showCustomIcons = EditorPrefs.GetBool("TestEditorTools_ShowCustomIcons", true);

        // 图标资源路径（请确保路径正确）
        string iconPath_Speaker = "Assets/Scripts/_TestTTS/Editor/小熊贴纸.png";
        string iconPath_Reader = "Assets/Scripts/_TestTTS/Editor/耳机贴纸.png";
        string iconPath_Taker = "Assets/Scripts/_TestTTS/Editor/手机贴纸.png";
        string iconPath_Revolver = "Assets/Scripts/_TestTTS/Editor/选择组件.png";

        speakerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath_Speaker);
        readerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath_Reader);
        takerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath_Taker);
        revolverIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath_Revolver);

        if (speakerIcon == null)
            Debug.LogError($"无法加载图标，请确认路径是否正确：{iconPath_Speaker}");
        if (readerIcon == null)
            Debug.LogError($"无法加载图标，请确认路径是否正确：{iconPath_Reader}");
        if (takerIcon == null)
            Debug.LogError($"无法加载图标，请确认路径是否正确：{iconPath_Taker}");
        if (revolverIcon == null)
            Debug.LogError($"无法加载图标，请确认路径是否正确：{iconPath_Revolver}");

        // 添加 Hierarchy 绘制回调
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        // 延迟调用，为 Project/Inspector 中的脚本设置图标
        EditorApplication.delayCall += SetComponentInspectorIcon;
    }

    /// <summary>
    /// 在 Hierarchy 视图中绘制图标（允许同时显示多个），并依据开关状态决定是否绘制
    /// </summary>
    private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        // 如果关闭了自定义图标，则直接返回
        if (!showCustomIcons)
            return;

        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null)
            return;

        var iconsToDisplay = new System.Collections.Generic.List<Texture2D>();

        // 检查 Taker 脚本
        foreach (string scriptName in takerScriptNames)
        {
            if (obj.GetComponent(scriptName) != null)
            {
                iconsToDisplay.Add(takerIcon);
                break;
            }
        }
        // 检查 Reader 脚本
        foreach (string scriptName in readerScriptNames)
        {
            if (obj.GetComponent(scriptName) != null)
            {
                iconsToDisplay.Add(readerIcon);
                break;
            }
        }
        // 检查 Speaker 脚本
        foreach (string scriptName in speakerScriptNames)
        {
            if (obj.GetComponent(scriptName) != null)
            {
                iconsToDisplay.Add(speakerIcon);
                break;
            }
        }
        // 检查 Revolver 脚本
        foreach (string scriptName in revolverScriptNames)
        {
            if (obj.GetComponent(scriptName) != null)
            {
                iconsToDisplay.Add(revolverIcon);
                break;
            }
        }

        // 绘制所有图标，横向排列，每个图标尺寸使用 iconSize (12×12)
        for (int i = 0; i < iconsToDisplay.Count; i++)
        {
            // 根据需要调整图标位置：此处采用固定偏移量（可根据实际需要进一步调整）
            Rect iconRect = new Rect(selectionRect.x - 30 - (iconSize * i), selectionRect.y, iconSize, iconSize);
            GUI.Label(iconRect, iconsToDisplay[i]);
        }
    }

    /// <summary>
    /// 设置 Project/Inspector 视图中显示的脚本图标（修改 MonoScript 图标）
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
        // 为 Taker 脚本设置图标
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
        // 为 Revolver 脚本设置图标
        foreach (string scriptName in revolverScriptNames)
        {
            string[] guids = AssetDatabase.FindAssets(scriptName + " t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.name == scriptName)
                {
                    EditorGUIUtility.SetIconForObject(ms, revolverIcon);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 增加菜单项来切换自定义图标的显示状态。
    /// 你可以通过 Unity 顶部菜单：Tools -> Test Editor Tools -> Toggle Custom Icons 来启用或关闭这些图标。
    /// </summary>
    [MenuItem("Tools/Test Editor Tools/Toggle Custom Icons")]
    private static void ToggleCustomIcons()
    {
        showCustomIcons = !showCustomIcons;
        EditorPrefs.SetBool("TestEditorTools_ShowCustomIcons", showCustomIcons);
        EditorApplication.RepaintHierarchyWindow();
        Debug.Log("Custom Icons: " + (showCustomIcons ? "Enabled" : "Disabled"));
    }
}
#endif
