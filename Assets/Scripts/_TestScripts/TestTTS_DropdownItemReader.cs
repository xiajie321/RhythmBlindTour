using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QFramework;

/// <summary>
/// 需要添加“可被选择 selectable”组件
/// </summary>
public class TestTTS_DropdownItemReader : MonoBehaviour, ISelectHandler
{
    /// <summary>
    /// 当该选项被选中（高亮）时触发
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        // 尝试获取子物体中的 TMP_Text
        TMP_Text tmpText = GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text.UAPSpeak();
        }
        else
        {
            // 如果没有 TMP_Text，则尝试获取传统的 UI Text
            Text uiText = GetComponentInChildren<Text>();
            if (uiText != null)
            {
                uiText.text.UAPSpeak();
            }
        }
    }
}
