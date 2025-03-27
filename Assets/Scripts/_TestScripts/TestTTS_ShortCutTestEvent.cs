using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 独立的快捷键事件处理器（第二个功能）
/// 使用 UnityEvent 作为回调事件，快捷键直接选用 KeyCode 枚举。
/// 前缀：TestTTS_ShortCutTestEvent
/// </summary>
public class TestTTS_ShortCutTestEvent : MonoBehaviour
{
    [System.Serializable]
    public class TestTTS_ShortCutTestEventEntry
    {
        public string groupName;

        [Tooltip("触发该事件的回调")]
        public UnityEvent shortcutEvent;

        [Tooltip("触发该事件的快捷键，直接选择 KeyCode 枚举")]
        public KeyCode keyCode = KeyCode.None;
    }

    [Tooltip("快捷键事件条目列表")]
    public List<TestTTS_ShortCutTestEventEntry> shortcuts = new List<TestTTS_ShortCutTestEventEntry>();

    private void Update()
    {
        // 遍历所有快捷键条目
        foreach (var entry in shortcuts)
        {
            if (entry.shortcutEvent == null)
                continue;

            if (entry.keyCode == KeyCode.None)
                continue;

            // 当对应的按键按下时，直接调用 UnityEvent
            if (Input.GetKeyDown(entry.keyCode))
            {
                entry.shortcutEvent.Invoke();
            }
        }
    }
}
