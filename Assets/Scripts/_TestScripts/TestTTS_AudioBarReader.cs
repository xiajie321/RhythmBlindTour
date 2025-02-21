using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework; //且先接入


/// <summary>
/// 需要添加“可被选择 selectable”组件
/// </summary>
public class TestTTS_AudioBarReader : MonoBehaviour, ISelectHandler
{
    /// <summary>
    /// 尝试获取父级第一个子物体上的文本组件内容（优先 TMP_Text，其次 Text）
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
    /// 尝试获取自身第一个子物体上的文本组件内容（优先 TMP_Text，其次 Text）
    /// </summary>
    /// <returns>文本内容，未找到时返回空字符串</returns>
    private string GetSelfChildText()
    {
        if (transform.childCount > 0)
        {
            Transform firstChild = transform.GetChild(0);
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
    /// 当物体被选中时，获取父级和自身第一个子物体的文本，
    /// 两段文本以回车符分隔后朗读
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public void OnSelect(BaseEventData eventData)
    {
        string parentText = GetParentChildText();
        string selfText = GetSelfChildText();

        // 如果父级文本为空，直接返回
        if (string.IsNullOrEmpty(parentText))
        {
            return;
        }
        // 允许自身文本为空
        if (string.IsNullOrEmpty(selfText))
        {
            selfText = "";
        }

        // 用回车符分隔两段文本
        string finalText = parentText + "\n" + selfText;

        // 调用封装的 TTS 方法进行朗读
        finalText.UAPSpeak();
    }
}
