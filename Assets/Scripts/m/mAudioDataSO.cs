using UnityEngine;

[CreateAssetMenu(fileName = "mAudioDataSO", menuName = "AudioEdit/音频数据配置组", order = 0)]
public class mAudioDataSO : ScriptableObject
{
    [Header("主/默认/失败 音频")]
    public AudioClip MainAudio;      // 主音乐
    public AudioClip DefaultAudio;   // 默认音乐（无主音乐时兜底）
    public AudioClip FailAudio;      // 失败音频

    [Header("成功音频（上/下/左/右/点击）")]
    public AudioClip SucceedUp;
    public AudioClip SucceedDown;
    public AudioClip SucceedLeft;
    public AudioClip SucceedRight;
    public AudioClip SucceedClick;

    [Header("提示音（上/下/左/右/点击）")]
    public AudioClip TipsUp;
    public AudioClip TipsDown;
    public AudioClip TipsLeft;
    public AudioClip TipsRight;
    public AudioClip TipsClick;

    [Header("全局参数")]
    [Tooltip("全局提示音预告偏移（秒）")]
    public float TipOffset = 0f;

    [Tooltip("鼓点存在时间（秒）")]
    public float TimeOfExistence = 0f;

    [Header("音量（0~1）")]
    public float MainAudioVolume = 1f;
    public float SucceedVolume = 1f;
    public float LoseVolume = 1f;
    public float DefaultVolume = 1f;
    public float PreAdventVolume = 1f;

    [Header("播放偏移（仅存储，不参与播放）")]
    public float MainAudioOffset = 0f;  // 主音乐偏移
    public float TipOffsetSwipeUp = 0f;
    public float TipOffsetSwipeDown = 0f;
    public float TipOffsetSwipeLeft = 0f;
    public float TipOffsetSwipeRight = 0f;
    public float TipOffsetClick = 0f;

    [Header("可选：初始时间指针位置")]
    public float InitialThisTime = 0f;

    [Header("元信息")]
    public int DataVersion = 1;
}
