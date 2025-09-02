using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("RB/TTS/RB_TTSTest (Test & Control)")]
public class RB_TTSTest : MonoBehaviour
{
    [Header("Awake 自动唤醒无障碍")]
    [Tooltip("勾选则在 Awake() 调用 RB_TTS.RB_WakeAccessibility(wakeEnableState)。仅 Win/macOS 有效")]
    public bool autoWakeOnAwake = true;

    [Tooltip("自动唤醒时传入的启用状态：true=启用；false=关闭")]
    public bool wakeEnableState = true;

    [Header("开机播报")]
    [Tooltip("Start() 时播报一条欢迎语")]
    public bool sayHelloOnStart = true;

    [Tooltip("欢迎语内容")]
    public string helloText = "欢迎。";

    [Header("箭头键列表测试")]
    [Tooltip("按上下左右切换并朗读该列表内容")]
    public List<string> testItems = new List<string> { "第一关", "第二关", "第三关" };

    [Tooltip("是否在列表末尾/开头回绕")]
    public bool loopList = true;

    [Tooltip("排序播放（true）还是打断播放（false=当前方案）")]
    public bool queueMode = false;

    [Tooltip("当前索引（仅展示/调试，可在 Inspector 中手动改）")]
    public int currentIndex = 0;

    private float _repeatGuard = 0f;       // 简单防抖：避免同一帧多次触发
    private const float RepeatCD = 0.02f;  // 20ms

    private void Awake()
    {
        if (autoWakeOnAwake)
        {
            // 仅 Win/macOS 生效；内部已做平台宏判断
            RB_TTS.RB_WakeAccessibility(wakeEnableState); // UAP: EnableAccessibility(enable)  :contentReference[oaicite:2]{index=2}
        }
    }

    private void Start()
    {
        if (sayHelloOnStart && !string.IsNullOrEmpty(helloText))
        {
            // 开机播报一条欢迎语（默认打断式）
            RB_TTS.RB_Say(helloText, interruptBeforeSpeak: true);  // UAP: Say()  :contentReference[oaicite:3]{index=3}
        }

        // 规范化索引
        ClampIndex();
    }


    #region 测试用
    private void Update()
    {
        // 仅用于测试：监听箭头键，上/左=上一项，下/右=下一项
        bool moved = false;
        if (Time.unscaledTime - _repeatGuard < RepeatCD) return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MovePrev();
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveNext();
            moved = true;
        }

        if (moved)
        {
            _repeatGuard = Time.unscaledTime;
            SpeakCurrent();
        }
    }

    private void MovePrev()
    {
        if (testItems == null || testItems.Count == 0) return;

        if (currentIndex > 0) currentIndex--;
        else if (loopList) currentIndex = testItems.Count - 1;
        else currentIndex = 0;
    }

    private void MoveNext()
    {
        if (testItems == null || testItems.Count == 0) return;

        if (currentIndex < testItems.Count - 1) currentIndex++;
        else if (loopList) currentIndex = 0;
        else currentIndex = testItems.Count - 1;
    }

    private void SpeakCurrent()
    {
        if (testItems == null || testItems.Count == 0) return;

        string text = testItems[Mathf.Clamp(currentIndex, 0, testItems.Count - 1)];
        if (string.IsNullOrEmpty(text)) return;

        // 两种模式：
        // - 打断播放（false）：先 Stop 再 Say（稳定即时反馈，等同于你当前方案）
        // - 排序播放（true）：不 Stop，直接 Say（交给 UAP 的内部队列/拼接处理）
        bool interruptBeforeSpeak = !queueMode;

        RB_TTS.RB_Say(text, interruptBeforeSpeak); // UAP: StopSpeaking()/Say()  :contentReference[oaicite:4]{index=4}
    }

    private void ClampIndex()
    {
        if (testItems == null || testItems.Count == 0) { currentIndex = 0; return; }
        currentIndex = Mathf.Clamp(currentIndex, 0, testItems.Count - 1);
    }
    
    #endregion
}