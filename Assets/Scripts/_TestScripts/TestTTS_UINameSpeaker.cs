using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;

public class TestTTS_UINameSpeaker : TestTTS_NameSpeakerBase // 可实现 IDeselectHandler 等接口
{
    private bool hasRead = false;

    #region ------ 初始化 ------
    private void Start()
    {
        CacheTextComponents();
    }
    #endregion

    #region ------ 事件处理 ------
    public override void OnSelect(BaseEventData eventData)
    {
        if (!EnableOnSelected)
            return;

        switch (TestTTS_StaticActionUAP.CurrentReadMode)
        {
            case TestTTS_StaticActionUAP.ReadModeType.MP3:
                HandleMP3Mode();
                break;
            case TestTTS_StaticActionUAP.ReadModeType.UAP:
                HandleUAPMode();
                break;
            case TestTTS_StaticActionUAP.ReadModeType.None:
                return;
        }
    }

    /// <summary>
    /// 原有的 _ReadUIName 方法
    /// </summary>
    public void _ReadUIName()
    {
        switch (TestTTS_StaticActionUAP.CurrentReadMode)
        {
            case TestTTS_StaticActionUAP.ReadModeType.MP3:
                HandleMP3Mode();
                break;
            case TestTTS_StaticActionUAP.ReadModeType.UAP:
                HandleUAPMode();
                break;
            case TestTTS_StaticActionUAP.ReadModeType.None:
                return;
        }
    }

    /// <summary>
    /// 新增重载方法，使用传入的 mp3NewPath 参数加载音频
    /// </summary>
    public void _ReadUIName(string mp3NewPath)
    {
        if (TestTTS_StaticActionUAP.CurrentReadMode == TestTTS_StaticActionUAP.ReadModeType.MP3)
        {
            HandleMP3Mode(mp3NewPath);
        }
        else if (TestTTS_StaticActionUAP.CurrentReadMode == TestTTS_StaticActionUAP.ReadModeType.UAP)
        {
            HandleUAPMode();
        }
    }
    #endregion

    #region ------ MP3模式处理 ------
    private void HandleMP3Mode()
    {
        string baseText = GetBaseText();
        if (string.IsNullOrEmpty(baseText))
            return;

        // 如果已指定 AudioClipToSpeak，则直接播放
        if (AudioClipToSpeak == null)
        {
            AudioClip clip = TestTTS_StaticActionUAP.TryLoadAudioClip(baseText, AudioClipNewPath);
            if (clip != null)
                AudioClipToSpeak = clip;
        }
        if (AudioClipToSpeak != null)
        {
            TestTTS_StaticActionUAP.PlayAudioClip(AudioClipToSpeak);
            StartDelayedCallbackAutomatically();
        }
        else
        {
            ("未能加载 MP3 文件").TTSLog(Color.red);
        }
    }


    // 重载方法，使用自定义 mp3NewPath 参数
    private void HandleMP3Mode(string mp3NewPath)
    {
        string baseText = GetBaseText();
        if (string.IsNullOrEmpty(baseText))
            return;

        AudioClip clip = TestTTS_StaticActionUAP.TryLoadAudioClip(baseText, mp3NewPath);
        if (clip != null)
        {
            AudioClipToSpeak = clip;
            TestTTS_StaticActionUAP.PlayAudioClip(AudioClipToSpeak);
            StartDelayedCallbackAutomatically();
        }
        else
        {
            ("未能加载 MP3 文件").TTSLog(Color.red);
        }
    }
    #endregion

    #region ------ UAP模式处理 ------
    private void HandleUAPMode()
    {
        string baseText = GetBaseText();
        if (string.IsNullOrEmpty(baseText))
            return;

        string finalText = ConstructFinalText(baseText, ReadPrefix, ReadSuffix);
        finalText.UAPSpeak();
        StartDelayedCallbackAutomatically();
    }
    #endregion
}
