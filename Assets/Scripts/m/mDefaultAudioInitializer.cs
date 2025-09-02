using Qf.Events;
using Qf.Models.AudioEdit;
using Qf.Querys.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

// 统一的“初始化入口”——所有注入都从这里走
public class mDefaultAudioInitializer : MonoBehaviour, IController
{
    [Header("主音乐与可视化组件（必须在 Inspector 里拖好）")]
    [SerializeField] private AudioSource mainAudioSource;       // 主音源（波形、标尺跟随它）
    [SerializeField] private MonoBehaviour waveformDrawer;      // 波形组件（脚本实例）
    [SerializeField] private MonoBehaviour timelineRuler;       // 标尺/刻度组件（脚本实例）
    [SerializeField] private UIAudioEditTimeHand timeHand;      // 时间指针（有的话拖上）

    [Header("面板/轨道（可选，但强烈建议拖齐）")]
    [SerializeField] private UIMainAttributeSetPanel mainAttrPanel;
    [SerializeField] private mUIDrumsInspectorPanel inspectorPanel;
    [SerializeField] private UIAudioEditDrumsOrbit drumsOrbit;
    [SerializeField] private mBeatSetManager beatSetManager;

    // ========== 对外 API：统一注入入口 ==========
    public void ApplyFromSaveData(AudioSaveData save)
    {
        var model = this.GetModel<AudioEditModel>();
        if (save == null) { Debug.LogError("[DefaultInit] SaveData 为空"); return; }

        // 1) 先写回 BPM / 拍号，并广播一次，驱动 UI 文本等
        if (save.BPM > 0) model.BPM = (int)save.BPM;
        if (save.BeatA > 0) model.BeatA = save.BeatA;
        if (save.BeatB > 0) model.BeatB = save.BeatB;
        this.SendEvent(new BPMChangeValue { BPM = model.BPM });

        // 2) 回填基础参数与时间轴
        model.EditAudioClipVolume.Value = save.EditAudioClipVolume;
        model.TipOffset.Value = save.TipOffset;
        model.TimeOfExistence.Value = save.TimeOfExistence;
        model.ThisTime = save.ThisTime;
        model.TimeLineData = save.TimeLineData;

        // 3) 回填所有音频（通过 Query 载入）
        model.EditAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.EditAudioClip));
        model.DownSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.DownSucceedAudioClip));
        model.UpSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.UpSucceedAudioClip));
        model.LeftSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.LeftSucceedAudioClip));
        model.RightSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.RightSucceedAudioClip));
        model.ClickSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.ClickSucceedAudioClip));
        model.LoseAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.LoseAudioClip));
        model.DefaultAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.DefaultAudioClip));
        model.DownTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.DownTipsAudioClip));
        model.UpTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.UpTipsAudioClip));
        model.LeftTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.LeftTipsAudioClip));
        model.RightTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.RightTipsAudioClip));
        model.ClickTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(save.ClickTipsAudioClip));

        // 4) 播放偏移（仅存不应用）
        model.MainAudioOffset.Value = save.MainAudioOffset;
        model.TipOffsetSwipeUp.Value = save.TipOffsetSwipeUp;
        model.TipOffsetSwipeDown.Value = save.TipOffsetSwipeDown;
        model.TipOffsetSwipeLeft.Value = save.TipOffsetSwipeLeft;
        model.TipOffsetSwipeRight.Value = save.TipOffsetSwipeRight;
        model.TipOffsetClick.Value = save.TipOffsetClick;

        // 5) 统一驱动可视化 & 面板 & 事件
        InjectCommon(model, reason: "ApplyFromSaveData");
    }

    public void ApplyFromSO(mAudioDataSO so)
    {
        var model = this.GetModel<AudioEditModel>();
        if (so == null)
        {
            Debug.LogError("[DefaultInit] mAudioDataSO 为空");
            return;
        }

        // 主/默认/失败 音频
        if (so.MainAudio) model.EditAudioClip = so.MainAudio;
        if (so.DefaultAudio) model.DefaultAudioClip = so.DefaultAudio;
        if (so.FailAudio) model.LoseAudioClip = so.FailAudio;

        // 成功音频（上/下/左/右/点击）
        model.UpSucceedAudioClip = so.SucceedUp;
        model.DownSucceedAudioClip = so.SucceedDown;
        model.LeftSucceedAudioClip = so.SucceedLeft;
        model.RightSucceedAudioClip = so.SucceedRight;
        model.ClickSucceedAudioClip = so.SucceedClick;

        // 提示音（上/下/左/右/点击）
        model.UpTipsAudioClip = so.TipsUp;
        model.DownTipsAudioClip = so.TipsDown;
        model.LeftTipsAudioClip = so.TipsLeft;
        model.RightTipsAudioClip = so.TipsRight;
        model.ClickTipsAudioClip = so.TipsClick;

        // 全局参数
        model.TipOffset.Value = so.TipOffset;
        model.TimeOfExistence.Value = so.TimeOfExistence;

        // 音量
        model.EditAudioClipVolume.Value = Mathf.Clamp01(so.MainAudioVolume);
        model.SucceedAudioVolume.Value = Mathf.Clamp01(so.SucceedVolume);
        model.LoseAudioVolume.Value = Mathf.Clamp01(so.LoseVolume);
        model.DefaultAudioVolume.Value = Mathf.Clamp01(so.DefaultVolume);
        model.PreAdventVolume.Value = Mathf.Clamp01(so.PreAdventVolume);

        // 播放偏移（仅存储，不参与播放）
        model.MainAudioOffset.Value = so.MainAudioOffset;
        model.TipOffsetSwipeUp.Value = so.TipOffsetSwipeUp;
        model.TipOffsetSwipeDown.Value = so.TipOffsetSwipeDown;
        model.TipOffsetSwipeLeft.Value = so.TipOffsetSwipeLeft;
        model.TipOffsetSwipeRight.Value = so.TipOffsetSwipeRight;
        model.TipOffsetClick.Value = so.TipOffsetClick;

        // 可选：初始时间指针
        model.ThisTime = Mathf.Max(0f, so.InitialThisTime);

        // 驱动可视化 & 面板
        InjectCommon(model, reason: "ApplyFromSO");
    }

    // 兼容你原有的“默认初始化按钮”
    public void DoDefaultAudioInit()
    {
        var model = this.GetModel<AudioEditModel>();
        // 若你原先这里会给一批默认值，也可继续保留
        InjectCommon(model, reason: "DoDefaultAudioInit");
    }

    // ========== 内部：把 Model 注入到主音源/波形/标尺/面板/列表 ==========
    private void InjectCommon(AudioEditModel model, string reason)
    {
        if (model == null) { Debug.LogError("[DefaultInit] AudioEditModel 为空"); return; }

        // 1) 主音源
        var clip = model.EditAudioClip != null ? model.EditAudioClip : model.DefaultAudioClip;
        if (mainAudioSource != null)
        {
            mainAudioSource.clip = clip;
            mainAudioSource.time = Mathf.Clamp(model.ThisTime, 0f, clip ? clip.length : 0f);
            mainAudioSource.playOnAwake = false;
        }
        else
        {
            Debug.LogWarning("[DefaultInit] mainAudioSource 未指定：波形/标尺可能不更新。");
        }

        // 2) 波形 —— 用协程确保加载顺序正确
        KickWaveformRedraw(clip);

        // 3) 标尺 —— 立刻重建
        float length = clip ? clip.length : 0f;
        TryInvokeAny(timelineRuler, "SetAudioSource", mainAudioSource);
        TryInvokeAny(timelineRuler, "Bind", mainAudioSource);
        TryInvokeAny(timelineRuler, "SetDuration", length);
        TryInvokeAny(timelineRuler, "SetLength", length);
        TryInvokeAny(timelineRuler, "Initialize");
        TryInvokeAny(timelineRuler, "Rebuild");
        TryInvokeAny(timelineRuler, "Refresh");
        TryInvokeAny(timelineRuler, "Redraw");

        // 4) 时间指针（可选居中）
        if (timeHand != null) timeHand.SetTime(model.ThisTime, true);

        // 5) 让面板/轨道按“面板设置音频”的方式走一次
        this.SendEvent<OnUpdateAudioEditDrumsUI>();
        this.SendEvent<AudioEditModelLoad>();
        inspectorPanel?.EnsureReadyAndRefresh();
        drumsOrbit?.ClearAllDrwmsUI();
        beatSetManager?.Init(model);
        mainAttrPanel?.SendEvent(new AudioEditModelLoad());

        // 6) 关键补丁：下一帧做“与面板设置后等价”的二次重建 + 布局刷新
        StartCoroutine(PostInitFullRebuild());

        Debug.Log($"[DefaultInit] 注入完成 ← {reason}");
        this.SendEvent(new OnDefaultInjectionDone());
    }
    public struct OnDefaultInjectionDone { }

    // 二次重建：等一帧，保证各面板/标尺完全生成后，再统一刷新
    private IEnumerator PostInitFullRebuild()
    {
        yield return null; // 等一帧：让 UIDrawAScale/波形把子物体建好

        // 再刷新一次标尺/时间轴（等价于你在面板上重新设置音频后做的事）
        TryInvokeAny(timelineRuler, "Refresh");
        TryInvokeAny(timelineRuler, "Redraw");

        // 强制刷新 ScrollRect/Scrollbar 几何，修正初始手柄长度与可滚动范围
        var sr = GetComponentInChildren<ScrollRect>(true);
        if (sr != null)
        {
            if (sr.content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content);
            if (sr.viewport != null) LayoutRebuilder.ForceRebuildLayoutImmediate(sr.viewport);
            Canvas.ForceUpdateCanvases();
        }
    }

    // 反射式“尽量调用”，兼容不同组件API命名
    private static void TryInvokeAny(object target, string methodName, object arg = null)
    {
        if (target == null) return;
        var t = target.GetType();

        System.Reflection.MethodInfo mi = null;

        if (arg == null)
        {
            mi = t.GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (mi != null) { mi.Invoke(target, null); return; }
        }
        else
        {
            var argType = arg.GetType();
            mi = t.GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new[] { argType }, null);
            if (mi != null) { mi.Invoke(target, new[] { arg }); return; }

            if (arg is AudioSource)
            {
                mi = t.GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(AudioSource) }, null);
                if (mi != null) { mi.Invoke(target, new[] { arg }); return; }
            }
            if (arg is AudioClip)
            {
                mi = t.GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(AudioClip) }, null);
                if (mi != null) { mi.Invoke(target, new[] { arg }); return; }
            }
            if (arg is float)
            {
                mi = t.GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
                if (mi != null) { mi.Invoke(target, new object[] { arg }); return; }
            }
        }
    }

    private Coroutine waveformRoutine;

    private void KickWaveformRedraw(AudioClip clip)
    {
        if (waveformRoutine != null) StopCoroutine(waveformRoutine);
        waveformRoutine = StartCoroutine(RebindWaveformRoutine(clip));
    }

    private IEnumerator RebindWaveformRoutine(AudioClip clip)
    {
        // 1) 等待 AudioClip 真正可用
        if (clip != null)
        {
            // 等到 LoadState=Loaded（避免 Streaming/异步未就绪）
            var start = Time.realtimeSinceStartup;
            while (clip.loadState == AudioDataLoadState.Loading)
            {
                if (Time.realtimeSinceStartup - start > 3f) break; // 最多等3秒，避免卡死
                yield return null;
            }
        }

        // 2) 强制确保波形组件已激活
        if (waveformDrawer != null && !waveformDrawer.gameObject.activeInHierarchy)
            waveformDrawer.gameObject.SetActive(true);

        // 3) 强顺序调用一遍（先绑定源/Clip，再初始化，再绘制）
        TryInvokeAny(waveformDrawer, "SetAudioSource", mainAudioSource);
        TryInvokeAny(waveformDrawer, "SetSource", mainAudioSource);
        TryInvokeAny(waveformDrawer, "Bind", mainAudioSource);
        TryInvokeAny(waveformDrawer, "SetClip", clip);
        TryInvokeAny(waveformDrawer, "Initialize");
        TryInvokeAny(waveformDrawer, "Init");
        TryInvokeAny(waveformDrawer, "Render");
        TryInvokeAny(waveformDrawer, "Refresh");
        TryInvokeAny(waveformDrawer, "Redraw");
        TryInvokeAny(waveformDrawer, "Rebuild");

        // 4) 再等一帧，UI/RectTransform 尺寸稳定后补一次
        yield return null;
        TryInvokeAny(waveformDrawer, "Render");
        TryInvokeAny(waveformDrawer, "Refresh");
        TryInvokeAny(waveformDrawer, "Redraw");
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
}
