using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;
using System.Collections;

public class TestTTS_InputValueSpeaker : TestTTS_ValueSpeakerBase
{
    private void Start()
    {
        CacheInputFields();
    }

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

    public void _ReadInputValue()
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
    /// MP3 模式下的处理：
    /// 当 ReadTarget 为 Text 时，使用 Text 模式的 MP3 朗读逻辑；
    /// 当 ReadTarget 为 Custom 时，启动协程依次播放 Custom 模式下各个 MP3 文件（等待播放完成）。
    /// </summary>
    private void HandleMP3Mode()
    {
        if (ReadTarget == TestTTS_ValueReadMode.Text)
        {
            string baseText = GetBaseText();
            if (string.IsNullOrEmpty(baseText))
                return;

            // 如果已在 Inspector 中指定了 MP3 AudioClip，则直接播放
            if (IV_AudioClipToSpeak == null)
            {
                AudioClip clip = TestTTS_StaticActionUAP.TryLoadAudioClip(baseText, IV_AudioClipNewPath);
                if (clip != null)
                    IV_AudioClipToSpeak = clip;
            }
            if (IV_AudioClipToSpeak != null)
            {
                TestTTS_StaticActionUAP.PlayAudioClip(IV_AudioClipToSpeak);
            }
            else
            {
                ("未能加载 MP3 文件").TTSLog(Color.red);
            }
        }
        else if (ReadTarget == TestTTS_ValueReadMode.Custom)
        {
            // redgin: 使用等待播放方案播放 Custom 模式下 MP3 朗读
            StartCoroutine(PlayCustomMP3Sequence());
        }
        else
        {
            ("当前输入模式不支持 MP3 朗读").TTSLog(Color.red);
        }
    }

    /// <summary>
    /// UAP 模式下的处理逻辑
    /// </summary>
    private void HandleUAPMode()
    {
        string baseText = GetBaseText();
        if (string.IsNullOrEmpty(baseText))
            return;

        string finalText = "";
        if (ReadTarget == TestTTS_ValueReadMode.Text)
            finalText = ConstructFinalValue(baseText, ReadPrefix, ReadSuffix);
        else if (ReadTarget == TestTTS_ValueReadMode.Custom)
            finalText = baseText;

        finalText.UAPSpeak();
        // 延迟回调功能已取消，不再启动回调协程
    }

    #region Public Methods to Change ReadTarget
    public void _SetToTextMode()
    {
        ReadTarget = TestTTS_ValueReadMode.Text;
    }

    public void _SetToCustomMode()
    {
        ReadTarget = TestTTS_ValueReadMode.Custom;
    }

    public void _SetToNoneMode()
    {
        ReadTarget = TestTTS_ValueReadMode.None;
    }
    #endregion
}
