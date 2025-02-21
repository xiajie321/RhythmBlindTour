using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;

/// <summary>
/// 需要添加“可被选择 selectable”组件
//* 针对当前项目中两种情况处理
//*     1.功能名称在自身子级；2.功能名称于自身同级
/// </summary>
public class TestTTS_UINameSpeaker : MonoBehaviour, ISelectHandler
{
    [Header("Default Settings")]
    [Tooltip("如果无法找到对应的文本，则使用该文本进行朗读。")]
    public string DefaultText;

    [Header("Speech Settings")]
    [Tooltip("朗读时的前缀，允许为空。")]
    public string Prefix = "";
    [Tooltip("朗读时的后缀，允许为空。")]
    public string Suffix = "";

    // 缓存的文本组件引用（优先 TMP_Text，其次 Text）
    private TMP_Text _cachedTMPText;
    private Text _cachedText;

    private void Start()
    {
        // 尝试在自身第一个子物体中查找 TMP_Text 或 Text 组件
        if (transform.childCount > 0)
        {
            _cachedTMPText = transform.GetChild(0).GetComponent<TMP_Text>();
            if (_cachedTMPText == null)
            {
                _cachedText = transform.GetChild(0).GetComponent<Text>();
            }
        }

        // 如果自身查找不到，则到父级的第一个子物体中查找
        if ((_cachedTMPText == null && _cachedText == null) && transform.parent != null && transform.parent.childCount > 0)
        {
            _cachedTMPText = transform.parent.GetChild(0).GetComponent<TMP_Text>();
            if (_cachedTMPText == null)
            {
                _cachedText = transform.parent.GetChild(0).GetComponent<Text>();
            }
        }
    }

    /// <summary>
    /// 当 UI 被选中时触发
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public void OnSelect(BaseEventData eventData)
    {
        string textToSpeak = "";

        // 优先使用缓存的 TMP_Text 组件的文本
        if (_cachedTMPText != null && !string.IsNullOrEmpty(_cachedTMPText.text))
        {
            textToSpeak = _cachedTMPText.text;
        }
        // 如果 TMP_Text 没有，再使用缓存的 Text 组件的文本
        else if (_cachedText != null && !string.IsNullOrEmpty(_cachedText.text))
        {
            textToSpeak = _cachedText.text;
        }
        // 如果仍然为空，则尝试使用 DefaultText
        else if (!string.IsNullOrEmpty(DefaultText))
        {
            textToSpeak = DefaultText;
        }

        if (!string.IsNullOrEmpty(textToSpeak))
        {
            // 构造最终朗读文本：如果 Prefix 不为空，则加 Prefix 和逗号；如果 Suffix 不为空，则在文本后加逗号和 Suffix
            string finalText = "";
            if (!string.IsNullOrEmpty(Prefix))
            {
                finalText += Prefix + ", ";
            }
            finalText += textToSpeak;
            if (!string.IsNullOrEmpty(Suffix))
            {
                finalText += ", " + Suffix;
            }

            finalText.UAPSpeak();
        }
        else
        {
            ("未找到要朗读的文本！").TTSLog(Color.yellow);
        }
    }
}
