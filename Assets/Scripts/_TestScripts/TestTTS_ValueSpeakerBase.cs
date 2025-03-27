using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;

/// <summary>
/// 输入值朗读基类，仅保留输入框相关的变量。
/// ReadMode 支持 Text、Custom 和 None：
///   - Text：朗读指定输入框的内容；
///   - Custom：朗读自定义组合的内容；
///   - None：不朗读。
/// 前缀和后缀仅在 Text 模式下生效，用于构造最终朗读文本。
/// </summary>
public enum TestTTS_ValueReadMode
{
    Text,    // 朗读指定输入框的内容
    Custom,  // 朗读自定义组合的内容
    None     // 不朗读
}

[System.Serializable]
public class CustomValue
{
    [HideInInspector]
    public string GroupName;  // 自动设置的组名（不在 Inspector 中显示）

    public TMP_Text TargetTMPInput;
    public Text TargetInput;
    public string DefaultValue;
}

public abstract class TestTTS_ValueSpeakerBase : MonoBehaviour, ISelectHandler
{
    #region ------ Input Field ------
    [Tooltip("拖入指定的 TMP_InputField 组件，朗读时优先使用此输入框的文本。")]
    public TMP_InputField TargetTMPInputField;
    [Tooltip("拖入指定的 InputField 组件，朗读时优先使用此输入框的文本（当 TargetTMPInputField 为空时）。")]
    public InputField TargetInputField;
    #endregion

    #region ------ Read Mode ------
    [Tooltip("选择朗读对象：‘Text’使用上面的输入框内容；‘Custom’使用自定义组合文本；‘None’则不朗读。")]
    public TestTTS_ValueReadMode ReadTarget = TestTTS_ValueReadMode.Text;
    #endregion

    #region ------ Speech Settings ------
    [Tooltip("朗读时的前缀（仅 Text 模式下生效），允许为空。")]
    public string ReadPrefix = "";
    [Tooltip("朗读时的后缀（仅 Text 模式下生效），允许为空。")]
    public string ReadSuffix = "";
    #endregion

    [Tooltip("当 ReadTarget 为 Custom 时使用，组合朗读内容。")]
    public CustomValue[] ValueArray;

    #region ------ Control Options ------
    [Tooltip("是否启用组件内 OnSelect 触发的朗读功能")]
    public bool EnableOnSelected = true;
    #endregion

    #region ------ Caching Fields (供派生类使用) ------
    protected TMP_InputField _cachedTMPInputField;
    protected InputField _cachedInputField;
    #endregion

    /// <summary>
    /// 构造最终朗读文本：将前缀、基本文本和后缀以逗号分隔组合在一起。
    /// 若基本文本为空，则返回空字符串。（仅 Text 模式下调用）
    /// </summary>
    protected string ConstructFinalValue(string baseValue, string prefix, string suffix)
    {
        if (string.IsNullOrEmpty(baseValue))
        {
            baseValue = "";
        }
        string result = "";
        if (!string.IsNullOrEmpty(prefix))
        {
            result += prefix + ", ";
        }
        result += baseValue;
        if (!string.IsNullOrEmpty(suffix))
        {
            result += ", " + suffix;
        }
        return result;
    }

    #region ------ Helper Methods (供派生类调用) ------
    /// <summary>
    /// 缓存当前物体或其父级第一个子物体上的输入组件
    /// </summary>
    protected void CacheInputFields()
    {
        if (transform.childCount > 0)
        {
            _cachedTMPInputField = transform.GetChild(0).GetComponent<TMP_InputField>();
            if (_cachedTMPInputField == null)
            {
                _cachedInputField = transform.GetChild(0).GetComponent<InputField>();
            }
        }
        if ((_cachedTMPInputField == null && _cachedInputField == null) &&
            transform.parent != null && transform.parent.childCount > 0)
        {
            _cachedTMPInputField = transform.parent.GetChild(0).GetComponent<TMP_InputField>();
            if (_cachedTMPInputField == null)
            {
                _cachedInputField = transform.parent.GetChild(0).GetComponent<InputField>();
            }
        }
    }

    /// <summary>
    /// 根据当前 ReadTarget 模式获取基础文本（不包含前后缀）。
    /// Text 模式：优先使用指定输入框；Custom 模式：遍历 ValueArray 组合文本；None 模式返回空字符串。
    /// </summary>
    public string GetBaseText()
    {
        string text = "";
        if (ReadTarget == TestTTS_ValueReadMode.Text)
        {
            if (TargetTMPInputField != null)
                text = TargetTMPInputField.text;
            else if (TargetInputField != null)
                text = TargetInputField.text;
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
                    if (ValueArray[i].TargetInput != null)
                        val = ValueArray[i].TargetInput.text;
                    else if (ValueArray[i].TargetTMPInput != null)
                        val = ValueArray[i].TargetTMPInput.text;
                    else
                        val = ValueArray[i].DefaultValue;

                    if (builder.Length > 0)
                        builder.Append(" ");
                    builder.Append(val);
                }
            }
            text = builder.ToString();
        }
        else // None
        {
            text = "";
        }
        return text;
    }
    #endregion

    public abstract void OnSelect(BaseEventData eventData);
}
