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

    private readonly List<InputMode> gameObjects = new();
    private readonly HashSet<string> activeDrumCodes = new();   // 已创建/激活的鼓点（按 DrumCode 去重）
    private readonly HashSet<string> playedTipCodes = new();    // 已触发过“提示音”的鼓点（按 DrumCode 去重）

    private float lastUpdateTime = -1f; // 上一帧 ThisTime

    public IReadOnlyList<InputMode> ActiveInputModes => gameObjects;

    public override void Init()
    {
        editModel = this.GetModel<AudioEditModel>();
        cachingModel = this.GetModel<DataCachingModel>();
        CreateSetClass.Instance = new CreateSetClass(audioSource);

        this.RegisterEvent<OnUpdateThisTime>(v =>
        {
            float now = v.ThisTime;
            bool ctrlRun = (AudioEditManager.Instance != null && AudioEditManager.Instance.IsControlRunning);

            // —— PlayMode：生成/判定/播放 —— //
            if (editModel.Mode.Equals(SystemModeData.PlayMode) && editModel.TimeLineData != null)
            {
                foreach (var kvp in editModel.TimeLineData)
                {
                    float centerTime = kvp.Key;

                    foreach (var data in kvp.Value)
                    {
                        string code = data.DrwmsData.DrumCode;

                        float preOffset = data.DrwmsData.VPreAdventAudioClipOffsetTime; // 数据上的“提示音提前”
                        float playOffset = data.DrwmsData.VTipPlayOffset;               // 播放时再提前
                        float preAdventTime = centerTime - preOffset;                    // 仍作为创建 InputMode 的时刻
                        float tipTriggerTime = centerTime - preOffset - (ctrlRun ? playOffset : 0f); // 实际播放提示音的触发点（CtrlRun时生效）

                        // 只发声（存在时间==0 且 预告偏移==0）——保留你的旧分支
                        if (Mathf.Approximately(data.DrwmsData.VTimeOfExistence, 0f) &&
                            Mathf.Approximately(preOffset, 0f))
                        {
                            if (centerTime > lastUpdateTime && centerTime <= now && !activeDrumCodes.Contains(code))
                            {
                                var clip = cachingModel.GetAudioClip(data.DrwmsData.FSucceedAudioClipPath);
                                if (clip != null) audioSource.PlayOneShot(clip, data.MusicData.SSucceedVolume);
                                activeDrumCodes.Add(code);
                                playedTipCodes.Add(code); // 避免后续再误播提示音
                            }
                            continue;
                        }

                        // A) 到达“提示音触发点”就播提示音（与创建解耦；避免晚播）
                        if (tipTriggerTime > lastUpdateTime && tipTriggerTime <= now && !playedTipCodes.Contains(code))
                        {
                            var tipClip = cachingModel.GetAudioClip(data.DrwmsData.FPreAdventAudioClipPath);
                            if (tipClip != null)
                                AudioEditManager.Instance.PlayVFXWithFallback(
                                    data.DrwmsData.DtheTypeOfOperation,
                                    tipClip,
                                    data.MusicData.SPreAdventVolume);
                            playedTipCodes.Add(code);
                        }

                        // B) 到达“预告时间点（数据上的偏移）”才创建 InputMode（维持原判定时序）
                        if (preAdventTime > lastUpdateTime && preAdventTime <= now && !activeDrumCodes.Contains(code))
                        {
                            var inputMode = CreateDrums(data.DrwmsData.DtheTypeOfOperation, data).GetInputMode();
                            gameObjects.Add(inputMode);
                            activeDrumCodes.Add(code);
                            // 注意：此处不再重复播放提示音（避免与 A 重复/晚播）
                        }
                    }
                }
            }
            // —— 其它模式（例如编辑），但仍然在 CtrlRun 下需要“提前播放提示音” —— //
            else
            {
                if (!AudioEditManager.Instance || !AudioEditManager.Instance.IsControlRunning)
                {
                    // 未在 CtrlRun 下不做播放推进
                    lastUpdateTime = now;
                    return;
                }

                if (editModel.TimeLineData != null)
                {
                    foreach (var kvp in editModel.TimeLineData)
                    {
                        float centerTime = kvp.Key;
                        foreach (var data in kvp.Value)
                        {
                            string code = data.DrwmsData.DrumCode;

                            float preOffset = data.DrwmsData.VPreAdventAudioClipOffsetTime;
                            float playOffset = data.DrwmsData.VTipPlayOffset;
                            float tipTriggerTime = centerTime - preOffset - playOffset;

                            // 提示音（用类型音量）
                            if (tipTriggerTime > lastUpdateTime && tipTriggerTime <= now && !playedTipCodes.Contains(code))
                            {
                                AudioEditManager.Instance.PlayVFXWithFallback(
                                    data.DrwmsData.DtheTypeOfOperation,
                                    cachingModel.GetAudioClip(data.DrwmsData.FPreAdventAudioClipPath),
                                    data.MusicData.SPreAdventVolume);
                                playedTipCodes.Add(code);
                            }

                            // 回答/成功音
                            if (centerTime > lastUpdateTime && centerTime <= now && !activeDrumCodes.Contains(code))
                            {
                                var clip = cachingModel.GetAudioClip(data.DrwmsData.FSucceedAudioClipPath);
                                if (clip != null) audioSource.PlayOneShot(clip, data.MusicData.SSucceedVolume);
                                activeDrumCodes.Add(code);
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
                        if (mode.DrwmsData != null) activeDrumCodes.Remove(mode.DrwmsData.DrwmsData.DrumCode);
                    }
                }
                foreach (var dead in toRemove) gameObjects.Remove(dead);
            }

            lastUpdateTime = now;
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

        Debug.Log("CreateDrumsManager initialized...");
    }

    public void ResetAllActiveCodes()
    {
        activeDrumCodes.Clear();
        playedTipCodes.Clear();
    }

    public CreateSetClass CreateDrums(TheTypeOfOperation operation, DrumsLoadData drumsLoadData = null)
    {
        GameObject go = Instantiate(Resources.Load<GameObject>(PathConfig.ProfabsOath + "InputMode"));
        if (inputModeParent != null)
            go.transform.SetParent(inputModeParent, false);

        InputMode mode = go.GetComponent<InputMode>();

        // 第一次创建时用当前 ThisTime 作为 CenterTime
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

    // —— 工具：在开始一轮新的“游玩回合”时建议调用（例如进入 PlayMode 后） —— //
    public void ResetRuntimeForPlaySession(float now, bool destroyExisting = true)
    {
        activeDrumCodes.Clear();
        playedTipCodes.Clear();

        if (destroyExisting)
        {
            foreach (var im in gameObjects)
                if (im != null) Destroy(im.gameObject);
        }
        gameObjects.Clear();

        lastUpdateTime = now;
    }

    public class CreateSetClass
    {
        AudioSource _AudioSource;
        InputMode _Mode;

        public CreateSetClass() { }
        public CreateSetClass(AudioSource audioSource) { _AudioSource = audioSource; }

        static CreateSetClass instance;
        public static CreateSetClass Instance
        {
            get { if (instance == null) instance = new CreateSetClass(); return instance; }
            set { if (value != null) instance = value; }
        }

        public void SetInputMode(InputMode inputMode) { _Mode = inputMode; }
        public InputMode GetInputMode() { return _Mode; }

        public InputMode SetData(DrumsLoadData drumsLoadData)
        {
            _Mode.DrwmsData = drumsLoadData;
            return _Mode;
        }

        public void SetSuccessSounds(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            if (Clip != null) _Mode.SuccessClip = Clip;
            SetCpVector(channelPosition);
        }

        public void SetPreAdventSound(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            if (Clip != null) _Mode.PreAdventClip = Clip;
            _Mode.DrwmsData.DrwmsData.VPreAdventAudioClipOffsetTime = DelayTime;
            SetCpVector(channelPosition);
        }

        public void SetFailureSound(AudioClip Clip, float DelayTime, ChannelPosition channelPosition = ChannelPosition.FullChannel)
        {
            if (Clip != null) _Mode.LoseClip = Clip;
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

    public enum ChannelPosition { LeftChannel, RightChannel, FullChannel }
}
