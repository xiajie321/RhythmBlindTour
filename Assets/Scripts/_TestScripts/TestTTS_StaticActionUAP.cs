using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public static bool isUAPSpeaking()
    {
        return UAP_AccessibilityManager.IsSpeaking();
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
    }
    #endregion

    #region SafeText方法
    public static string SafeText(this TMP_Text uiText)
    {
        return (uiText != null && !string.IsNullOrEmpty(uiText.text)) ? uiText.text : "";
    }

    public static string SafeText(this Text uiText)
    {
        return (uiText != null && !string.IsNullOrEmpty(uiText.text)) ? uiText.text : "";
    }
    #endregion

    /// <summary>
    /// 根据前缀、基本文本和后缀构造最终朗读文本，
    /// 若基本文本为空则使用 "未设置" 替代，
    /// 各部分之间用逗号分隔
    /// </summary>
    public static string ConstructFinalText(string baseText, string prefix = "", string suffix = "")
    {
        if (string.IsNullOrEmpty(baseText))
        {
            baseText = "";
        }
        string result = "";
        if (!string.IsNullOrEmpty(prefix))
        {
            result += prefix + ", ";
        }
        result += baseText;
        if (!string.IsNullOrEmpty(suffix))
        {
            result += ", " + suffix;
        }
        return result;
    }

    #region ------ Read Mode 设置 ------
    public enum ReadModeType
    {
        MP3,
        UAP,
        None
    }

    // 当前朗读模式，默认 MP3
    public static ReadModeType CurrentReadMode = ReadModeType.MP3;

    /// <summary>
    /// 切换为 MP3 模式，保存数据到 PlayerPrefs 中
    /// </summary>
    public static void SetReadModeToMP3()
    {
        CurrentReadMode = ReadModeType.MP3;
        PlayerPrefs.SetString("ReadMode", "ReadMode_MP3");
    }

    /// <summary>
    /// 切换为 UAP 模式，保存数据到 PlayerPrefs 中
    /// </summary>
    public static void SetReadModeToUAP()
    {
        CurrentReadMode = ReadModeType.UAP;
        PlayerPrefs.SetString("ReadMode", "ReadMode_UAP");
    }

    /// <summary>
    /// 切换为 None 模式，保存数据到 PlayerPrefs 中
    /// </summary>
    public static void SetReadModeToNone()
    {
        CurrentReadMode = ReadModeType.None;
        PlayerPrefs.SetString("ReadMode", "ReadMode_None");
    }
    #endregion

    #region ------ AudioClip播放方法 ------
    /// <summary>
    /// 播放指定 AudioClip，使用名为 "TTSAudioSpeaker" 的 GameObject 上的 AudioSource。
    /// 如果该 AudioSource 正在播放，则直接停止并切换播放新的 AudioClip；
    /// 播放完成后禁用该 AudioSource，但保留 GameObject 以便下次复用。
    /// </summary>
    /// <param name="clip">要播放的 AudioClip</param>
    public static void PlayAudioClip(AudioClip clip)
    {
        if (clip == null)
            return;

        // 查找或创建 AudioSource GameObject
        GameObject go = GameObject.Find("TTSAudioSpeaker");
        if (go == null)
        {
            go = new GameObject("TTSAudioSpeaker");
        }

        AudioSource audioSource = go.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = go.AddComponent<AudioSource>();
        }
        // 如果已有音频在播放，则直接停止
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.clip = clip;
        audioSource.Play();
    }
    #endregion

    #region ------ AudioClip查找方法 ------
    /*
        根据传入的基础文本从 Resources/Audios/_TTSAudios/ 下加载对应的 MP3 文件。
        如果 audioClipNewPath 不为空，则将其插入路径中。
        例如，真实地址为 Assets/Resources/Audios/_TTSAudios/Part1/编辑模式.mp3，
        则调用时传入 baseText = "编辑模式"，audioClipNewPath = "/Part1"。
        Resources.Load 使用路径 "Audios/_TTSAudios" + audioClipNewPath + "/" + baseText  (不含扩展名)。
    */
    public static AudioClip TryLoadAudioClip(string baseText, string audioClipNewPath = "")
    {
        // 基础路径
        string path = "Audios/_TTSAudios";
        if (!string.IsNullOrEmpty(audioClipNewPath))
        {
            path += audioClipNewPath;
        }
        if (!string.IsNullOrEmpty(baseText))
        {
            path += "/" + baseText;
        }
       
        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            ("未能加载音频文件：" + path).TTSLog(Color.red);
        }
        return clip;
    }
    #endregion

    #region Tip Color
    public static Color titleColor = Color.yellow;
    public static Color separatorColor = Color.green;
    #endregion
}
