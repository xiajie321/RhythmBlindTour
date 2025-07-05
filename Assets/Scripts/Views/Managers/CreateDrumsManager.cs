using MoonSharp.VsCodeDebugger.SDK;
using Qf.ClassDatas.AudioEdit;
using Qf.Events;
using Qf.Managers;
using Qf.Models;
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateDrumsManager : ManagerBase
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform inputModeParent;     // InputModePoint // 添加InputMode的“水平移动” -mixyao/06/19
    [SerializeField] private Transform judgeLineTransform;  // TargetLine // 添加InputMode的“水平移动” -mixyao/06/19

    private AudioEditModel editModel;
    private DataCachingModel cachingModel;
    private List<InputMode> gameObjects = new();
    private HashSet<float> activeDrumCenters = new();

    public IReadOnlyList<InputMode> ActiveInputModes => gameObjects;

    public override void Init()
    {
        editModel = this.GetModel<AudioEditModel>();
        cachingModel = this.GetModel<DataCachingModel>();
        CreateSetClass.Instance = new CreateSetClass(audioSource);

        this.RegisterEvent<OnUpdateThisTime>(v =>
        {
            // PlayMode 下播放预告音和生成鼓点
            if (editModel.Mode.Equals(SystemModeData.PlayMode))
            {
                if (editModel.TipsAudio.ContainsKey(v.ThisTime))
                {
                    AudioEditManager.Instance.Play(editModel.TipsAudio[v.ThisTime].ToArray(), editModel.TipsVolume[v.ThisTime].ToArray());
                }

                if (editModel.TimeLineData != null)
                {
                    foreach (var kvp in editModel.TimeLineData)
                    {
                        float centerTime = kvp.Key;

                        foreach (var data in kvp.Value)
                        {
                            float existence = data.DrwmsData.VTimeOfExistence;
                            float preAdventOffset = data.DrwmsData.VPreAdventAudioClipOffsetTime;
                            float preAdventTime = centerTime - preAdventOffset;

                            if (v.ThisTime >= preAdventTime && !activeDrumCenters.Contains(centerTime))
                            {
                                var inputMode = CreateDrums(data.DrwmsData.DtheTypeOfOperation, data).GetInputMode();
                                gameObjects.Add(inputMode);
                                activeDrumCenters.Add(centerTime);
                            }
                        }
                    }
                }
            }

            // 非播放模式：试听预告音和主音
            else
            {
                if (editModel.TimeLineData != null)
                {
                    foreach (var kvp in editModel.TimeLineData)
                    {
                        float centerTime = kvp.Key;

                        foreach (var data in kvp.Value)
                        {
                            float preAdventTime = centerTime - data.DrwmsData.VPreAdventAudioClipOffsetTime;

                            if (Mathf.Approximately(v.ThisTime, preAdventTime))
                            {
                                var clip = cachingModel.GetAudioClip(data.DrwmsData.FPreAdventAudioClipPath);
                                if (clip != null) audioSource.PlayOneShot(clip);
                            }

                            if (Mathf.Approximately(v.ThisTime, centerTime))
                            {
                                var clip = cachingModel.GetAudioClip(data.DrwmsData.FSucceedAudioClipPath);
                                if (clip != null) audioSource.PlayOneShot(clip);
                            }


                        }
                    }
                }

                // 编辑模式鼓点清理
                List<InputMode> toRemove = new();

                foreach (var j in gameObjects)
                {
                    if (j != null && (v.ThisTime > j.EndTime || v.ThisTime < j.StartTime))
                    {
                        toRemove.Add(j);
                        Destroy(j.gameObject);
                        activeDrumCenters.Remove(j.DrwmsData.DrwmsData.CenterTime);
                    }
                }

                foreach (var e in toRemove)
                {
                    gameObjects.Remove(e);
                }
            }

        }).UnRegisterWhenGameObjectDestroyed(gameObject);

        Debug.Log("CreateDrumsManager initialized...");
    }

    public void ResetAllActiveCenters()
    {
        activeDrumCenters.Clear();
    }

    public CreateSetClass CreateDrums(TheTypeOfOperation operation, DrumsLoadData drumsLoadData = null)
    {
        GameObject gameObject = Instantiate(Resources.Load<GameObject>(PathConfig.ProfabsOath + "InputMode"));

        if (inputModeParent != null)
            gameObject.transform.SetParent(inputModeParent, worldPositionStays: false);

        InputMode mode = gameObject.GetComponent<InputMode>();

        if (drumsLoadData != null && drumsLoadData.DrwmsData.CenterTime == 0f)
        {
            drumsLoadData.DrwmsData.CenterTime = this.GetModel<AudioEditModel>().ThisTime;
        }

        float centerTime = drumsLoadData.DrwmsData.CenterTime;
        float existence = drumsLoadData.DrwmsData.VTimeOfExistence;
        float preOffset = drumsLoadData.DrwmsData.VPreAdventAudioClipOffsetTime;

        float startTime = centerTime - existence / 2f;
        float endTime = centerTime + existence / 2f;
        float preAdventTime = centerTime - preOffset;

        mode.InitializeTimes(preAdventTime, startTime, endTime);
        mode.SetIsDemoInputMode(Mathf.Approximately(existence, 0f));
        mode.DrwmsData = drumsLoadData;
        mode.SetOperation(operation);

        mode.PreAdventClip = cachingModel.GetAudioClip(drumsLoadData.DrwmsData.FPreAdventAudioClipPath);
        mode.LoseClip = cachingModel.GetAudioClip(drumsLoadData.DrwmsData.FLoseAudioClipPath);
        mode.SuccessClip = cachingModel.GetAudioClip(drumsLoadData.DrwmsData.FSucceedAudioClipPath);

        var visualController = gameObject.GetComponent<mInputModeVisualController>();
        if (visualController != null && judgeLineTransform != null)
        {
            visualController.judgeLineTarget = judgeLineTransform;
        }

        CreateSetClass.Instance.SetInputMode(mode);

        this.SendEvent(new DrumsGenerate()
        {
            InputMode = mode
        });

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

        public void SetInputMode(InputMode inputMode)
        {
            _Mode = inputMode;
        }

        public InputMode GetInputMode()
        {
            return _Mode;
        }

        public InputMode SetData(DrumsLoadData drumsLoadData)
        {
            _Mode.DrwmsData = drumsLoadData;
            return _Mode;
        }

        public void SetSuccessSounds(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            if (Clip != null)
                _Mode.SuccessClip = Clip;
            SetCpVector(channelPosition);
        }

        public void SetPreAdventSound(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            if (Clip != null)
                _Mode.PreAdventClip = Clip;
            _Mode.DrwmsData.DrwmsData.VPreAdventAudioClipOffsetTime = DelayTime;
            SetCpVector(channelPosition);
        }

        public void SetFailureSound(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            if (Clip != null)
                _Mode.LoseClip = Clip;
            SetCpVector(channelPosition);
        }

        void SetCpVector(ChannelPosition channelPosition)
        {
            switch (channelPosition)
            {
                case ChannelPosition.FullChannel: _AudioSource.panStereo = 0; break;
                case ChannelPosition.LeftChannel: _AudioSource.panStereo = -1; break;
                case ChannelPosition.RightChannel: _AudioSource.panStereo = 1; break;
            }
        }
    }

    public enum ChannelPosition
    {
        LeftChannel,
        RightChannel,
        FullChannel
    }
}
