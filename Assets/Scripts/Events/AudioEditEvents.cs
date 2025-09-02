using UnityEngine;

namespace Qf.Events
{
    /// <summary>
    /// 进入游玩模式
    /// </summary>
    public struct OnPlayMode { };
    /// <summary>
    /// 退出游玩模式
    /// </summary>
    public struct ExitPlayMode { };
    /// <summary>
    /// 进入编辑模式
    /// </summary>
    public struct OnEditMode { };
    /// <summary>
    /// 退出编辑模式
    /// </summary>
    public struct ExitEditMode { };
    /// <summary>
    /// 进入录制模式
    /// </summary>
    public struct OnRecordingMode { };
    /// <summary>
    /// 退出录制模式
    /// </summary>
    public struct ExitRecordingMode { };
    /// <summary>
    /// 更新当前时间UI
    /// </summary>
    public struct OnUpdateThisTime {
        public float ThisTime;
    };
    /// <summary>
    /// 初始化当前时间
    /// </summary>
    public struct OnStartThisTime
    {
        public float ThisTime;
    }
    /// <summary>
    /// 更新鼓点UI
    /// </summary>
    public struct OnUpdateAudioEditDrumsUI { };
    public struct BPMChangeValue
    {
        public int BPM;
    }
    /// <summary>
    /// 主音频改变
    /// </summary>
    public struct MainAudioChangeValue {
        public string Name;
        public float Length;
    };
    /// <summary>
    /// 选择音频
    /// </summary>
    public struct SelectOptions
    {
        public object SelectValue;
        public GameObject SelectObject;
    }
}
