using UnityEngine;
namespace Qf.Events
{
    /// <summary>
    /// ��������ģʽ
    /// </summary>
    public struct OnPlayMode { };
    /// <summary>
    /// �˳�����ģʽ
    /// </summary>
    public struct ExitPlayMode { };
    /// <summary>
    /// ����༭ģʽ
    /// </summary>
    public struct OnEditMode { };
    /// <summary>
    /// �˳��༭ģʽ
    /// </summary>
    public struct ExitEditMode { };
    /// <summary>
    /// ����¼��ģʽ
    /// </summary>
    public struct OnRecordingMode { };
    /// <summary>
    /// �˳�¼��ģʽ
    /// </summary>
    public struct ExitRecordingMode { };
    /// <summary>
    /// ���µ�ǰʱ��UI
    /// </summary>
    public struct UpdateThisTimeEvent { };
    /// <summary>
    /// ���¹ĵ�UI
    /// </summary>
    public struct OnUpdateAudioEditDrumsUI { };
    /// <summary>
    /// �༭����Ƶ�ļ�����֪ͨ
    /// </summary>
    public struct AudioEditChangeEvent
    {
        public AudioClip clip;
    }
}
