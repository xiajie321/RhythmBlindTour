// File: UIAudioEditShowMassage.cs
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Managers;
using Qf.Models.AudioEdit;
using Qf.Querys.AudioEdit;
using QFramework;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景内信息展示（模式 / 时间 / BPM 及拍号等）。
/// 新增 4 种朗读模式切换：
/// - 秒（SetSpeakSeconds）
/// - 绝对拍序（SetSpeakBeatAbsolute）：从 1 开始计拍，不按小节分组
/// - 小节（SetSpeakMeasureOnly）
/// - 小节+拍（SetSpeakMeasureBeat）：例如“第1小节第1拍”
/// 拍点换算：60/BPM * (4/BeatB)，半开区间 + 微小偏移避免边界抖动。
/// </summary>
public class UIAudioEditShowMassage : MonoBehaviour, IController
{
    [Header("显示文本")]
    [SerializeField] private TMP_Text _ModeShow;
    [SerializeField] private TMP_Text _TimeShow;
    [SerializeField] private TMP_Text _BPMShow;

    [Header("拍号选择（可选显示/回写）")]
    [SerializeField] private TMP_Dropdown _A;   // 分子：每小节拍数（1~N）
    [SerializeField] private TMP_Dropdown _B;   // 分母：{1,2,4,8,...}

    [Header("时间朗读设置")]
    [SerializeField] private bool speakTimeOnUpdate = false;
    [SerializeField] private bool safeAtFrameEnd = true;
    [SerializeField] private mTTS.TimeReadStyle timeReadStyle = mTTS.TimeReadStyle.NumericTokens;

    public enum SpeakTimeFormat
    {
        Seconds,        // 秒
        BeatAbsolute,   // 绝对拍序（从 1 开始，不按小节分组）
        MeasureOnly,    // 只读小节
        MeasureBeat     // 小节 + 拍（例：“第1小节第1拍”）
    }

    [Header("朗读格式切换")]
    [SerializeField] private SpeakTimeFormat speakFormat = SpeakTimeFormat.Seconds;

    [Header("节拍朗读参数")]
    [Tooltip("边界偏移（秒），用于半开区间 [k*beat,(k+1)*beat) 的浮点安全计算")]
    [SerializeField] private float boundaryEpsilonSec = 1e-4f;

    // —— 最新时间缓存（避免从 TMP 取值造成延迟） ——
    private float _lastTimeSec = 0f;

    // —— 帧末安全朗读版本号（同帧只保留最后一次） ——
    private int _safeSpeakVer = 0;

    public IArchitecture GetArchitecture() => GameBody.Interface;

