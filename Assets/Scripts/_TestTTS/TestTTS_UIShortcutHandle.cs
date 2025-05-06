using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 暂时使用的测试脚本，会弃用。
/// </summary>
public class TestTTS_UIShortcutHandle : MonoBehaviour
{
    [System.Serializable]
    public class UIShortcutEntry
    {
        public string groupName;

        [Tooltip("目标 GameObject，需要挂有 Button 或 EventTrigger 组件")]
        public GameObject target;

        [Tooltip("事件类型：如果目标上有 Button 组件，则可选择 Click 或 None；如果有 EventTrigger，则可选择 Click、PointDown 或 PointUp")]
        public UIShortcutEventType eventType = UIShortcutEventType.None;

        [Tooltip("触发该操作的快捷键，使用 KeyCode 枚举名称的字符串，例如 'A'、'Space'、'LeftArrow' 等")]
        public string keyCode = "";

        [Tooltip("如果为 true，则使用 hotKey 而不是字符串 keyCode；如果字符串为空，则会自动设置为 true")]
        public bool useHotKey;

        [Tooltip("使用 useHotKey 时，触发该操作的快捷键")]
        public KeyCode hotKey;
    }

    public enum UIShortcutEventType
    {
        None,
        Click,
        PointDown,
        PointUp
    }

    [Tooltip("快捷键条目列表")]
    public List<UIShortcutEntry> shortcuts = new List<UIShortcutEntry>();

    private void Update()
    {
        // 如果场景中没有 EventSystem，则无法模拟 UI 事件
        if (EventSystem.current == null)
            return;

        // 遍历所有快捷键条目
        foreach (var entry in shortcuts)
        {
            if (entry.target == null)
                continue;

            // 根据 useHotKey 决定使用哪个 KeyCode
            KeyCode keyToCheck = KeyCode.None;
            if (entry.useHotKey)
            {
                keyToCheck = entry.hotKey;
            }
            else
            {
                // 如果字符串为空，则自动使用 hotKey
                if (string.IsNullOrEmpty(entry.keyCode))
                {
                    entry.useHotKey = true;
                    keyToCheck = entry.hotKey;
                }
                else
                {
                    // 尝试将字符串转换为 KeyCode（忽略大小写）
                    if (!System.Enum.TryParse(entry.keyCode, true, out keyToCheck))
                    {
                        ($"无法将字符串 \"{entry.keyCode}\" 转换为 KeyCode").TTSLog(Color.yellow);
                        continue;
                    }
                }
            }

            if (keyToCheck == KeyCode.None)
                continue;

            // 根据不同事件类型选择不同的按键触发方式
            switch (entry.eventType)
            {
                case UIShortcutEventType.Click:
                    // 使用 GetKeyUp 触发 Click 事件
                    if (Input.GetKeyUp(keyToCheck))
                    {
                        PointerEventData eventData = new PointerEventData(EventSystem.current);
                        ExecuteEvents.Execute(entry.target, eventData, ExecuteEvents.pointerClickHandler);
                    }
                    break;
                case UIShortcutEventType.PointDown:
                    if (Input.GetKeyDown(keyToCheck))
                    {
                        PointerEventData eventData = new PointerEventData(EventSystem.current);
                        ExecuteEvents.Execute(entry.target, eventData, ExecuteEvents.pointerDownHandler);
                    }
                    break;
                case UIShortcutEventType.PointUp:
                    if (Input.GetKeyUp(keyToCheck))
                    {
                        PointerEventData eventData = new PointerEventData(EventSystem.current);
                        ExecuteEvents.Execute(entry.target, eventData, ExecuteEvents.pointerUpHandler);
                    }
                    break;
                case UIShortcutEventType.None:
                default:
                    // 不执行任何操作
                    break;
            }
        }
    }
}
