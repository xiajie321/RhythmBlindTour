using Assets.Scripts.Querys.AudioEdit;
using Qf.ClassDatas.AudioEdit;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Managers;
using Qf.Models;
using Qf.Models.AudioEdit;
using Qf.Querys.AudioEdit;
using Qf.Systems;
using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAudioEditDrumsOrbit : MonoBehaviour, IController
{
    [System.Serializable]
    public class TrackStyle
    {
        public TheTypeOfOperation Operation;
        public Color DrumColor;
        public Color PreTipColor;
        public Sprite DrumSprite;
        public Sprite PreTipSprite;
    }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<TrackStyle> trackStyles = new();
    [SerializeField] private GameObject DrumsProfabs;
    [SerializeField] private RectTransform[] DrumsUI;

    [Header("鼓点布局设置")]
    [SerializeField, Tooltip("是否以中心时间为创建位置锚点")]
    private bool isCenterCreate = false;

    private bool isNextDrumDemo = false;
    private bool isAutoTipOffset = false;
    public void ToggleNextDrumDemo(bool isOn) => isNextDrumDemo = isOn;
    public void ToggleAutoTipOffset(bool isOn) => isAutoTipOffset = isOn;

    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;
    int _EditHeight = AudioEditConfig.EditHeight;
    AudioEditModel editModel;
    Dictionary<TheTypeOfOperation, int> operationToTrackIndex;
    [SerializeField] private UIAudioEditTimeHand timeHand;
    [SerializeField] private Transform drumsPoolHiddenRoot;

    private UIAudioEditDrumsPool drumsPool;

    void Start() => Init();
    void Update() => InputContller();
    IArchitecture IBelongToArchitecture.GetArchitecture() => GameBody.Interface;

    void Init()
    {
        editModel = this.GetModel<AudioEditModel>();
        InitOperationTracks();
        StartLength();
        drumsPool = new UIAudioEditDrumsPool(DrumsProfabs, DrumsUI);

        this.RegisterEvent<OnUpdateAudioEditDrumsUI>(v => UpDateDrwmsUI()).UnRegisterWhenGameObjectDestroyed(gameObject);
        this.RegisterEvent<MainAudioChangeValue>(v => StartLength()).UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    void InitOperationTracks()
    {
        operationToTrackIndex = new();
        for (int i = 0; i < trackStyles.Count; i++)
            if (!operationToTrackIndex.ContainsKey(trackStyles[i].Operation))
                operationToTrackIndex[trackStyles[i].Operation] = i;
    }

    void InputContller()
    {
        if (!editModel.Mode.Equals(SystemModeData.RecordingMode)) return;
        if (AudioEditManager.Instance != null && AudioEditManager.Instance.IsControlRunning)
        {
            if (InputSystems.Click) AddDrwms(TheTypeOfOperation.Click);
            if (InputSystems.SwipeUp) AddDrwms(TheTypeOfOperation.SwipeUp);
            if (InputSystems.SwipeDown) AddDrwms(TheTypeOfOperation.SwipeDown);
            if (InputSystems.SwipeLeft) AddDrwms(TheTypeOfOperation.SwipeLeft);
            if (InputSystems.SwipeRight) AddDrwms(TheTypeOfOperation.SwipeRight);
            return;
        }
        if (InputSystems.Click) AddDrwms(TheTypeOfOperation.Click);
        if (InputSystems.SwipeUp) AddDrwms(TheTypeOfOperation.SwipeUp);
        if (InputSystems.SwipeDown) AddDrwms(TheTypeOfOperation.SwipeDown);
        if (InputSystems.SwipeLeft) AddDrwms(TheTypeOfOperation.SwipeLeft);
        if (InputSystems.SwipeRight) AddDrwms(TheTypeOfOperation.SwipeRight);
    }

    public void AddDrwms(int i = 0)
    {
        TheTypeOfOperation op = (TheTypeOfOperation)Mathf.Clamp(i, 0, 4);
        AddDrwms(op);
    }

    public void AddDrwms(TheTypeOfOperation op)
    {
        if (editModel.EditAudioClip == null) return;

        float thisTime = editModel.ThisTime;
        var dataDict = this.SendQuery(new QueryAudioEditTimeLineAllData());
        if (dataDict.TryGetValue(thisTime, out var list))
        {
            if (list.Count >= 5) return;
            foreach (var drum in list)
                if (drum.DrwmsData.DtheTypeOfOperation == op) return;
        }

        float tipOffset = editModel.TipOffset.Value;
        float existence = isNextDrumDemo ? 0f : editModel.TimeOfExistence.Value;
        tipOffset = isAutoTipOffset ? existence / 2 : tipOffset;
        float centerTime = isCenterCreate ? thisTime : thisTime + tipOffset;
        centerTime = isAutoTipOffset ? thisTime + tipOffset : centerTime;

        var newDrums = new DrumsLoadData
        {
            DrwmsData = new DrwmsData
            {
                DtheTypeOfOperation = op,
                FPreAdventAudioClipPath = this.SendQuery(new QueryAudioEditComeTipAudio(op))?.name,
                FSucceedAudioClipPath = this.SendQuery(new QueryAudioEditSucceedsAudio(op))?.name,
                FLoseAudioClipPath = editModel.LoseAudioClip?.name,
                VPreAdventAudioClipOffsetTime = tipOffset,
                VTimeOfExistence = existence,
                CenterTime = centerTime,
            },
            MusicData = new MusicData
            {
                SPreAdventVolume = editModel.PreAdventVolume.Value,
                SLoseVolume = editModel.LoseAudioVolume.Value,
                SSucceedVolume = editModel.SucceedAudioVolume.Value
            }
        };

        this.SendCommand(new AddAudioEditTimeLineDataCommand(centerTime, newDrums));
        var clip = this.GetModel<DataCachingModel>().GetAudioClip(isCenterCreate ? newDrums.DrwmsData.FSucceedAudioClipPath : newDrums.DrwmsData.FPreAdventAudioClipPath);
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    void UpDateDrwmsUI() => UpDateAllDrwmsUI();

    public void RemoveDrwms(int index = -1)
    {
        this.SendCommand(new RemoveAudioEditTimeLineDataCommand(editModel.ThisTime, index));
    }

    void StartLength()
    {
        float songTime = this.SendQuery(new QueryAudioEditAudioClipLength());
        for (int i = 0; i < DrumsUI.Length; i++)
            DrumsUI[i].sizeDelta = new Vector2(songTime * _PixelUnitsPerSecond, _EditHeight / DrumsUI.Length);
    }

    void UpDateAllDrwmsUI()
    {
        drumsPool.RecycleAll();
        var dataDict = this.SendQuery(new QueryAudioEditTimeLineAllData());

        foreach (var item in dataDict.Keys)
            for (int i = 0; i < dataDict[item].Count; i++)
                if (dataDict[item][i] != null)
                    CreateDrumItemUI(item, i, dataDict);
    }

    void CreateDrumItemUI(float time, int index, Dictionary<float, List<DrumsLoadData>> dataDict)
    {
        var data = dataDict[time][index];
        if (!operationToTrackIndex.TryGetValue(data.DrwmsData.DtheTypeOfOperation, out int trackIndex)) return;
        var style = trackStyles[trackIndex];

        float tipOffset = data.DrwmsData.VPreAdventAudioClipOffsetTime;
        float existence = data.DrwmsData.VTimeOfExistence;
        float drumX = time * _PixelUnitsPerSecond;

        //  从池中获取对象，并设置位置，保持在轨道下，无需 setParent
        var root = drumsPool.Get(trackIndex, drumX);

        foreach (var ui in root.GetComponentsInChildren<UIAudioEditDrums>(true))
        {
            ui.SetTimeHand(timeHand);
            ui.InitUI(time, index, ui.IsTip, style, tipOffset, existence, _PixelUnitsPerSecond);
        }
    }


    public void ClearUnusedPoolObjects() => drumsPool.ClearUnused(); // 可暴露至外部使用 -- mixyao/25/07/04
}
