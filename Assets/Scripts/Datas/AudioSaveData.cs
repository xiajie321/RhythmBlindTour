using System;
using System.Collections.Generic;
using UnityEngine;
using Qf.ClassDatas.AudioEdit; // DrumsLoadData

/// <summary>
/// 用于关卡编辑器的统一存档载体；字段与 mDefaultAudioInitializer.ApplyFromSaveData 使用到的一一对齐。
/// </summary>
[Serializable]
public class AudioSaveData
{
    // —— 基础信息 —— //
    public float BPM;
    public int BeatA;
    public int BeatB;
    public float ThisTime;

    // —— 全局音量/时序 —— //
    public float EditAudioClipVolume;  // 主音乐音量(0..1)
    public float TipOffset;            // 全局-预告音提前(秒)
    public float TimeOfExistence;      // 全局-判定时长(秒)

    // —— 时间线（按秒） —— //
    public Dictionary<float, List<DrumsLoadData>> TimeLineData;

    // —— 主/默认/失败 —— //
    public string EditAudioClip;       // 主音乐名
    public string DefaultAudioClip;    // 默认音频名
    public string LoseAudioClip;       // 失败音名

    // —— 成功音（上/下/左/右/点击） —— //
    public string UpSucceedAudioClip;
    public string DownSucceedAudioClip;
    public string LeftSucceedAudioClip;
    public string RightSucceedAudioClip;
    public string ClickSucceedAudioClip;

    // —— 提示音（上/下/左/右/点击） —— //
    public string UpTipsAudioClip;
    public string DownTipsAudioClip;
    public string LeftTipsAudioClip;
    public string RightTipsAudioClip;
    public string ClickTipsAudioClip;

    // —— 播放偏移（仅存储，不直接驱动播放） —— //
    public float MainAudioOffset;
    public float TipOffsetSwipeUp;
    public float TipOffsetSwipeDown;
    public float TipOffsetSwipeLeft;
    public float TipOffsetSwipeRight;
    public float TipOffsetClick;
}
