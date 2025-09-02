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

    // 预知鼓点类型
    public enum PrefabDrumType
    {
        TipOnly = 0,       // 仅提示音（isCenter=false）
        AnswerOnly = 1,    // 仅回答音（isCenter=true）
        JudgeOnly = 2,     // 仅判定区（isCenter=true）
        PlaceAtCenter = 3, // 以判定区间为基准点创建鼓点（isCenter=true）
        PlaceAtTip = 4,    // 以预告音位置为基准点创建鼓点（isCenter=false）
        Audition = 5       // 试听（isCenter=true）
    }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<TrackStyle> trackStyles = new();
    [SerializeField] private GameObject DrumsProfabs;
    [SerializeField] private RectTransform[] DrumsUI;

    [Header("鼓点布局设置")]
    [SerializeField, Tooltip("是否以中心时间为创建位置锚点（保留开关）")]
    private bool isCenterCreate = false;

    [Header("创建选项")]
    [SerializeField] private bool isNextDrumDemo = false;
    [SerializeField] private bool isAutoTipOffset = false;

    [Header("UI: 预制类型下拉（作用于之后创建的所有新鼓点）")]
    [SerializeField] private UIDorpAttribute PrefabTypeDropdown;

    // ——“之后创建”的全局预制类型设置（默认中心）——
    [SerializeField] private PrefabDrumType nextPrefabType = PrefabDrumType.PlaceAtCenter;

    [Header("TTS")]
    [SerializeField, Tooltip("创建参数非法时是否播报错误TTS")]
    private bool enableCreateErrorTTS = true;

    public void ToggleNextDrumDemo(bool isOn) => isNextDrumDemo = isOn;
    public void ToggleAutoTipOffset(bool isOn) => isAutoTipOffset = isOn;

    // 外部（或下拉）设置“之后创建”的预制类型
    public void SetNextPrefabType(int dropdownIndex)
    {
        Debug.Log("DropDown : " + dropdownIndex);
        dropdownIndex = Mathf.Clamp(dropdownIndex, 0, 5); // 现在一共 6 种（0..5）
        nextPrefabType = (PrefabDrumType)dropdownIndex;

        BroadcastNavigationMode();

    }

    public void SetNextPrefabType(PrefabDrumType t)
    {
        nextPrefabType = t;
        BroadcastNavigationMode();
    }
    // 当前预设是否按“中心”放置（仅 Orbit 内部判定，不写回数据）
    private bool IsCenterForCurrentPreset()
    {
        return nextPrefabType == PrefabDrumType.AnswerOnly
            || nextPrefabType == PrefabDrumType.JudgeOnly
            || nextPrefabType == PrefabDrumType.PlaceAtCenter
            || nextPrefabType == PrefabDrumType.Audition; // TipOnly / PlaceAtTip 返回 false
    }

    // —— 可被外部监听的 TTS 请求事件 —— //
    public struct OnTTSRequest { public string Text; }

    private void SpeakTTS(string text)
    {
        if (!enableCreateErrorTTS) return;
        this.SendEvent(new OnTTSRequest { Text = text });
    }

    private static string OpLabel(TheTypeOfOperation op)
    {
        switch (op)
        {
            case TheTypeOfOperation.SwipeUp: return "上滑";
            case TheTypeOfOperation.SwipeDown: return "下滑";
            case TheTypeOfOperation.SwipeLeft: return "左滑";
            case TheTypeOfOperation.SwipeRight: return "右滑";
            case TheTypeOfOperation.Click: return "点击";
            default: return op.ToString();
        }
    }

    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;
    int _EditHeight = AudioEditConfig.EditHeight;
    AudioEditModel editModel;
    Dictionary<TheTypeOfOperation, int> operationToTrackIndex;
    [SerializeField] private UIAudioEditTimeHand timeHand;
    [SerializeField] private Transform drumsPoolHiddenRoot;

    private UIAudioEditDrumsPool drumsPool;

    // 面板之间统一选择事件
    public struct OnSelectDrumByCode
    {
        public string DrumCode;
    }

    void Start()
    {
        Init();
        //SetNextPrefabType(3);
        BroadcastNavigationMode();//广播告知当前设置鼓点的锚点模式
        // 绑定下拉（若拖了引用）
        if (PrefabTypeDropdown != null)
        {
            PrefabTypeDropdown.SetDropdownVlaue<PrefabDrumType>(nextPrefabType);
            PrefabTypeDropdown.SetAction(v =>
            {
                int idx = System.Convert.ToInt32(v);
                SetNextPrefabType(idx);
            });
        }

        this.RegisterEvent<OnSelectDrumByCode>(e => currentSelectedDrumCode = e.DrumCode)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    void Update() => InputContller();
    public IArchitecture GetArchitecture() => GameBody.Interface;

    void Init()
    {
        editModel = this.GetModel<AudioEditModel>();
        InitOperationTracks();
        StartLength(); // 初始一次
        drumsPool = new UIAudioEditDrumsPool(DrumsProfabs, DrumsUI);

        // 鼓点 UI 刷新
        this.RegisterEvent<OnUpdateAudioEditDrumsUI>(v => UpDateDrwmsUI())
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // 主音频变化 → 重设长度
        this.RegisterEvent<MainAudioChangeValue>(v => StartLength())
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // Level/SO 注入完成 → 重设长度
        this.RegisterEvent<AudioEditModelLoad>(v => StartLength())
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // 波形生成就绪 → 再同步一次长度（使用像素宽度最稳）
        this.RegisterEvent<UIAudioEditWaveformDiagram.OnWaveformReady>(e => StartLength(e.PixelWidth))
            .UnRegisterWhenGameObjectDestroyed(gameObject);
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

    // 创建鼓点（优先按 TypeSettings 初始化；再退回全局；最后受 nextPrefabType 修饰）
    public void AddDrwms(TheTypeOfOperation op)
    {
        if (editModel.EditAudioClip == null) return;

        float thisTime = editModel.ThisTime;
        int fixedIndex = (int)op;

        // —— 读取 TypeSettings（若无则为 null） ——
        editModel.TypeSettings.TryGetValue(op, out var ts);

        // —— 优先 TypeSettings → 退回全局 —— 
        // 时间参数
        float existence = isNextDrumDemo ? 0f :
                          (ts != null ? ts.TimeOfExistence : editModel.TimeOfExistence.Value);
        float tipOffset = ts != null ? ts.TipAdvance : editModel.TipOffset.Value;
        float tipPlayOffset = ts != null ? ts.TipPlayOffset : 0f; // 全局无统一“播放偏移”，默认 0

        // 音量（0..1）
        float preVol = ts != null ? ts.TipVolume : editModel.PreAdventVolume.Value;
        float sucVol = ts != null ? ts.SucceedVolume : editModel.SucceedAudioVolume.Value;
        float loseVol = ts != null ? ts.LoseVolume : editModel.LoseAudioVolume.Value;
        float defVol = ts != null ? ts.DefaultVolume : editModel.DefaultAudioVolume.Value;

        // 音频名称（字符串）
        string tipName = !string.IsNullOrEmpty(ts?.TipAudio)
                            ? ts.TipAudio
                            : this.SendQuery(new QueryAudioEditComeTipAudio(op))?.name;

        string succName = !string.IsNullOrEmpty(ts?.SucceedAudio)
                            ? ts.SucceedAudio
                            : this.SendQuery(new QueryAudioEditSucceedsAudio(op))?.name;

        string loseName = !string.IsNullOrEmpty(ts?.LoseAudio)
                            ? ts.LoseAudio
                            : (editModel.LoseAudioClip ? editModel.LoseAudioClip.name : null);

        string defName = !string.IsNullOrEmpty(ts?.DefaultAudio)
                            ? ts.DefaultAudio
                            : (editModel.DefaultAudioClip ? editModel.DefaultAudioClip.name : null);

        // 是否自动把“提示偏移=时长的一半”
        if (isAutoTipOffset) tipOffset = existence / 2f;

        // —— 按“之后创建”的预制类型再做修饰 —— //
        bool placeAtCenter = IsCenterForCurrentPreset();
        switch (nextPrefabType)
        {
            case PrefabDrumType.TipOnly:
                placeAtCenter = false; existence = 0f; tipOffset = 0f; sucVol = 0f; break;
            case PrefabDrumType.AnswerOnly:
                placeAtCenter = true; existence = 0f; tipOffset = 0f; preVol = 0f; break;
            case PrefabDrumType.JudgeOnly:
                placeAtCenter = true; tipOffset = 0f; preVol = 0f; break;
            case PrefabDrumType.PlaceAtCenter:
                placeAtCenter = true; break;
            case PrefabDrumType.PlaceAtTip:
                placeAtCenter = false; break;
            case PrefabDrumType.Audition:
                placeAtCenter = true; existence = 0f; break;
        }

        // —— 正常鼓点校验：仅在 PlaceAtCenter / PlaceAtTip 下触发 —— //
        if (nextPrefabType == PrefabDrumType.PlaceAtCenter || nextPrefabType == PrefabDrumType.PlaceAtTip)
        {
            // 1) 任一音频设置为空
            if (string.IsNullOrEmpty(tipName) || string.IsNullOrEmpty(succName)
                || string.IsNullOrEmpty(loseName) || string.IsNullOrEmpty(defName))
            {
                SpeakTTS($"{OpLabel(op)}鼓点创建错误,存在为空的音频设置");
                return;
            }

            // 2) 判定时长值为 0
            if (Mathf.Approximately(existence, 0f))
            {
                SpeakTTS($"{OpLabel(op)}鼓点创建错误,判定时长值为0");
                return;
            }

            // 3) 提示音提前值为 0
            if (Mathf.Approximately(tipOffset, 0f))
            {
                SpeakTTS($"{OpLabel(op)}鼓点创建错误,提示音提前值为零");
                return;
            }
        }

        // 用“中心时间”作为创建位置 & 唯一 key
        float centerTime = placeAtCenter ? thisTime : thisTime + tipOffset;

        var dataDict = this.SendQuery(new QueryAudioEditTimeLineAllData());

        // 以 centerTime 查重（含浮点容差）
        if (TryGetListAtTime(dataDict, centerTime, out var existList, out _))
        {
            foreach (var drum in existList)
            {
                if (drum.DrwmsData.DtheTypeOfOperation == op)
                {
                    // 同中心时间、同轨道 不允许重复
                    return;
                }
            }
        }

        // —— 组装新鼓点数据 —— //
        var newDrums = new DrumsLoadData
        {
            DrwmsData = new DrwmsData
            {
                DtheTypeOfOperation = op,
                FPreAdventAudioClipPath = tipName,
                FSucceedAudioClipPath = succName,
                FLoseAudioClipPath = loseName,
                FDefaultAudioClipPath = defName,
                VPreAdventAudioClipOffsetTime = tipOffset,
                VTimeOfExistence = existence,
                VTipPlayOffset = tipPlayOffset,
                CenterTime = centerTime,
                DrumCode = GenerateDrumCode(centerTime, fixedIndex),
                PrefabType = (int)nextPrefabType
            },
            MusicData = new MusicData
            {
                SPreAdventVolume = preVol,
                SSucceedVolume = sucVol,
                SLoseVolume = loseVol,
                SDefaultVolume = defVol
            }
        };

        // 写入使用 centerTime（与查重一致）
        this.SendCommand(new AddAudioEditTimeLineDataCommand(centerTime, newDrums));

        // 通知 UI 刷新（确保 SetPanel/Orbit 立刻看到）
        this.SendEvent<OnUpdateAudioEditDrumsUI>();

        // 选中新建的鼓点
        GameBody.Interface.SendEvent(new OnSelectDrumByCode { DrumCode = newDrums.DrwmsData.DrumCode });

        // 试听（非 PlayMode）：根据锚点类型决定播放哪个音效
        if (!editModel.Mode.Equals(SystemModeData.PlayMode))
        {
            var cache = this.GetModel<DataCachingModel>();
            if (placeAtCenter)
            {
                // Center 系：回答音
                var clip = cache.GetAudioClip(succName);
                if (clip != null) AudioEditManager.Instance.PlayVFXWithFallback(op, clip, 1f);
            }
            else
            {
                // Tip 系：提示音
                var clip = cache.GetAudioClip(tipName);
                if (clip != null) AudioEditManager.Instance.PlayVFXWithFallback(op, clip, 1f);
            }
        }

    }

    private const float TIME_EPS = 0.0005f;

    /// <summary>
    /// 在时间轴字典中，用 keyTime 查找列表；若精确不存在，则容忍浮点误差做一次近似匹配。
    /// </summary>
    private bool TryGetListAtTime(Dictionary<float, List<DrumsLoadData>> dict, float keyTime,
                                  out List<DrumsLoadData> list, out float matchedKey)
    {
        if (dict.TryGetValue(keyTime, out list))
        {
            matchedKey = keyTime;
            return true;
        }
        foreach (var kv in dict)
        {
            if (Mathf.Abs(kv.Key - keyTime) < TIME_EPS)
            {
                matchedKey = kv.Key;
                list = kv.Value;
                return true;
            }
        }
        matchedKey = 0f;
        list = null;
        return false;
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
        UIAttributeSetPanel.DecodeDrumCode(currentSelectedDrumCode, out float codeTime, out int typeIndex);

        //  用 DrumCode 解出的时间作为 key，确保删的是“中心时间上的那个”
        RemoveDrwmsByType(typeIndex, codeTime);
    }

    public void RemoveDrwmsByType(int trackIndex)
    {
        // 兼容旧用法：用当前编辑时间
        RemoveDrwmsByType(trackIndex, editModel.ThisTime);
    }

    public void RemoveDrwmsByType(int trackIndex, float timeKey)
    {
        TheTypeOfOperation op = (TheTypeOfOperation)trackIndex;
        var dataDict = this.SendQuery(new QueryAudioEditTimeLineAllData());

        if (TryGetListAtTime(dataDict, timeKey, out var list, out float matchedKey))
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].DrwmsData.DtheTypeOfOperation == op)
                {
                    //  用 matchedKey（可能是容差匹配到的真实键）
                    this.SendCommand(new RemoveAudioEditTimeLineDataCommand(matchedKey, i));
                    return;
                }
            }
        }
    }

    public void RemoveDrwms(int index = -1)
    {
        RemoveDrwms(editModel.ThisTime, index);
    }

    public void RemoveDrwms(float timeKey, int index = -1)
    {
        this.SendCommand(new RemoveAudioEditTimeLineDataCommand(timeKey, index));
    }

    // —— 轨道长度同步（使用音频秒数）——
    void StartLength()
    {
        float songTime = this.SendQuery(new QueryAudioEditAudioClipLength());
        int widthPx = Mathf.CeilToInt(songTime * _PixelUnitsPerSecond);
        ApplyTracksLength(widthPx);
    }

    // —— 轨道长度同步（使用像素宽度）——
    void StartLength(int pixelWidth)
    {
        int widthPx = Mathf.Max(0, pixelWidth);
        ApplyTracksLength(widthPx);
    }

    // 应用统一：左对齐 + 长度设置
    void ApplyTracksLength(int widthPx)
    {
        if (DrumsUI == null) return;

        float trackH = _EditHeight / Mathf.Max(1, DrumsUI.Length);
        for (int i = 0; i < DrumsUI.Length; i++)
        {
            var rt = DrumsUI[i];
            if (rt == null) continue;

            // —— 左对齐到 0 —— 
            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(0f, rt.anchorMax.y);
            rt.pivot = new Vector2(0f, rt.pivot.y);
            rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);

            // 同步轨道长度
            rt.sizeDelta = new Vector2(widthPx, trackH);
        }
        this.SendEvent(new OnOrbitTracksReady { PixelWidth = widthPx });

    }
    public struct OnOrbitTracksReady { public int PixelWidth; }

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
                if (dataDict[item] != null)
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

        // —— 将 “预设标签” 传入 tip/center 两个 UI 组件 —— 
        foreach (var ui in root.GetComponentsInChildren<UIAudioEditDrums>(true))
        {
            ui.PrefabType = (UIAudioEditDrumsOrbit.PrefabDrumType)data.DrwmsData.PrefabType; // 修正
            ui.SetTimeHand(timeHand);
            ui.DrumCode = data.DrwmsData.DrumCode;
            ui.PreviewAudioSource = AudioEditManager.Instance.GetVFXSource(type);
            ui.InitUI(time, trackIndex, ui.IsTip, style, tipOffset, existence, _PixelUnitsPerSecond);
        }

        // 不在这里广播选中事件
    }
    // #endredgin

    public void ClearUnusedPoolObjects() => drumsPool.ClearUnused();

    // [鼓点唯一编号生成工具] -- mixyao/07/08
    public static string GenerateDrumCode(float thisTime, int index)
    {
        int timeInt = Mathf.RoundToInt(thisTime * 100); // 两位小数
        return timeInt.ToString("D5") + index.ToString("D1"); // 5+1=6位
    }

    /// <summary>清空底层鼓点数据</summary>
    public void ClearAllDrumsData()
    {
        editModel.TimeLineData.Clear();
        editModel.ThisTime = 0f;
        this.SendEvent<OnUpdateAudioEditDrumsUI>();
        this.SendEvent(new OnUpdateThisTime { ThisTime = editModel.ThisTime });
    }

    // UIAudioEditDrumsOrbit.cs 顶部 class 内新增：
    // —— 列表导航锚点模式变更（true=按Tip导航；false=按Center导航）——
    public struct OnPlacementNavigationModeChanged { public bool UseTipForNavigation; }
    // UIAudioEditDrumsOrbit.cs 内部任意位置新增一个小工具：
    private void BroadcastNavigationMode()
    {
        bool useTip = !IsCenterForCurrentPreset(); // PlaceAtTip / TipOnly => true
        this.SendEvent(new OnPlacementNavigationModeChanged { UseTipForNavigation = useTip });
    }

}
