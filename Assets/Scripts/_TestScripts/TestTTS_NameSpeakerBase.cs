using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;

public enum TestTTS_ReadMode
{
    UI,         // 朗读指定的 UI Text（Target Text-UI）
    Default,    // 朗读 Default Text 字符串
    Auto        // 自动搜索文本
}

public abstract class TestTTS_NameSpeakerBase : MonoBehaviour, ISelectHandler
{
    #region ------ Default Settings ------
    [Tooltip("如果不为空，则优先朗读此内容；否则自动搜索文本。")]
    public string DefaultText;
    #endregion

    #region ------ UI Text ------
    [Tooltip("拖入指定的 TMP_Text 组件，朗读时优先使用此组件的文本。")]
    public TMP_Text TargetTMPText;
    [Tooltip("拖入指定的 UI Text 组件，朗读时优先使用此组件的文本（当 TargetTMPText 为空时）。")]
    public Text TargetUIText;
    #endregion

    #region ------ Read Mode ------
    [Tooltip("选择朗读对象：‘朗读 UI Text’使用上面的 UI Text；‘朗读 Default Text’直接朗读 DefaultText；‘自动’自动搜索文本。")]
    public TestTTS_ReadMode ReadTarget = TestTTS_ReadMode.Auto;
    #endregion

    #region ------ Speech Settings ------
    [Tooltip("朗读时的前缀，允许为空。")]
    public string ReadPrefix = "";
    [Tooltip("朗读时的后缀，允许为空。")]
    public string ReadSuffix = "";
    #endregion

    #region ------ AudioClip ------
    [Tooltip("指定 MP3 播读时使用的 AudioClip。")]
    public AudioClip AudioClipToSpeak;
    [Tooltip("选择自动查找MP3时使用的文件夹路径：默认位置 Audio/_TTSAudios【/path】")]
    public string AudioClipNewPath;
    #endregion

    #region ------ Caching Fields (供派生类使用) ------
    protected TMP_Text _cachedTMPText;
    protected Text _cachedText;
    #endregion

    #region ------ Control Options ------
    [Tooltip("是否启用组件内 OnSelect 触发的朗读功能")]
    public bool EnableOnSelected = true;
    #endregion

    /// <summary>
    /// 构造最终朗读文本：将前缀、基本文本和后缀以逗号分隔组合在一起。
    /// 若基本文本为空，则返回空字符串。
    /// </summary>
    protected string ConstructFinalText(string baseText, string prefix, string suffix)
    {
        if (string.IsNullOrEmpty(baseText))
            baseText = "";
        string result = "";
        if (!string.IsNullOrEmpty(prefix))
            result += prefix + ", ";
        result += baseText;
        if (!string.IsNullOrEmpty(suffix))
            result += ", " + suffix;
        return result;
    }

    #region ------ Helper Methods (供派生类调用) ------
    /// <summary>
    /// 缓存当前物体或其父级第一个子物体上的文本组件
    /// </summary>
    protected void CacheTextComponents()
    {
        if (transform.childCount > 0)
        {
            _cachedTMPText = transform.GetChild(0).GetComponent<TMP_Text>();
            if (_cachedTMPText == null)
            {
                _cachedText = transform.GetChild(0).GetComponent<Text>();
            }
        }
        if ((_cachedTMPText == null && _cachedText == null) &&
            transform.parent != null && transform.parent.childCount > 0)
        {
            _cachedTMPText = transform.parent.GetChild(0).GetComponent<TMP_Text>();
            if (_cachedTMPText == null)
            {
                _cachedText = transform.parent.GetChild(0).GetComponent<Text>();
            }
        }
    }

    /// <summary>
    /// 根据当前 ReadTarget 模式获取基础文本（不包含前后缀）。
    /// UI 模式：优先使用 TargetTMPText，再 TargetUIText；
    /// Default 模式：使用 DefaultText；
    /// Auto 模式：采用自动搜索缓存的文本组件（先 TMP_Text，再 Text）。
    /// </summary>
    public string GetBaseText()
    {
        string text = "";
        switch (ReadTarget)
        {
            case TestTTS_ReadMode.UI:
                if (TargetTMPText != null && !string.IsNullOrEmpty(TargetTMPText.SafeText()))
                    text = TargetTMPText.SafeText();
                else if (TargetUIText != null && !string.IsNullOrEmpty(TargetUIText.SafeText()))
                    text = TargetUIText.SafeText();
                break;
            case TestTTS_ReadMode.Default:
                text = DefaultText;
                break;
            case TestTTS_ReadMode.Auto:
                if (_cachedTMPText != null && !string.IsNullOrEmpty(_cachedTMPText.SafeText()))
                    text = _cachedTMPText.SafeText();
                else if (_cachedText != null && !string.IsNullOrEmpty(_cachedText.SafeText()))
                    text = _cachedText.SafeText();
                break;
        }
        return text;
    }
    #endregion

    public abstract void OnSelect(BaseEventData eventData);
}
