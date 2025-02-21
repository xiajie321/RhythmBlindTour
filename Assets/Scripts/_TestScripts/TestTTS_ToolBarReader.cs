using UnityEngine;
using UnityEngine.EventSystems;
using QFramework; // QFramework 的命令系统

/// <summary>
/// 需要添加“可被选择 selectable”组件
/// 
// *TTS模式枚举：仅朗读文本、仅播放音频、或两者同时 -主要用来朗读“工具1，2，3，4，5”
// *注册在其点击事件中，启用“点击”时，可以朗读其功能，后面需要重新处理为注册到QF框架的事件中。
/// </summary>
public enum TTSSourceType
{
    Text,
    Audio,
    Both
}

/// <summary>
/// 挂载在具有 EventTrigger 组件的 GameObject 上，
/// 当检测到 PointerDown 事件时，根据设置朗读文本、播放音频或同时执行两者。
/// 允许通过 Inspector 设置朗读文本、音频剪辑、模式以及是否允许重复触发。
/// </summary>
public class TestTTS_ToolBarReader : MonoBehaviour
{
    [Tooltip("要朗读的文本字符串。")]
    public string textToSpeak;

    [Tooltip("要播放的音频剪辑。")]
    public AudioClip audioClip;

    [Tooltip("选择 TTS 模式：仅朗读文本、仅播放音频或同时执行两者。")]
    public TTSSourceType mode = TTSSourceType.Text;

    [Tooltip("是否允许重复触发（如果为 false，则只在第一次触发时朗读）。")]
    public bool allowRepeat = true;

    // 内部标志，若 allowRepeat 为 false，则在第一次触发后置为 true，后续不再触发
    private bool hasFired = false;

    private EventTrigger eventTrigger;
    private AudioSource audioSource;

    private void Awake()
    {
        eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            ("TestTTS_EventTriggerPointDownReader 需要挂载在具有 EventTrigger 组件的 GameObject 上。").TTSLog(Color.red);
            return;
        }

        // 如果模式涉及播放音频，确保当前物体有 AudioSource 组件
        if (mode == TTSSourceType.Audio || mode == TTSSourceType.Both)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // 注册 PointerDown 事件
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => { OnPointerDown((PointerEventData)data); });
        eventTrigger.triggers.Add(entry);
    }

    /// <summary>
    /// PointerDown 事件回调，根据模式执行朗读和/或播放音频
    /// </summary>
    /// <param name="data">PointerEventData 数据</param>
    private void OnPointerDown(PointerEventData data)
    {
        if (!allowRepeat && hasFired)
        {
            return;
        }

        // 模式为 Text 或 Both，则朗读文本
        if (mode == TTSSourceType.Text || mode == TTSSourceType.Both)
        {
            if (!string.IsNullOrEmpty(textToSpeak))
            {
                textToSpeak.UAPSpeak();
            }
            else
            {
                ("textToSpeak 为空，无法朗读。").TTSLog(Color.yellow);
            }
        }

        // 模式为 Audio 或 Both，则播放音频
        if (mode == TTSSourceType.Audio || mode == TTSSourceType.Both)
        {
            if (audioClip != null)
            {
                audioSource.PlayOneShot(audioClip);
            }
            else
            {
                ("audioClip 未设置。").TTSLog(Color.yellow);
            }
        }

        if (!allowRepeat)
        {
            hasFired = true;
        }
    }
}
