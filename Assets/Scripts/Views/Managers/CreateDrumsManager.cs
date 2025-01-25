using MoonSharp.VsCodeDebugger.SDK;
using Qf.ClassDatas.AudioEdit;
using Qf.Events;
using Qf.Managers;
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// ���������ڹ���ĵ�ʵ���(��ͨ����ʽ����ȥ���ùĵ�����)
/// </summary>
public class CreateDrumsManager : ManagerBase
{
    [SerializeField]
    AudioSource audioSource;
    AudioEditModel editModel;
    List<InputMode> gameObjects = new();
    public override void Init()
    {
        editModel = this.GetModel<AudioEditModel>();
        CreateSetClass.Instance = new CreateSetClass(audioSource);
        this.RegisterEvent<OnUpdateThisTime>(v =>
        {
            if (editModel.TimeLineData.ContainsKey(v.ThisTime))
            {
                foreach (var i in editModel.TimeLineData[v.ThisTime])
                {
                    //����Ҫ����һ������(��Ч��ʾ,ʱ����Ӱ��ĵ�ı��ֳ���(����ʱ�����ϵĹĵ㳤��))
                    gameObjects.Add(CreateDrums(i.DrwmsData.theTypeOfOperation).GetInputMode());
                }
            }
            else if (!editModel.Mode.Equals(SystemModeData.PlayMode))
            {
                //ͬ��һ������ʾ����,���ﴦ������뿪��Χ��رն�Ӧ��ʾ�Ĺĵ�
                foreach (var j in gameObjects)
                {
                    if(j != null)
                    Destroy(j.gameObject);
                }
                gameObjects.Clear();
            }

        }).UnRegisterWhenGameObjectDestroyed(gameObject);
        Debug.Log("CreateDrumsManager �Ѽ���...");
    }
    /// <summary>
    /// ��ӹĵ�ʵ��
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="vector3"></param>
    public CreateSetClass CreateDrums(TheTypeOfOperation operation, Vector3 vector3 = default,DrumsLoadData drumsLoadData = null)
    {
        GameObject gameObject = Instantiate(Resources.Load<GameObject>(PathConfig.ProfabsOath + "InputMode"));
        InputMode mode = gameObject.GetComponent<InputMode>();
        CreateSetClass.Instance.SetInputMode(mode);
        mode.SetOperation(operation);
        if (vector3.Equals(default))
            mode.transform.position = new Vector3(0, 0, 0);
        // gameObject.transform.position = vector3;
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
                if (instance == null)
                    instance = new CreateSetClass();
                return instance;
            }
            set
            {
                if (value != null)
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
        public InputMode GetInputMode()
        {
            return _Mode;
        }
        /// <summary>
        /// ���ô����ɹ���Ч(��Ч���ӳ�ʱ��)
        /// </summary>
        public void SetSuccessSounds(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            if (Clip != null)
                _Mode.SuccessClip = Clip;
            SetCpVector(channelPosition);
        }
        /// <summary>
        /// ��������ǰ��Ч(��Ч���ӳ�ʱ��)
        /// </summary>
        public void SetPreAdventSound(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            if (Clip != null)
                _Mode.PreAdventClip = Clip;
            _Mode.DrwmsData.DrwmsData.PreAdventAudioClipOffsetTime = DelayTime;
            SetCpVector(channelPosition);
        }
        /// <summary>
        /// ����ʧ����Ч(��Ч���ӳ�ʱ��)
        /// </summary>
        public void SetFailureSound(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            if (Clip != null)
                _Mode.FailClip = Clip;
            SetCpVector(channelPosition);
        }
        void SetCpVector(ChannelPosition channelPosition)
        {
            switch (channelPosition)
            {
                case ChannelPosition.FullChannel:
                    _AudioSource.panStereo = 0;
                    break;
                case ChannelPosition.LeftChannel:
                    _AudioSource.panStereo = -1;
                    break;
                case ChannelPosition.RightChannel:
                    _AudioSource.panStereo = 1;
                    break;
                default:
                    break;
            }
        }
    }

    public enum ChannelPosition //����λ��
    {
        LeftChannel,
        RightChannel,
        FullChannel
    }
}
