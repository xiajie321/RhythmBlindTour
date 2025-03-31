using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;
using System.Collections;

public enum TestTTS_ValueReadMode
{
    Text,    // 朗读指定输入框的内容
    Custom,  // 朗读自定义组合的内容
    None     // 不朗读
}

public enum CustomValueSource
{
    Text,      // 从 InputField 获取
    T_TMP,     // 从 TMP_InputField 获取（默认）
    Slider,    // 从 Slider 的 value 获取
    Default,   // 直接使用 DefaultValue 字符串
    Auto       // 自动查找：按 InputField > UI Text > Slider 顺序获取
}

[System.Serializable]
public class CustomValue
{
    [HideInInspector]
    public string GroupName;

    [Tooltip("选择此 CustomValue 获取文本的来源")]
    public CustomValueSource Source = CustomValueSource.Text;

    [Tooltip("如果选择 InputField，则指定此组件（其 Text 属性作为朗读内容）")]
    public Text TargetText;
    [Tooltip("如果选择 Text，则指定此组件（其 text 属性作为朗读内容）")]
    public TMP_Text TargetTextTMP;
    [Tooltip("如果选择 Slider，则指定此 Slider 组件，其 value 作为朗读内容")]
    public Slider TargetSlider;
    [Tooltip("当以上组件均未指定时，使用此默认文本")]
    public string DefaultValue;

    // redgin: Custom 模式下 MP3 朗读支持字段
    [Tooltip("在 Custom 模式下 MP3 播放时使用的 AudioClip")]
    public AudioClip Custom_AudioClip;

    // redgin: Custom 模式下 MP3 朗读优先控制字段，开启时将优先使用通过路径加载的 MP3
    [Tooltip("redgin: 当开启时，在 Custom 模式下 MP3 播放时优先使用通过路径加载的 MP3")]
    public bool Toggle;

    [Tooltip("在 Custom 模式下 MP3 播放时使用的新路径")]
    public string Custom_AudioClipNewPath;
}

public abstract class TestTTS_ValueSpeakerBase : MonoBehaviour, ISelectHandler
{
    #region Target Text
    [Tooltip("拖入指定的 TMP_InputField 组件，朗读时优先使用此输入框的文本。")]
    public TMP_Text TargetTMPText;
    [Tooltip("拖入指定的 InputField 组件，朗读时优先使用此输入框的文本（当 TargetTMPText 为空时）。")]
    public Text TargetText;
    #endregion

    #region Read Mode
    [Tooltip("选择朗读对象：‘Text’使用上面的输入框内容；‘Custom’使用自定义组合文本；‘None’则不朗读。")]
    public TestTTS_ValueReadMode ReadTarget = TestTTS_ValueReadMode.Text;
    #endregion

    #region Speech Settings
    [Tooltip("朗读时的前缀（仅 Text 模式下生效），允许为空。")]
    public string ReadPrefix = "";
    [Tooltip("朗读时的后缀（仅 Text 模式下生效），允许为空。")]
    public string ReadSuffix = "";
    #endregion

    #region MP3 Playback Settings (仅 Text 模式下使用)
    [Tooltip("在 Text 模式下 MP3 播放时使用的 AudioClip（IV前缀用于区分）")]
    public AudioClip IV_AudioClipToSpeak;
    [Tooltip("在 Text 模式下 MP3 播放时使用的新路径（IV前缀用于区分）")]
    public string IV_AudioClipNewPath;
    #endregion

    [Tooltip("当 ReadTarget 为 Custom 时使用，组合朗读内容。")]
    public CustomValue[] ValueArray;

    #region Control Options
    [Tooltip("是否启用组件内 OnSelect 触发的朗读功能")]
    public bool EnableOnSelected = true;
    #endregion

    #region Caching Fields
    protected TMP_InputField _cachedTMPInputField;
    protected InputField _cachedInputField;
    #endregion

    /// <summary>
    /// 构造最终朗读文本：将前缀、基本文本和后缀以逗号分隔组合在一起。
    /// 若基本文本为空，则返回空字符串。
    /// </summary>
    protected string ConstructFinalValue(string baseValue, string prefix, string suffix)
    {
        if (string.IsNullOrEmpty(baseValue))
            baseValue = "";
        string result = "";
        if (!string.IsNullOrEmpty(prefix))
            result += prefix + ", ";
        result += baseValue;
        if (!string.IsNullOrEmpty(suffix))
            result += ", " + suffix;
        return result;
    }

    #region Helper Methods
    /// <summary>
    /// 缓存当前物体或其父级第一个子物体上的输入组件
    /// </summary>
    protected void CacheInputFields()
    {
        if (transform.childCount > 0)
        {
            _cachedTMPInputField = transform.GetChild(0).GetComponent<TMP_InputField>();
            if (_cachedTMPInputField == null)
                _cachedInputField = transform.GetChild(0).GetComponent<InputField>();
        }
        if ((_cachedTMPInputField == null && _cachedInputField == null) &&
            transform.parent != null && transform.parent.childCount > 0)
        {
            _cachedTMPInputField = transform.parent.GetChild(0).GetComponent<TMP_InputField>();
            if (_cachedTMPInputField == null)
                _cachedInputField = transform.parent.GetChild(0).GetComponent<InputField>();
        }
    }