    #region Unity Lifecycle & Event Wiring
    private void Start()
    {
        // 模式显示
        this.RegisterEvent<OnEditMode>(_ => { if (_ModeShow) _ModeShow.text = "编辑模式"; })
            .UnRegisterWhenGameObjectDestroyed(gameObject);
        this.RegisterEvent<OnPlayMode>(_ => { if (_ModeShow) _ModeShow.text = "游玩模式"; })
            .UnRegisterWhenGameObjectDestroyed(gameObject);
        this.RegisterEvent<OnRecordingMode>(_ => { if (_ModeShow) _ModeShow.text = "录制模式"; })
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // 时间更新：写 UI + 缓存数值；如需自动播报，延迟到帧末
        this.RegisterEvent<OnUpdateThisTime>(v =>
        {
            if (_TimeShow) _TimeShow.text = v.ThisTime.ToString("0.00");
            _lastTimeSec = v.ThisTime;
            if (speakTimeOnUpdate) ScheduleSafeSpeakTime();
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

        // BPM 改变 & 关卡加载 → 刷新 BPM 文本与拍号下拉
        this.RegisterEvent<BPMChangeValue>(e =>
        {
            if (_BPMShow) _BPMShow.text = e.BPM.ToString();
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

        this.RegisterEvent<AudioEditModelLoad>(_ =>
        {
            var m = this.GetModel<AudioEditModel>();
            if (_BPMShow) _BPMShow.text = m != null ? m.BPM.ToString() : string.Empty;
            RefreshBeatDropdownFromModel();
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

        // 初始化 BPM 显示
        var model = this.GetModel<AudioEditModel>();
        if (_BPMShow) _BPMShow.text = model != null ? model.BPM.ToString() : string.Empty;

        // （可选）拍号下拉监听 → 回写模型并触发外部重绘
        if (_A) _A.onValueChanged.AddListener(_ => OnBeatDropdownChanged());
        if (_B) _B.onValueChanged.AddListener(_ => OnBeatDropdownChanged());

        RefreshBeatDropdownFromModel();
    }

    private void OnDestroy()
    {
        if (_A) _A.onValueChanged.RemoveListener(_ => OnBeatDropdownChanged());
        if (_B) _B.onValueChanged.RemoveListener(_ => OnBeatDropdownChanged());
    }

    private void OnEnable()
    {
        StartCoroutine(DelayedSendThisTime());
    }

    private IEnumerator DelayedSendThisTime()
    {
        yield return null;
        float cur = this.SendQuery(new QueryAudioEditAudioClipThisTime());
        this.SendEvent(new OnUpdateThisTime { ThisTime = cur });
    }
    #endregion

    #region 外部可调用：设置朗读模式（四个方法）
    /// <summary>朗读时间（秒）。</summary>
    public void SetSpeakSeconds() => speakFormat = SpeakTimeFormat.Seconds;

    /// <summary>朗读节拍（绝对拍序，从 1 开始计数，不按小节分组）。</summary>
    public void SetSpeakBeatAbsolute() => speakFormat = SpeakTimeFormat.BeatAbsolute;

    /// <summary>朗读小节。</summary>
    public void SetSpeakMeasureOnly() => speakFormat = SpeakTimeFormat.MeasureOnly;

    /// <summary>朗读“小节+拍”。</summary>
    public void SetSpeakMeasureBeat() => speakFormat = SpeakTimeFormat.MeasureBeat;
    #endregion

    #region Speak API
    /// <summary>按当前 speakFormat 朗读。</summary>
    public void SpeakTimeNow()
    {
        var opt = new mTTS.Options
        {
            interruptMode = mTTS.InterruptMode.Interrupt,
            writeMode = mTTS.WriteMode.Replace,
            simulateDuration = false
        };

        switch (speakFormat)
        {
            case SpeakTimeFormat.Seconds:
                {
                    mTTS.SpeakSeconds(_lastTimeSec, appendUnit: true, style: timeReadStyle, options: opt);
                    break;
                }
            case SpeakTimeFormat.BeatAbsolute:
                {
                    string text = FormatBeatAbsoluteText(_lastTimeSec, out bool ok);
                    mTTS.Speak(ok ? text : BuildSecondsOnly(), opt);
                    break;
                }
            case SpeakTimeFormat.MeasureOnly:
                {
                    string text = FormatMeasureOnlyText(_lastTimeSec, out bool ok);
                    mTTS.Speak(ok ? text : BuildSecondsOnly(), opt);
                    break;
                }
            case SpeakTimeFormat.MeasureBeat:
                {
                    string text = FormatMeasureBeatText(_lastTimeSec, out bool ok);
                    mTTS.Speak(ok ? text : BuildSecondsOnly(), opt);
                    break;
                }
        }

        string BuildSecondsOnly()
        {
            string core = mTTS.FormatSecondsForTTS(_lastTimeSec, timeReadStyle);
            return $"{core}秒";
        }
    }
    #endregion

    #region 文本格式化（节拍换算：半开区间 + 微小偏移）
    /// <summary>计算公共量：拍长、总拍索引（0-based）、小节索引（0-based）、小节内拍索引（0-based）。</summary>
    private bool TryCalcBeatPosition(float seconds,
                                     out float beatDuration,
                                     out int totalBeatIndex,
                                     out int measureIndex,
                                     out int beatInMeasure)
    {
        beatDuration = 0f;
        totalBeatIndex = measureIndex = beatInMeasure = 0;

        var model = this.GetModel<AudioEditModel>();
        if (model == null || model.BPM <= 0 || model.BeatA <= 0 || model.BeatB <= 0) return false;

        beatDuration = 60f / model.BPM * (4f / model.BeatB);
        if (beatDuration <= 0f) return false;

        // 半开区间：[k*beat, (k+1)*beat)，在边界处向右推一个极小正数，归到下一拍/下一小节
        float t = Mathf.Max(0f, seconds + Mathf.Max(0f, boundaryEpsilonSec));

        totalBeatIndex = Mathf.FloorToInt(t / beatDuration); // 0-based
        measureIndex = totalBeatIndex / model.BeatA;       // 0-based
        beatInMeasure = totalBeatIndex % model.BeatA;       // 0-based
        return true;
    }

    /// <summary>绝对拍序（从 1 开始、不分小节）：例“第37拍”。</summary>
    private string FormatBeatAbsoluteText(float seconds, out bool ok)
    {
        ok = TryCalcBeatPosition(seconds, out var beatDur, out var totalBeatIndex, out _, out _);
        if (!ok) return string.Empty;
        long n = (long)totalBeatIndex + 1; // 1-based
        return $"第{n}拍";
    }

    /// <summary>只读小节：例“第9小节”。</summary>
    private string FormatMeasureOnlyText(float seconds, out bool ok)
    {
        ok = TryCalcBeatPosition(seconds, out var beatDur, out var totalBeatIndex, out var measureIndex, out _);
        if (!ok) return string.Empty;
        long m = (long)measureIndex + 1; // 1-based
        return $"第{m}小节";
    }

    /// <summary>小节 + 拍：例“第1小节第1拍”。</summary>
    private string FormatMeasureBeatText(float seconds, out bool ok)
    {
        ok = TryCalcBeatPosition(seconds, out var beatDur, out var totalBeatIndex, out var measureIndex, out var beatInMeasure);
        if (!ok) return string.Empty;
        long m = (long)measureIndex + 1;      // 1-based
        long b = (long)beatInMeasure + 1;     // 1-based
        return $"{m}节{b}拍";//第{m}小节第{b}拍
    }
    #endregion

    #region 帧末安全朗读
    private void ScheduleSafeSpeakTime()
    {
        int ver = ++_safeSpeakVer;
        if (safeAtFrameEnd)
            StartCoroutine(CoSpeakTimeAtFrameEnd(ver));
        else
            SpeakTimeNow();
    }

    private IEnumerator CoSpeakTimeAtFrameEnd(int ver)
    {
        yield return new WaitForEndOfFrame();
        if (ver != _safeSpeakVer) yield break;
        SpeakTimeNow();
    }
    #endregion

    #region 拍号下拉：可选 → 写回模型并触发重绘事件
    private void RefreshBeatDropdownFromModel()
    {
        var model = this.GetModel<AudioEditModel>();
        if (model == null) return;

        if (_A) _A.SetValueWithoutNotify(Mathf.Clamp(model.BeatA - 1, 0, (_A.options.Count - 1)));
        if (_B)
        {
            int[] bOptions = { 1, 2, 4, 8 };
            int idx = 0;
            for (int i = 0; i < bOptions.Length; i++)
                if (bOptions[i] == model.BeatB) { idx = i; break; }
            _B.SetValueWithoutNotify(Mathf.Clamp(idx, 0, (_B.options.Count - 1)));
        }
    }

    private void OnBeatDropdownChanged()
    {
        var model = this.GetModel<AudioEditModel>();
        if (model == null) return;

        int aValue = _A ? (_A.value + 1) : model.BeatA; // A 显示 1~6
        int[] bOptions = { 1, 2, 4, 8 };
        int bValue = (_B ? bOptions[Mathf.Clamp(_B.value, 0, bOptions.Length - 1)] : model.BeatB);

        model.BeatA = aValue;
        model.BeatB = bValue;

        // 触发外部重绘（刻度、网格等）
        this.SendEvent(new BPMChangeValue { BPM = model.BPM });
    }
    #endregion
}
