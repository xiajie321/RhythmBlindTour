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
    private bool isOnlySoundDrum = false; // 新增

    public void ToggleNextDrumDemo(bool isOn) => isNextDrumDemo = isOn;
    public void ToggleAutoTipOffset(bool isOn) => isAutoTipOffset = isOn;
    public void ToggleOnlySoundDrum(bool isOn) => isOnlySoundDrum = isOn; // 新增


    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;
    int _EditHeight = AudioEditConfig.EditHeight;
    AudioEditModel editModel;
    Dictionary<TheTypeOfOperation, int> operationToTrackIndex;
    [SerializeField] private UIAudioEditTimeHand timeHand;
    [SerializeField] private Transform drumsPoolHiddenRoot;

    private UIAudioEditDrumsPool drumsPool;
    // 推荐：放在 UIAudioEditDrumsOrbit.cs 文件顶部
    public struct OnSelectDrumByCode
    {
        public string DrumCode;
    }


    void Start()
    {
        Init();

        this.RegisterEvent<OnSelectDrumByCode>(e => currentSelectedDrumCode = e.DrumCode)
            .UnRegisterWhenGameObjectDestroyed(gameObject);

    }

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

    // 替换 AddDrwms 方法
    public void AddDrwms(TheTypeOfOperation op)
    {
        if (editModel.EditAudioClip == null) return;
        float thisTime = editModel.ThisTime;

        // index 直接由类型 int 值决定
        int fixedIndex = (int)op;
        var dataDict = this.SendQuery(new QueryAudioEditTimeLineAllData());
        if (dataDict.TryGetValue(thisTime, out var list))
        {
            foreach (var drum in list)
                if (drum.DrwmsData.DtheTypeOfOperation == op) return; // 同时间同类型禁止重复
        }

        float tipOffset;
        float existence;

        // ---- 新增“只发声鼓点”逻辑 ----
        if (isOnlySoundDrum)
        {
            existence = 0f;
            tipOffset = 0f;
        }
        else
        {
            tipOffset = editModel.TipOffset.Value;
            existence = isNextDrumDemo ? 0f : editModel.TimeOfExistence.Value;
            tipOffset = isAutoTipOffset ? existence / 2 : tipOffset;
        }
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
                DrumCode = GenerateDrumCode(thisTime, fixedIndex),
            },
            MusicData = new MusicData
            {
                SPreAdventVolume = editModel.PreAdventVolume.Value,
                SLoseVolume = editModel.LoseAudioVolume.Value,
                SSucceedVolume = editModel.SucceedAudioVolume.Value
            }
        };

        this.SendCommand(new AddAudioEditTimeLineDataCommand(centerTime, newDrums));

        // 只在真正新增鼓点后，发一次唯一选中事件（面板监听此事件即可）
        GameBody.Interface.SendEvent(new OnSelectDrumByCode() { DrumCode = newDrums.DrwmsData.DrumCode });

        // 只发声鼓点：只播放正确音效，不播放其他
        if (isOnlySoundDrum)
        {
            var succeedClipPath = newDrums.DrwmsData.FSucceedAudioClipPath;
            var succeedClip = this.GetModel<DataCachingModel>().GetAudioClip(succeedClipPath);
            if (!editModel.Mode.Equals(SystemModeData.PlayMode) && succeedClip != null)
            {
                AudioEditManager.Instance.PlayVFXWithFallback(op, succeedClip, 1f);
            }
            return;
        }

        // 原有试听逻辑
        var sucPath = newDrums.DrwmsData.FSucceedAudioClipPath;
        var sucClip = this.GetModel<DataCachingModel>().GetAudioClip(sucPath);
        if (!editModel.Mode.Equals(SystemModeData.PlayMode) && sucClip != null)
        {
            AudioEditManager.Instance.PlayVFXWithFallback(op, sucClip, 1f);
        }
    }



    void UpDateDrwmsUI() => UpDateAllDrwmsUI();
    private string currentSelectedDrumCode;
    public void RemoveDrwmAuto()
    {
        if (string.IsNullOrEmpty(currentSelectedDrumCode))
        {
            Debug.LogWarning("No drum selected for delete.");
            return;
        }

        // 解析 DrumCode 获取 time 和 typeindex
        UIAttributeSetPanel.DecodeDrumCode(currentSelectedDrumCode, out float thisTime, out int typeIndex);

        // 查找 type 并删除
        RemoveDrwmsByType(typeIndex);
    }

    public void RemoveDrwmsByType(int trackIndex)
    {
        TheTypeOfOperation op = (TheTypeOfOperation)trackIndex;
        float time = editModel.ThisTime;
        var dataDict = this.SendQuery(new QueryAudioEditTimeLineAllData());
        if (dataDict.TryGetValue(time, out var list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].DrwmsData.DtheTypeOfOperation == op)
                {
                    RemoveDrwms(i);
                    return;
                }
            }
        }
    }


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
    public void ClearAllDrwmsUI()
    {

        UpDateAllDrwmsUI();
        ClearUnusedPoolObjects();

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


    // #redgin 鼓点创建后自动选中高亮 -- 2024-07-14
    void CreateDrumItemUI(float time, int index, Dictionary<float, List<DrumsLoadData>> dataDict)
    {
        var data = dataDict[time][index];
        var type = data.DrwmsData.DtheTypeOfOperation;
        int trackIndex = (int)type;
        var style = trackStyles[trackIndex];

        float tipOffset = data.DrwmsData.VPreAdventAudioClipOffsetTime;
        float existence = data.DrwmsData.VTimeOfExistence;
        float drumX = time * _PixelUnitsPerSecond;

        var root = drumsPool.Get(trackIndex, drumX);

        foreach (var ui in root.GetComponentsInChildren<UIAudioEditDrums>(true))
        {
            ui.SetTimeHand(timeHand);
            ui.DrumCode = data.DrwmsData.DrumCode;
            ui.PreviewAudioSource = AudioEditManager.Instance.GetVFXSource(type);
            ui.InitUI(time, trackIndex, ui.IsTip, style, tipOffset, existence, _PixelUnitsPerSecond);
        }
        // 不再这里自动选中/广播选中事件！
    }

    // #endredgin



    public void ClearUnusedPoolObjects() => drumsPool.ClearUnused(); // 可暴露至外部使用 -- mixyao/25/07/04


    // [鼓点唯一编号生成工具] -- mixyao/07/08
    public static string GenerateDrumCode(float thisTime, int index)
    {
        int timeInt = Mathf.RoundToInt(thisTime * 100); // 两位小数
        return timeInt.ToString("D5") + index.ToString("D1"); // 5+1=6位
    }
    /// <summary>
    /// 完全清空底层鼓点数据，并通知 UI 刷新
    /// </summary>
    public void ClearAllDrumsData()
    {
        // 1. 清空底层数据
        editModel.TimeLineData.Clear();

        // （可选）把当前播放/编辑指针归零
        editModel.ThisTime = 0f;

        // 2. 发事件让轨道上的鼓点 UI 重建（重建逻辑会读取空的 TimeLineData）
        this.SendEvent<OnUpdateAudioEditDrumsUI>();

        // 3. 发事件让 Inspector 面板也刷新（根据 ThisTime 重新高亮，数据为空就清空列表）
        this.SendEvent(new OnUpdateThisTime { ThisTime = editModel.ThisTime });
    }
}