    /// <summary>
    /// 根据当前 ReadTarget 模式获取基础文本（不包含前后缀）。
    /// Text 模式：优先使用 TargetTMPText 或 TargetText（或缓存）；Custom 模式：遍历 ValueArray，根据每个元素的 Source 获取文本；None 模式返回空字符串。
    /// </summary>
    public string GetBaseText()
    {
        string text = "";
        if (ReadTarget == TestTTS_ValueReadMode.Text)
        {
            if (TargetTMPText != null)
                text = TargetTMPText.text;
            else if (TargetText != null)
                text = TargetText.text;
            else
            {
                if (_cachedTMPInputField != null)
                    text = _cachedTMPInputField.text;
                else if (_cachedInputField != null)
                    text = _cachedInputField.text;
            }
        }
        else if (ReadTarget == TestTTS_ValueReadMode.Custom)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (ValueArray != null)
            {
                for (int i = 0; i < ValueArray.Length; i++)
                {
                    string val = "";
                    CustomValue cv = ValueArray[i];
                    switch (cv.Source)
                    {
                        case CustomValueSource.Text:
                            if (cv.TargetText != null)
                                val = cv.TargetText.text;
                            break;
                        case CustomValueSource.T_TMP:
                            if (cv.TargetTextTMP != null)
                                val = cv.TargetTextTMP.text;
                            break;
                        case CustomValueSource.Slider:
                            if (cv.TargetSlider != null)
                                val = cv.TargetSlider.value.ToString();
                            break;
                        case CustomValueSource.Default:
                            val = cv.DefaultValue;
                            break;
                        case CustomValueSource.Auto:
                            if (cv.TargetText != null && !string.IsNullOrEmpty(cv.TargetText.text))
                                val = cv.TargetText.text;
                            else if (cv.TargetTextTMP != null && !string.IsNullOrEmpty(cv.TargetTextTMP.text))
                                val = cv.TargetTextTMP.text;
                            else if (cv.TargetSlider != null)
                                val = cv.TargetSlider.value.ToString();
                            else
                                val = cv.DefaultValue;
                            break;
                    }
                    if (builder.Length > 0)
                        builder.Append(" ");
                    builder.Append(val);
                }
            }
            text = builder.ToString();
        }
        else
        {
            text = "";
        }
        return text;
    }
    #endregion

    /// <summary>
    /// Custom 模式下 MP3 朗读：
    /// 依次播放 ValueArray 中的每个 CustomValue 的 MP3 文件，等待前一个播放完成后再播放下一个。
    /// </summary>
    public IEnumerator PlayCustomMP3Sequence()
    {
        if (ValueArray == null || ValueArray.Length == 0)
            yield break;

        for (int i = 0; i < ValueArray.Length; i++)
        {
            CustomValue cv = ValueArray[i];
            string customText = "";
            switch (cv.Source)
            {
                case CustomValueSource.Text:
                    if (cv.TargetText != null)
                        customText = cv.TargetText.text;
                    break;
                case CustomValueSource.T_TMP:
                    if (cv.TargetTextTMP != null)
                        customText = cv.TargetTextTMP.text;
                    break;
                case CustomValueSource.Slider:
                    if (cv.TargetSlider != null)
                        customText = cv.TargetSlider.value.ToString();
                    break;
                case CustomValueSource.Default:
                    customText = cv.DefaultValue;
                    break;
                case CustomValueSource.Auto:
                    if (cv.TargetText != null && !string.IsNullOrEmpty(cv.TargetText.text))
                        customText = cv.TargetText.text;
                    else if (cv.TargetTextTMP != null && !string.IsNullOrEmpty(cv.TargetTextTMP.text))
                        customText = cv.TargetTextTMP.text;
                    else if (cv.TargetSlider != null)
                        customText = cv.TargetSlider.value.ToString();
                    else
                        customText = cv.DefaultValue;
                    break;
            }
            if (string.IsNullOrEmpty(customText))
                continue;

            // redgin: 根据 Toggle 的状态决定优先加载方式。在 Toggle 开启时，始终使用路径加载的 MP3 替代直接指定的 AudioClip
            if (cv.Toggle)
            {
                cv.Custom_AudioClip = TestTTS_StaticActionUAP.TryLoadAudioClip(customText, cv.Custom_AudioClipNewPath);
            }
            else if (cv.Custom_AudioClip == null)
            {
                cv.Custom_AudioClip = TestTTS_StaticActionUAP.TryLoadAudioClip(customText, cv.Custom_AudioClipNewPath);
            }

            if (cv.Custom_AudioClip != null)
            {
                TestTTS_StaticActionUAP.PlayAudioClip(cv.Custom_AudioClip);
                yield return new WaitUntil(() =>
                {
                    GameObject go = GameObject.Find("TTSAudioSpeaker");
                    if (go != null)
                    {
                        AudioSource asrc = go.GetComponent<AudioSource>();
                        return asrc == null || !asrc.isPlaying;
                    }
                    return true;
                });
            }
            else
            {
                ("未能加载 Custom 模式下第 " + (i + 1) + " 个 MP3 文件").TTSLog(Color.red);
            }
        }
    }

    public abstract void OnSelect(BaseEventData eventData);
}
