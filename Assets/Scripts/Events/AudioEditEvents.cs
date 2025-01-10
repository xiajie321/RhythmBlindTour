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
    public struct UpdateThisTimeEvent { };
    /// <summary>
    /// 更新鼓点UI
    /// </summary>
    public struct OnUpdateAudioEditDrumsUI { };
    /// <summary>
    /// 编辑器音频文件更新通知
    /// </summary>
    public struct AudioEditChangeEvent
    {
        public AudioClip clip;
    }
}
