using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TestTTS_StaticActionUAP
{
    /// <summary>
    /// 示例: "speak".UAPSpeak();
    /// </summary>
    public static void UAPSpeak(this string says)
    {
        UAP_AccessibilityManager.StopSpeaking();
        UAP_AccessibilityManager.Say("");
        UAP_AccessibilityManager.Say(says);
    }

    /// <summary>
    /// 检查 UAP 是否正在朗读
    /// </summary>
    /// <returns>如果正在朗读，则返回 true，否则返回 false</returns>
    public static bool isUAPSpeaking()
    {
        return UAP_AccessibilityManager.IsSpeaking(); // 确保 UAP_AccessibilityManager 有这个方法
    }

    #region DebugLog打印
    public static void TTSLog(this string text, Color color = default(Color))
    {
#if UNITY_EDITOR
        if (color == default(Color))
        {
            color = Color.white;
        }
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        Debug.Log($"<color=#{colorHex}>{text}</color>");
#endif
    #endregion
    }
}
