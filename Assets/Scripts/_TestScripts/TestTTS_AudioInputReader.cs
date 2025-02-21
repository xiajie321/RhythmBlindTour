using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;

/// <summary>
/// 需要添加“可被选择 selectable”组件
/// </summary>
public class TestTTS_AudioInputReader : MonoBehaviour, ISelectHandler
{
    private TMP_InputField mInputField;

    private void Awake()
    {
        mInputField = GetComponent<TMP_InputField>();
        if (mInputField == null)
        {
            ("TestTTS_AudioInputReader 脚本需要挂载在拥有 TMP_InputField 组件的 GameObject 上！").TTSLog(Color.yellow);
        }
    }

    /// <summary>
    /// 获取父级第一个子物体上的文本内容（优先 TMP_Text，其次 Text）
    /// </summary>
    /// <returns>文本内容，未找到时返回空字符串</returns>
    private string GetParentChildText()
    {
        if (transform.parent != null && transform.parent.childCount > 0)
        {
            Transform firstChild = transform.parent.GetChild(0);
            TMP_Text tmpText = firstChild.GetComponent<TMP_Text>();
            if (tmpText != null)
                return tmpText.text;
            Text uiText = firstChild.GetComponent<Text>();
            if (uiText != null)
                return uiText.text;
        }
        return "";
    }

    /// <summary>
    /// 当该物体被选中时，朗读内容为：
    /// 父级的第一个子物体的文本与自身输入框的文本直接拼接（不加额外分隔符）
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public void OnSelect(BaseEventData eventData)
    {
        string parentText = GetParentChildText();
        string inputText = mInputField != null ? mInputField.text : "";
        string finalText = parentText + inputText;
        finalText.UAPSpeak();
    }
}
