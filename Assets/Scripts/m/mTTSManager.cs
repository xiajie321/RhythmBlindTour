using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[DisallowMultipleComponent]
public class mTTSManager : MonoBehaviour
{
    public static mTTSManager Instance { get; private set; }

    [Header("写入目标（当前阶段用它来代替真实发声）")]
    [SerializeField] private TMP_Text targetTMP;
    public TMP_Text TargetTMP { get => targetTMP; set => targetTMP = value; }

    [Header("默认选项（可被每次调用覆盖）")]
    public mTTS.Options defaultOptions = new mTTS.Options
    {
        interruptMode = mTTS.InterruptMode.Interrupt,
        writeMode = mTTS.WriteMode.Replace,
        simulateDuration = false,
        charactersPerSecond = 16f,
        minSeconds = 0.2f,
        maxSeconds = 6f
    };

    [Header("完成后是否清空 TMP")]
    public bool clearTMPOnFinish = false;

    [Header("全局开关")]
    [SerializeField] private bool ttsEnabled = true;  // ← Inspector 勾选 = 开启
    public bool TTSEnabled => ttsEnabled;

    [Header("事件")]
    public UnityEvent<string> OnSpeakStart;
    public UnityEvent<string> OnSpeakEnd;
    public UnityEvent<string> OnInterrupted;
    public UnityEvent<int> OnQueueCountChanged;
    public UnityEvent<bool> OnTTSEnabledChanged; // 可选：开关变化事件

    /// <summary>是否正在“朗读”</summary>
    public bool IsSpeaking { get; private set; }

    private readonly Queue<(string text, mTTS.Options opt)> _queue = new();
    private Coroutine _runner;
    private string _currentText = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[mTTSManager] 场景中已存在另一个实例，销毁本对象。");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
     void Start()
    {
        if (ttsEnabled)
        {
            mTTS.Speak("欢迎");    

        }    
    }

    // ====== 新增：供 UIToggle 绑定 ======
    /// <summary>设置 TTS 总开关。on=false 时会立即打断并清空队列。</summary>
    public void SetTTSEnabled(bool on)
    {
        if (ttsEnabled == on) { OnTTSEnabledChanged?.Invoke(on); return; }
        ttsEnabled = on;

        // 关掉时，打断当前、清队；是否清空 TMP 由 clearTMPOnFinish 决定
        if (!ttsEnabled)
        {
            StopSpeaking(clearQueue: true, clearTMP: clearTMPOnFinish, raiseInterrupted: false);
        }

        OnTTSEnabledChanged?.Invoke(on);
    }

    /// <summary>根据选项立刻处理：打断/入队/丢弃</summary>
    public bool EnqueueOrPlay(string text, mTTS.Options options)
    {
        if (!ttsEnabled) return false; // ← 总开关：关闭时直接忽略

        var opt = options == null ? defaultOptions.Clone() : options.Clone();

        switch (opt.interruptMode)
        {
            case mTTS.InterruptMode.Interrupt:
                StopSpeaking(clearQueue: false, clearTMP: false, raiseInterrupted: true);
                _queue.Clear();
                _queue.Enqueue((text, opt));
                RaiseQueueChanged();
                TryRun();
                return true;

            case mTTS.InterruptMode.Queue:
                _queue.Enqueue((text, opt));
                RaiseQueueChanged();
                TryRun();
                return true;

            case mTTS.InterruptMode.DropIfBusy:
                if (IsSpeaking || _queue.Count > 0) return false;
                _queue.Enqueue((text, opt));
                RaiseQueueChanged();
                TryRun();
                return true;
        }
        return false;
    }

    /// <summary>单纯入队，不改变当前行为</summary>
    public bool Enqueue(string text, mTTS.Options options)
    {
        if (!ttsEnabled) return false; // ← 总开关：关闭时直接忽略

        var opt = options == null ? defaultOptions.Clone() : options.Clone();
        _queue.Enqueue((text, opt));
        RaiseQueueChanged();
        TryRun();
        return true;
    }

    /// <summary>停止当前朗读</summary>
    public void StopSpeaking(bool clearQueue, bool clearTMP, bool raiseInterrupted = true)
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }

        bool wasSpeaking = IsSpeaking;
        IsSpeaking = false;

        if (clearQueue)
        {
            _queue.Clear();
            RaiseQueueChanged();
        }

        if (clearTMP && targetTMP != null)
            targetTMP.text = string.Empty;

        if (wasSpeaking && raiseInterrupted)
            OnInterrupted?.Invoke(_currentText);

        _currentText = null;
    }

    /// <summary>清空队列</summary>
    public void ClearQueue()
    {
        _queue.Clear();
        RaiseQueueChanged();
    }

    private void TryRun()
    {
        if (_runner == null)
            _runner = StartCoroutine(RunQueue());
    }

    private IEnumerator RunQueue()
    {
        while (_queue.Count > 0)
        {
            var (text, opt) = _queue.Dequeue();
            RaiseQueueChanged();

            _currentText = text;
            IsSpeaking = true;
            OnSpeakStart?.Invoke(text);

            WriteToTMP(text, opt.writeMode);

            if (opt.simulateDuration)
            {
                float wait = CalcDuration(text, opt);
                if (wait > 0f)
                    yield return new WaitForSeconds(wait);
            }
            else
            {
                yield return null; // 1 帧
            }

            IsSpeaking = false;
            OnSpeakEnd?.Invoke(text);
            if (clearTMPOnFinish && targetTMP != null)
                targetTMP.text = string.Empty;

            _currentText = null;
        }

        _runner = null;
    }

    private void WriteToTMP(string text, mTTS.WriteMode writeMode)
    {
        if (targetTMP == null)
        {
            Debug.LogWarning("[mTTSManager] 未指定 TargetTMP，当前阶段无法显示朗读文本。");
            return;
        }

        switch (writeMode)
        {
            case mTTS.WriteMode.Replace:
                targetTMP.text = text;
                break;
            case mTTS.WriteMode.AppendLine:
                if (string.IsNullOrEmpty(targetTMP.text)) targetTMP.text = text;
                else targetTMP.text += "\n" + text;
                break;
            case mTTS.WriteMode.AppendSpace:
                if (string.IsNullOrEmpty(targetTMP.text)) targetTMP.text = text;
                else targetTMP.text += " " + text;
                break;
        }
    }

    private float CalcDuration(string text, mTTS.Options opt)
    {
        if (string.IsNullOrEmpty(text)) return opt.minSeconds;
        float seconds = text.Length / Mathf.Max(1e-3f, opt.charactersPerSecond);
        return Mathf.Clamp(seconds, opt.minSeconds, opt.maxSeconds);
    }

    

    private void RaiseQueueChanged() => OnQueueCountChanged?.Invoke(_queue.Count);
}
