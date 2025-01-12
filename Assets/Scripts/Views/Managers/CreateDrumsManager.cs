using MoonSharp.VsCodeDebugger.SDK;
using Qf.ClassDatas.AudioEdit;
using Qf.Events;
using Qf.Managers;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// ���������ڹ���ĵ�ʵ���
/// </summary>
public class CreateDrumsManager : ManagerBase
{
    [SerializeField]
    AudioSource audioSource;
    public override void Init()
    {
        CreateSetClass.Instance = new CreateSetClass(audioSource);
        Debug.Log("CreateDrumsManager �Ѽ���...");
    }
    /// <summary>
    /// ��ӹĵ�ʵ��
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="vector3"></param>
    public CreateSetClass CreateDrums(TheTypeOfOperation operation, Vector3 vector3)
    {
        GameObject gameObject = Instantiate(Resources.Load<GameObject>(PathConfig.ProfabsOath + "InputMode"));
        InputMode mode = gameObject.GetComponent<InputMode>();
        CreateSetClass.Instance.SetInputMode(mode);
        mode.SetOperation(operation);
        gameObject.transform.position = vector3;
        return CreateSetClass.Instance;
    }

    public class CreateSetClass
    {
        AudioSource _AudioSource;
        InputMode _Mode;
        public CreateSetClass() { }
        public CreateSetClass(AudioSource audioSource)
        {
            _AudioSource = audioSource;
        }
        static CreateSetClass instance;
        public static CreateSetClass Instance
        {
            get
            {
                if(instance == null)
                    instance = new CreateSetClass();
                return instance;
            }
            set
            {
                if(value != null)
                    instance = value;
            }
        }
        /// <summary>
        /// ���ñ�Ӱ���InputMode
        /// </summary>
        /// <param name="inputMode"></param>
        public void SetInputMode(InputMode inputMode)
        {
            _Mode = inputMode;
        }
        /// <summary>
        /// ���ô����ɹ���Ч(��Ч���ӳ�ʱ��)
        /// </summary>
        public void SetSuccessSounds(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            _Mode.SuccessClip = Clip;
        }
        /// <summary>
        /// ��������ǰ��Ч(��Ч���ӳ�ʱ��)
        /// </summary>
        public void SetPreAdventSound(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            _Mode.PreAdventClip = Clip;
        }
        /// <summary>
        /// ����ʧ����Ч(��Ч���ӳ�ʱ��)
        /// </summary>
        public void SetFailureSound(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            _Mode.FailClip = Clip;
        }
        
    }

    public enum ChannelPosition //����λ��
    {
        LeftChannel,
        RightChannel,
        FullChannel
    }
}
