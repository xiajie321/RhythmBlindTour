using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;

/// <summary>
/// 需要添加“可被选择 selectable”组件
/// </summary>
public class TestTTS_AudioSliderReader : MonoBehaviour, ISelectHandler
{
    /// <summary>
    /// 从父级的第一个子物体上获取文本组件的内容（优先 TMP_Text，其次 Text），未找到则返回空字符串。
    /// </summary>
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
    /// 当该 Slider 被选中时调用：
    /// 朗读父级第一个子物体的文本与当前 Slider 数值的组合（直接拼接）。
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        string parentText = GetParentChildText();
        Slider slider = GetComponent<Slider>();
        string sliderValue = slider != null ? slider.value.ToString() : "";
        // 直接拼接父级文本与 Slider 数值（无分隔符）
        string finalText = parentText + sliderValue;
        finalText.UAPSpeak();
    }

    /// <summary>
    /// 在 Start 中为 Slider 注册数值改变事件，
    /// 当 Slider 数值变化时只朗读 Slider 的数值（不包含前缀）。
    /// </summary>
    private void Start()
    {
        Slider slider = GetComponent<Slider>();
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    /// <summary>
    /// Slider 数值改变时调用，只朗读 Slider 的数值。
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        // 直接将数值转换为字符串进行朗读
        value.ToString().UAPSpeak();
    }
}
