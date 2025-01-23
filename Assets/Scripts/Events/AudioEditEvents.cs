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
    public struct OnUpdateThisTime {
        public float ThisTime;
    };
    /// <summary>
    /// ���¹ĵ�UI
    /// </summary>
    public struct OnUpdateAudioEditDrumsUI { };
    public struct BPMChangeValue
    {
        public int BPM;
    }
    /// <summary>
    /// ����Ƶ�ı�
    /// </summary>
    public struct MainAudioChangeValue {
        public string Name;
        public float Length;
    };
    /// <summary>
    /// ѡ����Ƶ
    /// </summary>
    public struct SelectOptions
    {
        public object SelectValue;
        public GameObject SelectObject;
    }
}
