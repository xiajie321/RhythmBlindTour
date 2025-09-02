using System;
using UnityEngine;

public static class RB_TTS
{
    /// <summary>是否正在朗读（底层来自 UAP）</summary>
    public static bool RB_IsSpeaking
    {
        get
        {
            try { return UAP_AccessibilityManager.IsSpeaking(); } // UAP
            catch { return false; }
        }
    }

    /// <summary>
    /// 朗读文本（默认先打断再播）。
    /// 仅做底层桥接；不做队列、不做 UI 可访问控制。
    /// </summary>
    public static bool RB_Say(this string text, bool interruptBeforeSpeak = true)
    {
        if (string.IsNullOrEmpty(text)) return false;

        try
        {
            if (interruptBeforeSpeak)
                UAP_AccessibilityManager.StopSpeaking();      // UAP
            UAP_AccessibilityManager.Say(text);               // UAP
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RB_TTS] RB_Say 调用失败：{e.Message}");
            return false;
        }
    }

    /// <summary>立刻停止当前朗读</summary>
    public static void RB_Stop()
    {
        try { UAP_AccessibilityManager.StopSpeaking(); }      // UAP
        catch (Exception e) { Debug.LogWarning($"[RB_TTS] RB_Stop 异常：{e.Message}"); }
    }

    /// <summary>
    /// “唤醒”无障碍总开关（仅 Win / macOS/Editor 下调用）。
    /// enable=true 启用；false 关闭。只桥接 UAP 的 EnableAccessibility。
    /// </summary>
    public static void RB_WakeAccessibility(bool enable)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        try { UAP_AccessibilityManager.EnableAccessibility(enable); } // UAP
        catch (Exception e) { Debug.LogWarning($"[RB_TTS] RB_WakeAccessibility 异常：{e.Message}"); }
#else
        // 其他平台不做处理（按需再扩展）
#endif
    }
}
