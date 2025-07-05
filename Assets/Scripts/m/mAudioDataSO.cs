using UnityEngine;

[CreateAssetMenu(fileName = "mAudioDataSO", menuName = "AudioEdit/音频数据配置组", order = 0)]
public class mAudioDataSO : ScriptableObject
{
    public AudioClip MainAudio;
    public AudioClip FailAudio;
    public AudioClip SucceedUp;
    public AudioClip SucceedDown;
    public AudioClip SucceedLeft;
    public AudioClip SucceedRight;
    public AudioClip SucceedClick;

    public AudioClip TipsUp;
    public AudioClip TipsDown;
    public AudioClip TipsLeft;
    public AudioClip TipsRight;
    public AudioClip TipsClick;
}
