using System;
using UnityEngine;
using TMPro;
using System.Globalization;

public static class mTTS
{
    public enum InterruptMode { Interrupt, Queue, DropIfBusy }
    public enum WriteMode { Replace, AppendLine, AppendSpace }

    [Serializable]
    public class Options
    {
        public InterruptMode interruptMode = InterruptMode.Interrupt;
        public WriteMode writeMode = WriteMode.Replace;

        [Header("（可选）模拟朗读时长（如仍使用旧的 mTTSManager 队列）")]
        public bool simulateDuration = false;
        [Tooltip("每秒多少字符，模拟朗读用")] public float charactersPerSecond = 16f;
        public float minSeconds = 0.2f;
        public float maxSeconds = 6f;

        public Options Clone() => (Options)MemberwiseClone();
    }

    public static bool IsSpeaking
    {
        get
        {
            try { return UAP_AccessibilityManager.IsSpeaking(); }
            catch { return false; }
        }
    }

    public static void SetTargetTMP(TMP_Text target)
    {
        if (mTTSManager.Instance == null)
        {
            Debug.LogWarning("[mTTS] 场景中没有 mTTSManager，无法设置目标 TMP。");
            return;
        }
        mTTSManager.Instance.TargetTMP = target;
    }

    // 一次朗读：先全量打断并清空队列，然后立即朗读“当前这条”
    public static bool Speak(string text, Options options = null)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // 强制打断上一条（见 Stop 实现）
        Stop(clearQueue: true, clearTMP: false);

        options ??= new Options
        {
            interruptMode = InterruptMode.Interrupt,
            writeMode = WriteMode.Replace,
            simulateDuration = false
        };

        // 同步写入目标TMP（保持原行为）
        TMP_Text targetTMP = (mTTSManager.Instance != null) ? mTTSManager.Instance.TargetTMP : null;
        if (targetTMP != null)
            WriteToTMP(targetTMP, text, options.writeMode);

        // 直接用 UAP 出声
        try
        {
            UAP_AccessibilityManager.Say(text);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[mTTS] UAP Say 失败：{e.Message}");
            return false;
        }
    }

    public static bool Enqueue(string text, Options options = null)
    {
        // 为了统一“随时可打断”的体验，这里也直接走 Speak
        return Speak(text, options);
    }

    /// <summary>立刻停止当前朗读；可选清空队列/清空 TMP</summary>
    public static void Stop(bool clearQueue = false, bool clearTMP = false)
    {
        // 关键改动：不再依赖 IsSpeaking() 的返回，始终尝试 StopSpeaking()
        try
        {
            UAP_AccessibilityManager.StopSpeaking(); // 无条件硬切断，避免某些平台 IsSpeaking=false 却仍在播
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[mTTS] UAP StopSpeaking 异常：{e.Message}");
        }

        // 兼容旧的 mTTSManager（内部队列/计时）
        if (mTTSManager.Instance != null)
            mTTSManager.Instance.StopSpeaking(clearQueue, clearTMP);

        // 仅清 TMP 的兜底
        if (clearTMP)
        {
            var tmp = (mTTSManager.Instance != null) ? mTTSManager.Instance.TargetTMP : null;
            if (tmp != null) tmp.text = string.Empty;
        }
    }

    public static void ClearQueue()
    {
        if (mTTSManager.Instance == null) return;
        mTTSManager.Instance.ClearQueue();
    }

    // ====== 数字与时间格式化 ======
    public enum TimeReadStyle { NumericTokens, ChineseText }

    public static string FormatSecondsForTTS(float seconds, TimeReadStyle style = TimeReadStyle.NumericTokens)
    {
        seconds = (float)Math.Round(seconds, 2, MidpointRounding.AwayFromZero);
        if (Mathf.Abs(seconds) < 0.005f) seconds = 0f;

        string s = seconds.ToString("0.00", CultureInfo.InvariantCulture);
        bool neg = s[0] == '-';
        if (neg) s = s.Substring(1);

        var parts = s.Split('.');
        string intPart = parts[0];
        string decPart = (parts.Length > 1 ? parts[1] : "00");

        string sign = neg ? "负" : "";

        if (style == TimeReadStyle.ChineseText)
        {
            string intCN = ToChineseInt(int.Parse(intPart));
            return $"{sign}{intCN}点{DigitCN(decPart[0])}{DigitCN(decPart[1])}";
        }
        else
        {
            return s;
        }
    }

    public static bool SpeakSeconds(float seconds, bool appendUnit = true,
                                    TimeReadStyle style = TimeReadStyle.NumericTokens,
                                    Options options = null)
    {
        string core = FormatSecondsForTTS(seconds, style);
        return Speak(appendUnit ? $"{core}秒" : core, options);
    }

    private static readonly string[] DIG_CN = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
    private static string DigitCN(char ch) => DIG_CN[ch - '0'];

    private static string ToChineseInt(int n)
    {
        if (n == 0) return "零";
        var units = new[] { "", "十", "百", "千" };
        var digits = new System.Collections.Generic.List<int>();
        while (n > 0) { digits.Add(n % 10); n /= 10; } // 低→高

        var sb = new System.Text.StringBuilder();
        bool pendingZero = false;
        for (int i = digits.Count - 1; i >= 0; i--)
        {
            int d = digits[i];
            if (d == 0)
            {
                if (sb.Length > 0) pendingZero = true;
                continue;
            }
            if (pendingZero) { sb.Append("零"); pendingZero = false; }
            if (!(i == 1 && d == 1 && digits.Count == 2)) sb.Append(DIG_CN[d]);
            sb.Append(units[i]);
        }
        return sb.ToString();
    }

    public static string FormatIntegerChinese(int n)
    {
        if (n == 0) return "零";
        if (n < 0) return "负" + ToChineseInt(Math.Abs(n));
        return ToChineseInt(n);
    }

    private static void WriteToTMP(TMP_Text tmp, string text, WriteMode mode)
    {
        if (tmp == null) return;
        switch (mode)
        {
            case WriteMode.Replace:
                tmp.text = text;
                break;
            case WriteMode.AppendLine:
                if (string.IsNullOrEmpty(tmp.text)) tmp.text = text;
                else tmp.text += "\n" + text;
                break;
            case WriteMode.AppendSpace:
                if (string.IsNullOrEmpty(tmp.text)) tmp.text = text;
                else tmp.text += " " + text;
                break;
        }
    }
}
