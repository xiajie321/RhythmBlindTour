using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using QFramework;

/// <summary>
/// 需要添加“可被选择 selectable”组件
/// </summary>
public class TestTTS_InputFieldRealTimeSpeaker : MonoBehaviour, ISelectHandler
{
    private TMP_InputField inputField;

    [Header("Speech Settings")]
    [Tooltip("朗读时的前缀，允许为空。")]
    public string Prefix = "";
    [Tooltip("朗读时的后缀，允许为空。")]
    public string Suffix = "";

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        if (inputField == null)
        {
            ("TestTTS_InputFieldRealTimeSpeaker 需要挂载在拥有 TMP_InputField 组件的 GameObject 上。").TTSLog(Color.red);
        }
    }

    private void Start()
    {
        if (inputField != null)
        {
            // 监听输入内容变化，每次输入变化都实时朗读
            inputField.onValueChanged.AddListener(OnInputValueChanged);
        }
    }

    /// <summary>
    /// 当输入框获得焦点时朗读当前文本（包含前缀和后缀）
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public void OnSelect(BaseEventData eventData)
    {
        if (inputField != null)
        {
            string finalText = ConstructFinalText(inputField.text);
            finalText.UAPSpeak();
        }
    }

    /// <summary>
    /// 当输入内容变化时触发，实时朗读当前文本（包含前缀和后缀）
    /// </summary>
    /// <param name="newText">最新输入的文本</param>
    private void OnInputValueChanged(string newText)
    {
        string finalText = ConstructFinalText(newText);
        finalText.UAPSpeak();
    }

    /// <summary>
    /// 根据前缀、基本文本和后缀构造最终朗读文本，
    /// 如果基本文本为空，则使用“未设置”，
    /// 各部分之间用逗号和空格分隔。
    /// </summary>
    /// <param name="baseText">输入框的文本内容</param>
    /// <returns>构造后的朗读文本</returns>
    private string ConstructFinalText(string baseText)
    {
        if (string.IsNullOrEmpty(baseText))
        {
            baseText = "未设置";
        }

        string result = "";
        if (!string.IsNullOrEmpty(Prefix))
        {
            result += Prefix + ", ";
        }
        result += baseText;
        if (!string.IsNullOrEmpty(Suffix))
        {
            result += ", " + Suffix;
        }
        return result;
    }
}
