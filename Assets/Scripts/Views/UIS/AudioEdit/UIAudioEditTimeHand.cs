using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Querys.AudioEdit;
using QFramework;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAudioEditTimeHand : MonoBehaviour, IController, IPointerClickHandler
{
    [SerializeField] RectTransform TimeHand;
    [SerializeField] TMP_Text ShowTimes;
    [SerializeField] ScrollRect ScrollRect;

    // 视觉与时间换算
    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;
    int _EditHeight = AudioEditConfig.EditHeight;

    // ===== 新增：播放时的自动跟随设置 =====
    [Header("CtrlRun 自动跟随")]
    [Tooltip("播放(CtrlRun)状态时，时间针是否始终保持在视窗中间（或指定锚点）")]
    [SerializeField] private bool followDuringCtrlRun = true;

    [Tooltip("视窗锚点（0=左边缘，0.5=正中，1=右边缘）")]
    [Range(0f, 1f)]
    [SerializeField] private float followAnchor = 0.5f;

    [Tooltip("滚动到目标位置所用时长（秒）")]
    [SerializeField] private float followLerpDuration = 0.01f;

    // 内部插值用
    private float? targetScrollValue = null;
    private float scrollLerpSpeed => 1f / Mathf.Max(0.001f, followLerpDuration);

    public Vector2 TimeHandPos
    {
        get => TimeHand.anchoredPosition;
        set => TimeHand.anchoredPosition = value;
    }

    private void Start()
    {
        // 收到 ThisTime 事件时，更新时间针与显示；如在播放则自动居中跟随
        this.RegisterEvent<OnUpdateThisTime>(v =>
        {
            // 更新 UI
            TimeHand.anchoredPosition = new Vector2(v.ThisTime * _PixelUnitsPerSecond, 0);
            ShowTimes.text = v.ThisTime.ToString("0.00");

            // 播放状态下，自动让时间针保持在视窗锚点位置（默认居中）
            var mgr = Qf.Managers.AudioEditManager.Instance;
            bool ctrlRun = (mgr != null && mgr.IsControlRunning);
            if (ctrlRun && followDuringCtrlRun)
            {
                FollowScrollToCenter(v.ThisTime);
            }
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

        this.RegisterEvent<MainAudioChangeValue>(v =>
        {
            UpdateThisTime();
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnEnable()
    {
        StartCoroutine(DelayedSendThisTime());
    }

    private IEnumerator DelayedSendThisTime()
    {
        yield return null;
        this.SendEvent<OnUpdateThisTime>();
    }

    // 根据 Model 的 ThisTime 刷新一次显示
    void UpdateThisTime()
    {
        float newTime = this.SendQuery(new QueryAudioEditAudioClipThisTime());
        TimeHand.anchoredPosition = new Vector2(newTime * _PixelUnitsPerSecond, 0);
        ShowTimes.text = newTime.ToString("0.00");
    }

    // 模拟长按步进（原有）
    int mode = 0;
    float Speed;
    float PressTime;

    private void Update()
    {
        // 长按自动步进（原有逻辑）
        if (mode == 0)
        {
            PressTime = 0;
        }
        else if (mode == 1)
        {
            if (PressTime >= 0.5f) AddTime(Speed);
            else PressTime += Time.deltaTime;
        }
        else if (mode == 2)
        {
            if (PressTime >= 0.5f) RemoveTime(Speed);
            else PressTime += Time.deltaTime;
        }

        // 平滑滚动到目标位置
        if (targetScrollValue.HasValue && ScrollRect && ScrollRect.horizontalScrollbar)
        {
            float current = ScrollRect.horizontalScrollbar.value;
            float target = targetScrollValue.Value;
            float delta = Time.deltaTime * scrollLerpSpeed;
            float newValue = Mathf.MoveTowards(current, target, delta);
            ScrollRect.horizontalScrollbar.value = newValue;

            if (Mathf.Approximately(newValue, target))
                targetScrollValue = null; // 插值完成
        }
    }

    public void AddTimeMode(float Speed) { mode = 1; this.Speed = Speed; }
    public void StopTimeMode(float Speed) { mode = 0; this.Speed = Speed; }
    public void RemoveTimeMode(float Speed) { mode = 2; this.Speed = Speed; }

    /// <summary>外部设置时间（不区分播放/暂停）。</summary>
    public void SetTime(float time)
    {
        this.SendCommand(new SetAudioEditThisTimeCommand(time));

        var audioMgr = Qf.Managers.AudioEditManager.Instance;
        if (audioMgr != null && audioMgr.audioSource != null)
        {
            audioMgr.audioSource.time = time;
            // 关键补丁：同步 lastThisTime，避免下一帧被“回填事件”干扰
            audioMgr.SetLastThisTime(time);
        }
        QFramework.TypeEventSystem.Global.Send(new OnUpdateThisTime() { ThisTime = time });
    }

    public void SetZero()
    {
        float t = 0f;
        SetTime(t);

        // 时间为 0 时，滚动条归位
        if (ScrollRect && ScrollRect.horizontalScrollbar)
        {
            ScrollRect.horizontalScrollbar.value = 0f;
            targetScrollValue = null;
        }
    }

    /// <summary>外部设置时间，并可选择让视窗跟随（用于非播放态的点进/微调）。</summary>
    public void SetTime(float time, bool isFollow = false)
    {
        // 强制两位小数
        float fixedTime = (float)Math.Round(time, 2, MidpointRounding.ToEven);
        this.SendCommand(new SetAudioEditThisTimeCommand(fixedTime));

        if (ScrollRect)
        {
            if (Mathf.Approximately(fixedTime, 0f))
            {
                if (ScrollRect.horizontalScrollbar)
                    ScrollRect.horizontalScrollbar.value = 0f;
                targetScrollValue = null;
            }
            else if (isFollow)
            {
                FollowScrollToCenter(fixedTime);
            }
        }
    }

    /// <summary>让当前时间在视窗中保持在指定锚点（默认居中）。</summary>
    private void FollowScrollToCenter(float timeSec)
    {
        if (!ScrollRect) return;

        // 视图与内容尺寸
        float viewWidth = ScrollRect.viewport.rect.width;
        float contentWidth = ScrollRect.content.rect.width;

        // 内容比视窗还窄：无需滚动
        if (contentWidth <= viewWidth || viewWidth <= 0f || contentWidth <= 0f)
        {
            if (ScrollRect.horizontalScrollbar)
                ScrollRect.horizontalScrollbar.value = 0f;
            targetScrollValue = null;
            return;
        }

        // 目标像素位置（让时间针位于视窗锚点）
        float handPixelX = timeSec * _PixelUnitsPerSecond;
        float anchorPixel = handPixelX - viewWidth * Mathf.Clamp01(followAnchor);

        // 映射到 0..1 的滚动值并裁剪
        float maxScrollable = contentWidth - viewWidth;
        float scrollValue = Mathf.Clamp01(anchorPixel / maxScrollable);

        // 设定插值目标（Update 中平滑过去）
        targetScrollValue = scrollValue;
    }

    // ===== 原有的步进/点击处理 =====
    public void AddTime(float Speed)
    {
        this.SendCommand(
            new SetAudioEditThisTimeCommand(
                this.SendQuery(new QueryAudioEditAudioClipThisTime()) + 0.01f * Speed));

        if (ScrollRect && ScrollRect.horizontalScrollbar)
        {
            ScrollRect.horizontalScrollbar.value =
                (TimeHand.anchoredPosition.x / _PixelUnitsPerSecond) /
                this.SendQuery(new QueryAudioEditAudioClipLength());
        }
    }

    public void RemoveTime(float Speed)
    {
        this.SendCommand(
            new SetAudioEditThisTimeCommand(
                this.SendQuery(new QueryAudioEditAudioClipThisTime()) - 0.01f * Speed));

        if (ScrollRect && ScrollRect.horizontalScrollbar)
        {
            ScrollRect.horizontalScrollbar.value =
                (TimeHand.anchoredPosition.x / _PixelUnitsPerSecond) /
                this.SendQuery(new QueryAudioEditAudioClipLength());
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        FindObjectOfType<CreateDrumsManager>()?.ResetAllActiveCodes(); // 2025/06/10 - mixyao
        TimeHand.position = eventData.position;
        this.SendCommand(new SetAudioEditThisTimeCommand((TimeHand.anchoredPosition.x) / _PixelUnitsPerSecond));
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;

    /// <summary>时间针前移0.01（非播放时常用），并让视窗跟随。</summary>
    public void MoveForward_0_01()
    {
        float cur = this.SendQuery(new QueryAudioEditAudioClipThisTime());
        SetTime(cur + 0.01f, isFollow: true);
    }

    /// <summary>时间针后移0.01（非播放时常用），并让视窗跟随。</summary>
    public void MoveBackward_0_01()
    {
        float cur = this.SendQuery(new QueryAudioEditAudioClipThisTime());
        SetTime(cur - 0.01f, isFollow: true);
    }
}
