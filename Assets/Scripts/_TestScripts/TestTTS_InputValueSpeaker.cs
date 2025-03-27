using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;

public class TestTTS_InputValueSpeaker : TestTTS_ValueSpeakerBase
{
    #region ------ 初始化 ------
    private void Start()
    {
        CacheInputFields();
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
            case TestTTS_StaticActionUAP.ReadModeType.UAP:
                HandleUAPMode();
                break;
            case TestTTS_StaticActionUAP.ReadModeType.None:
                return;
        }
    }
    #endregion

    #region ------ UAP模式处理 ------
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
    }
    #endregion
}
