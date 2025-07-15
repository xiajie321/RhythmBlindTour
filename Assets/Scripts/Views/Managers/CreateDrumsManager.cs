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
    [SerializeField] private Transform inputModeParent;     // InputModePoint
    [SerializeField] private Transform judgeLineTransform;  // TargetLine

    private AudioEditModel editModel;
    private DataCachingModel cachingModel;
    private List<InputMode> gameObjects = new();
    private HashSet<string> activeDrumCodes = new();  // 按 DrumCode 去重
    private float lastUpdateTime = -1f;               // 上一帧的 ThisTime

    public IReadOnlyList<InputMode> ActiveInputModes => gameObjects;

    public override void Init()
    {
        editModel = this.GetModel<AudioEditModel>();
        cachingModel = this.GetModel<DataCachingModel>();
        CreateSetClass.Instance = new CreateSetClass(audioSource);

        this.RegisterEvent<OnUpdateThisTime>(v =>
        {
            float now = v.ThisTime;

            // 原 PlayMode 流程（遍历所有鼓点）
            if (editModel.Mode.Equals(SystemModeData.PlayMode) && editModel.TimeLineData != null)
            {
                foreach (var kvp in editModel.TimeLineData)
                {
                    float centerTime = kvp.Key;
                    foreach (var data in kvp.Value)
                    {
                        string code = data.DrwmsData.DrumCode;
                        float preOffset = data.DrwmsData.VPreAdventAudioClipOffsetTime;
                        float preAdventTime = centerTime - preOffset;

                        // === 新增 ===
                        // 如果是只发声鼓点（判据：存在时间和预告音偏移都为0）
                        if (Mathf.Approximately(data.DrwmsData.VTimeOfExistence, 0f) &&
                            Mathf.Approximately(data.DrwmsData.VPreAdventAudioClipOffsetTime, 0f))
                        {
                            // 到达中心时间时才播放一次成功音效（避免重复播放）
                            if (centerTime > lastUpdateTime && centerTime <= now && !activeDrumCodes.Contains(code))
                            {
                                var clip = cachingModel.GetAudioClip(data.DrwmsData.FSucceedAudioClipPath);
                                if (clip != null) audioSource.PlayOneShot(clip, data.MusicData.SSucceedVolume);
                                activeDrumCodes.Add(code);
                            }
                            // 不创建 InputMode，不判定、不播放提示音
                            continue;
                        }
                        // === 结束新增 ===

                        // 原有区间判定、创建 InputMode、播放预告音/正确音
                        if (preAdventTime > lastUpdateTime
                            && preAdventTime <= now
                            && !activeDrumCodes.Contains(code))
                        {
                            var inputMode = CreateDrums(data.DrwmsData.DtheTypeOfOperation, data)
                                                .GetInputMode();
                            gameObjects.Add(inputMode);
                            activeDrumCodes.Add(code);

                            var tipClip = cachingModel.GetAudioClip(data.DrwmsData.FPreAdventAudioClipPath);
                            if (tipClip != null)
                                AudioEditManager.Instance
                                    .PlayVFXWithFallback(data.DrwmsData.DtheTypeOfOperation, tipClip, data.MusicData.SPreAdventVolume);

                        }
                    }
                }
            }

            else
            {
                // —— 原有编辑/清理逻辑不变 —— 
                if (!AudioEditManager.Instance.IsControlRunning) return;

                // 编辑时播放逻辑
                if (editModel.TimeLineData != null)
                {
                    foreach (var kvp in editModel.TimeLineData)
                    {
                        float centerTime = kvp.Key;
                        foreach (var data in kvp.Value)
                        {
                            float preAdventTime = centerTime - data.DrwmsData.VPreAdventAudioClipOffsetTime;
                            if (preAdventTime > lastUpdateTime && preAdventTime <= now)
                            {
                                AudioEditManager.Instance
                                    .PlayVFXWithFallback(data.DrwmsData.DtheTypeOfOperation,
                                                         cachingModel.GetAudioClip(data.DrwmsData.FPreAdventAudioClipPath),
                                                         1f);
                            }
                            if (centerTime > lastUpdateTime && centerTime <= now)
                            {
                                var clip = cachingModel.GetAudioClip(data.DrwmsData.FSucceedAudioClipPath);
                                if (clip != null) audioSource.PlayOneShot(clip, data.MusicData.SSucceedVolume);
                            }
                        }
                    }
                }

                // 清理超出或未到的 InputMode
                var toRemove = new List<InputMode>();
                foreach (var mode in gameObjects)
                {
                    if (mode != null && (now > mode.EndTime || now < mode.StartTime))
                    {
                        toRemove.Add(mode);
                        Destroy(mode.gameObject);
                        activeDrumCodes.Remove(mode.DrwmsData.DrwmsData.DrumCode);
                    }
                }
                foreach (var dead in toRemove)
                    gameObjects.Remove(dead);
            }

            // 更新 lastUpdateTime
            lastUpdateTime = now;
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

        Debug.Log("CreateDrumsManager initialized...");
    }

    public void ResetAllActiveCodes()
    {
        activeDrumCodes.Clear();
    }

    public CreateSetClass CreateDrums(TheTypeOfOperation operation, DrumsLoadData drumsLoadData = null)
    {
        GameObject go = Instantiate(Resources.Load<GameObject>(PathConfig.ProfabsOath + "InputMode"));
        if (inputModeParent != null)
            go.transform.SetParent(inputModeParent, false);

        InputMode mode = go.GetComponent<InputMode>();

        // 第一次创建时填充 CenterTime
        if (drumsLoadData != null && drumsLoadData.DrwmsData.CenterTime == 0f)
            drumsLoadData.DrwmsData.CenterTime = editModel.ThisTime;

        float centerTime = drumsLoadData.DrwmsData.CenterTime;
        float existence = drumsLoadData.DrwmsData.VTimeOfExistence;
        float preOffset = drumsLoadData.DrwmsData.VPreAdventAudioClipOffsetTime;
        float startTime = centerTime - existence / 2f;
        float endTime = centerTime + existence / 2f;
        float preAdvent = centerTime - preOffset;

        mode.InitializeTimes(preAdvent, startTime, endTime);
        mode.SetIsDemoInputMode(Mathf.Approximately(existence, 0f));
        mode.DrwmsData = drumsLoadData;
        mode.SetOperation(operation);
        mode.PreAdventClip = cachingModel.GetAudioClip(drumsLoadData.DrwmsData.FPreAdventAudioClipPath);
        mode.LoseClip = cachingModel.GetAudioClip(drumsLoadData.DrwmsData.FLoseAudioClipPath);
        mode.SuccessClip = cachingModel.GetAudioClip(drumsLoadData.DrwmsData.FSucceedAudioClipPath);

        var visualController = go.GetComponent<mInputModeVisualController>();
        if (visualController != null && judgeLineTransform != null)
            visualController.judgeLineTarget = judgeLineTransform;

        CreateSetClass.Instance.SetInputMode(mode);

        this.SendEvent(new DrumsGenerate { InputMode = mode });

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
