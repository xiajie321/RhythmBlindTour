using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 25/07/09 - mixyao
/// 节拍点击与便捷跳转均统一触发点击事件；节拍自动到达事件仅自动播放时触发
/// 25/08/08 - patch
/// - 监听 AudioEditModelLoad 以确保从 Level / SO 注入后也会重建
/// - Start 时自检一次（等待 Model/Clip/BPM/拍号就绪）
/// - 修正 measureDuration 计算，严格支持分母：60/BPM*(4/BeatB)
/// 25/08/10 - patch2
/// - 新增参数变更观察器：BPM/BeatA/BeatB/Clip 任一变化时自动重建刻度
/// </summary>
public class UIDrawAScale : MonoBehaviour, IController
{
    [SerializeField] RectTransform progressBar;

    [Header("颜色设置")]
    [SerializeField] Color mainBeatColor = Color.red;
    [SerializeField] Color subBeatColor = Color.black;
    [SerializeField]
    Color[] measureColors = new Color[]
    {
        new Color(0.9f, 0.9f, 0.9f),
        new Color(0.75f, 0.75f, 0.75f)
    };

    AudioEditModel editModel;
    List<GameObject> scaleLines = new();
    List<GameObject> measureBGs = new();
    List<GameObject> clickableBlocks = new();

    // 当前节拍文本信息
    private string currentMeasureStr = "";
    private string currentBeatInMeasureStr = "";
    private string currentBPMStr = "";

    public string CurrentMeasureStr => currentMeasureStr;
    public string CurrentBeatInMeasureStr => currentBeatInMeasureStr;
    public string CurrentBPMStr => currentBPMStr;

    // 事件
    public event Action<int, int, float, float> OnBeatBlockClicked; // measure, beat, bpm, time
    public event Action<int, int, float, float> OnBeatArrive;

    [Header("Inspector测试用UnityEvent")]
    public UnityEvent OnInspectorBeatClick;
    public UnityEvent OnInspectorBeatArrive;

    class BeatInfo
    {
        public int MeasureIndex;
        public int BeatInMeasure;
        public int TotalBeatIndex;
        public float Time;
        public float BPM;
        public GameObject Block;
    }

    List<BeatInfo> beatInfoList = new();
    int currentBeatIndex = 0;

    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;
    float scaleHeight = 80f;

    // —— 变更观察缓存 —— //
    int lastBPM = -1;
    int lastBeatA = -1;
    int lastBeatB = -1;
    AudioClip lastClip = null;

    void Start()
    {
        editModel = this.GetModel<AudioEditModel>();

        // A) 监听 BPM 改变 → 重建
        this.RegisterEvent<BPMChangeValue>(_ => GenerateScales())
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // B) 监听“加载完成” → 从 Level/SO 注入后重建
        this.RegisterEvent<AudioEditModelLoad>(_ => GenerateScales())
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // C) 启动后自检一次（避免错过事件）
        StartCoroutine(TryBuildOnceWhenReady());

        // D) 开启变更观察（BeatA/BeatB 在模型中是普通 int，不会自动发事件）
        StartCoroutine(WatchParamsChanges());
    }

    IEnumerator TryBuildOnceWhenReady()
    {
        // 等到模型和音频准备好 & BPM/拍号有效
        yield return new WaitUntil(() =>
            editModel != null &&
            editModel.EditAudioClip != null &&
            editModel.BPM > 0 &&
            editModel.BeatA > 0 &&
            editModel.BeatB > 0 &&
            progressBar != null);

        GenerateScales();
    }

    IEnumerator WatchParamsChanges()
    {
        // 小开销轮询：每帧/隔帧都行，这里每帧最简单
        while (true)
        {
            if (editModel != null)
            {
                var curClip = editModel.EditAudioClip;
                var curBPM = editModel.BPM;
                var curA = editModel.BeatA;
                var curB = editModel.BeatB;

                if (curClip != lastClip || curBPM != lastBPM || curA != lastBeatA || curB != lastBeatB)
                {
                    lastClip = curClip;
                    lastBPM = curBPM;
                    lastBeatA = curA;
                    lastBeatB = curB;

                    // 前置条件都满足时才重建
                    if (curClip != null && curBPM > 0 && curA > 0 && curB > 0 && progressBar != null)
                        GenerateScales();
                }
            }
            yield return null;
        }
    }

    void ClearAll()
    {
        foreach (var g in scaleLines) Destroy(g);
        foreach (var g in measureBGs) Destroy(g);
        foreach (var g in clickableBlocks) Destroy(g);
        scaleLines.Clear();
        measureBGs.Clear();
        clickableBlocks.Clear();
        beatInfoList.Clear();
        currentBeatIndex = 0;
    }

    public void GenerateScales()
    {
        if (editModel == null || progressBar == null) return;
        if (editModel.EditAudioClip == null) return;
        if (editModel.BPM <= 0 || editModel.BeatA <= 0 || editModel.BeatB <= 0) return;

        ClearAll();

        int beatA = editModel.BeatA;   // 每小节拍数（分子）
        int beatB = editModel.BeatB;   // 拍单位（分母：4=四分、8=八分…）
        // 更严谨：一拍时长要乘以 (4 / beatB)，确保 3/8、7/16 等拍号正确
        float beatDuration = 60f / editModel.BPM * (4f / beatB);
        float measureDuration = beatDuration * beatA; // 一小节 = beatA 个拍
        float audioLength = editModel.EditAudioClip.length;
        float bpm = editModel.BPM;

        int beatIndex = 0;
        float time = 0f;

        while (time < audioLength)
        {
            bool isMainBeat = (beatIndex % beatA == 0);

            if (isMainBeat)
            {
                int measureIndex = beatIndex / beatA;
                Color bgColor = measureColors[measureIndex % measureColors.Length];
                CreateMeasureBG(time, measureDuration, bgColor);
            }

            int beatInMeasure = beatIndex % beatA;
            int measureIndexFull = beatIndex / beatA;
            CreateScaleLine(time, isMainBeat, beatIndex, beatInMeasure, measureIndexFull, bpm);

            time += beatDuration;
            beatIndex++;
        }

        UpdateCurrentIndexToNearest(editModel.ThisTime);
        this.SendEvent(new OnScaleBuilt { TotalBeats = beatInfoList.Count });
    }
    public struct OnScaleBuilt { public int TotalBeats; }

    void CreateMeasureBG(float time, float duration, Color color)
    {
        GameObject bg = new GameObject("MeasureBG");
        bg.transform.SetParent(progressBar, false);

        RectTransform rt = bg.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(duration * _PixelUnitsPerSecond, scaleHeight);
        rt.anchoredPosition = new Vector2(time * _PixelUnitsPerSecond, 0);

        Image img = bg.AddComponent<Image>();
        img.color = color;

        measureBGs.Add(bg);
    }

    void CreateScaleLine(float time, bool isMainBeat, int totalBeatIndex, int beatInMeasure, int measureIndex, float bpm)
    {
        GameObject line = new GameObject(isMainBeat ? "MainBeat" : "SubBeat");
        line.transform.SetParent(progressBar, false);

        RectTransform rt = line.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(time * _PixelUnitsPerSecond, 0);

        float height = isMainBeat ? scaleHeight : scaleHeight * 0.3f;
        float width = 3f * 1.5f;
        rt.sizeDelta = new Vector2(width, height);

        Image img = line.AddComponent<Image>();
        img.color = isMainBeat ? mainBeatColor : subBeatColor;

        scaleLines.Add(line);

        var block = CreateClickableBlockAbove(time, width, totalBeatIndex, beatInMeasure, measureIndex, bpm);
        beatInfoList.Add(new BeatInfo
        {
            MeasureIndex = measureIndex,
            BeatInMeasure = beatInMeasure,
            TotalBeatIndex = totalBeatIndex,
            Time = time,
            BPM = bpm,
            Block = block
        });
    }

    GameObject CreateClickableBlockAbove(float time, float width, int totalBeatIndex, int beatInMeasure, int measureIndex, float bpm)
    {
        GameObject block = new GameObject($"{(measureIndex + 1):D3}-{(beatInMeasure + 1):D3}-{bpm:0}");
        block.transform.SetParent(progressBar, false);

        RectTransform rt = block.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(width * 7.5f, 20f);
        rt.anchoredPosition = new Vector2(time * _PixelUnitsPerSecond, scaleHeight);

        Image img = block.AddComponent<Image>();
        img.color = new Color(1f, 0.5f, 0f, 0.6f);

        var eventTrigger = block.AddComponent<EventTrigger>();
        float tCopy = time;
        int measureCopy = measureIndex;
        int beatInMeasureCopy = beatInMeasure;
        float bpmCopy = bpm;

        var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener((data) =>
        {
            this.SendCommand(new SetAudioEditThisTimeCommand(tCopy));
            FindObjectOfType<CreateDrumsManager>()?.ResetAllActiveCodes();

            // 查找当前BeatInfo
            var beat = beatInfoList.Find(b =>
                Mathf.Abs(b.Time - tCopy) < 0.001f &&
                b.MeasureIndex == measureCopy &&
                b.BeatInMeasure == beatInMeasureCopy);

            if (beat != null)
                InvokeBeatClickEvent(beat);
        });
        eventTrigger.triggers.Add(clickEntry);

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) =>
        {
            img.color = new Color(1f, 0.7f, 0.2f, 0.8f);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        });
        eventTrigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) =>
        {
            img.color = new Color(1f, 0.5f, 0f, 0.6f);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        });
        eventTrigger.triggers.Add(exitEntry);

        clickableBlocks.Add(block);
        return block;
    }

    // 统一触发节拍点击事件
    private void InvokeBeatClickEvent(BeatInfo beat)
    {
        currentMeasureStr = (beat.MeasureIndex + 1).ToString();
        currentBeatInMeasureStr = (beat.BeatInMeasure + 1).ToString();
        currentBPMStr = beat.BPM.ToString("0");
        OnBeatBlockClicked?.Invoke(beat.MeasureIndex, beat.BeatInMeasure, beat.BPM, beat.Time);

        // Inspector用
        OnInspectorBeatClick?.Invoke();
    }

    int UpdateCurrentIndexToNearest(float currentTime)
    {
        float minDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < beatInfoList.Count; i++)
        {
            float d = Mathf.Abs(beatInfoList[i].Time - currentTime);
            if (d < minDistance)
            {
                minDistance = d;
                nearestIndex = i;
            }
        }

        currentBeatIndex = nearestIndex;
        if (beatInfoList.Count > 0)
        {
            var beat = beatInfoList[nearestIndex];
            currentMeasureStr = (beat.MeasureIndex + 1).ToString();
            currentBeatInMeasureStr = (beat.BeatInMeasure + 1).ToString();
            currentBPMStr = beat.BPM.ToString("0");
        }
        return nearestIndex;
    }

    // 节拍-TimeHand事件：只在isControlRunning时触发
    float lastBeatTime = -100f;
    float lastCheckTime = 0f; // 类成员

    void Update()
    {
        if (beatInfoList.Count == 0 || editModel == null) return;
        bool isControlRunning = Qf.Managers.AudioEditManager.Instance?.IsControlRunning ?? false;
        if (!isControlRunning) return;

        float now = editModel.ThisTime;
        float prev = lastCheckTime;
        lastCheckTime = now;

        // 让 prev < now
        if (now < prev)
        {
            prev = now; // 跳播/倒退，直接重设
        }

        // 查找所有区间内未触发过的beat
        for (int i = 0; i < beatInfoList.Count; i++)
        {
            var beat = beatInfoList[i];
            // 在上一次和这一次之间的所有点
            if (beat.Time > prev && beat.Time <= now && !Mathf.Approximately(beat.Time, lastBeatTime))
            {
                lastBeatTime = beat.Time;
                OnBeatArrive?.Invoke(beat.MeasureIndex, beat.BeatInMeasure, beat.BPM, beat.Time);
                currentMeasureStr = (beat.MeasureIndex + 1).ToString();
                currentBeatInMeasureStr = (beat.BeatInMeasure + 1).ToString();
                currentBPMStr = beat.BPM.ToString("0");
                OnInspectorBeatArrive?.Invoke();
                // 不break，所有点都补全！
            }
        }
    }

    // 便捷移动/跳转方法全部用统一逻辑

    public void NextBeat()
    {
        for (int i = 0; i < beatInfoList.Count; i++)
        {
            if (beatInfoList[i].Time > editModel.ThisTime)
            {
                currentBeatIndex = i;
                this.SendCommand(new SetAudioEditThisTimeCommand(beatInfoList[i].Time));
                InvokeBeatClickEvent(beatInfoList[i]);
                return;
            }
        }
    }

    public void PrevBeat()
    {
        for (int i = beatInfoList.Count - 1; i >= 0; i--)
        {
            if (beatInfoList[i].Time < editModel.ThisTime)
            {
                currentBeatIndex = i;
                this.SendCommand(new SetAudioEditThisTimeCommand(beatInfoList[i].Time));
                InvokeBeatClickEvent(beatInfoList[i]);
                return;
            }
        }
    }

    public void NextMeasure()
    {
        UpdateCurrentIndexToNearest(editModel.ThisTime);
        int currentMeasure = beatInfoList[currentBeatIndex].MeasureIndex;
        for (int i = currentBeatIndex + 1; i < beatInfoList.Count; i++)
        {
            if (beatInfoList[i].MeasureIndex > currentMeasure && beatInfoList[i].BeatInMeasure == 0)
            {
                currentBeatIndex = i;
                this.SendCommand(new SetAudioEditThisTimeCommand(beatInfoList[i].Time));
                InvokeBeatClickEvent(beatInfoList[i]);
                return;
            }
        }
        for (int i = beatInfoList.Count - 1; i >= 0; i--)
        {
            if (beatInfoList[i].BeatInMeasure == 0)
            {
                currentBeatIndex = i;
                this.SendCommand(new SetAudioEditThisTimeCommand(beatInfoList[i].Time));
                InvokeBeatClickEvent(beatInfoList[i]);
                return;
            }
        }
    }

    public bool fixPreMeasure = false; // Inspector可设置

    public void PrevMeasure()
    {
        UpdateCurrentIndexToNearest(editModel.ThisTime);
        int currentMeasure = beatInfoList[currentBeatIndex].MeasureIndex;

        if (fixPreMeasure)
        {
            // 先判断当前是否已经在本小节开头（即BeatInMeasure==0）
            if (beatInfoList[currentBeatIndex].BeatInMeasure != 0)
            {
                // 本小节归位
                for (int i = currentBeatIndex; i >= 0; i--)
                {
                    if (beatInfoList[i].MeasureIndex == currentMeasure && beatInfoList[i].BeatInMeasure == 0)
                    {
                        currentBeatIndex = i;
                        this.SendCommand(new SetAudioEditThisTimeCommand(beatInfoList[i].Time));
                        InvokeBeatClickEvent(beatInfoList[i]);
                        return;
                    }
                }
            }
            else
            {
                // 已在本小节开头，再跳到上一个小节开头
                for (int i = currentBeatIndex - 1; i >= 0; i--)
                {
                    if (beatInfoList[i].BeatInMeasure == 0 && beatInfoList[i].MeasureIndex < currentMeasure)
                    {
                        currentBeatIndex = i;
                        this.SendCommand(new SetAudioEditThisTimeCommand(beatInfoList[i].Time));
                        InvokeBeatClickEvent(beatInfoList[i]);
                        return;
                    }
                }
                // 没找到，则跳最前面
                currentBeatIndex = 0;
                this.SendCommand(new SetAudioEditThisTimeCommand(beatInfoList[0].Time));
                InvokeBeatClickEvent(beatInfoList[0]);
            }
        }
        else
        {
            // 传统模式，直接跳到上一个小节头
            for (int i = currentBeatIndex - 1; i >= 0; i--)
            {
                if (beatInfoList[i].BeatInMeasure == 0 && beatInfoList[i].MeasureIndex < currentMeasure)
                {
                    currentBeatIndex = i;
                    this.SendCommand(new SetAudioEditThisTimeCommand(beatInfoList[i].Time));
                    InvokeBeatClickEvent(beatInfoList[i]);
                    return;
                }
            }
            // 没找到，跳最前面
            currentBeatIndex = 0;
            this.SendCommand(new SetAudioEditThisTimeCommand(beatInfoList[0].Time));
            InvokeBeatClickEvent(beatInfoList[0]);
        }
    }

    public void MoveNextBeat()
    {
        float beatDuration = 60f / editModel.BPM * (4f / editModel.BeatB);
        float newTime = editModel.ThisTime + beatDuration;
        this.SendCommand(new SetAudioEditThisTimeCommand(newTime));
    }

    public void MovePrevBeat()
    {
        float beatDuration = 60f / editModel.BPM * (4f / editModel.BeatB);
        float newTime = Mathf.Max(0f, editModel.ThisTime - beatDuration);
        this.SendCommand(new SetAudioEditThisTimeCommand(newTime));
    }

    public void MoveNextMeasure()
    {
        float beatDuration = 60f / editModel.BPM * (4f / editModel.BeatB);
        float measureDuration = beatDuration * editModel.BeatA;
        float newTime = editModel.ThisTime + measureDuration;
        this.SendCommand(new SetAudioEditThisTimeCommand(newTime));
    }

    public void MovePrevMeasure()
    {
        float beatDuration = 60f / editModel.BPM * (4f / editModel.BeatB);
        float measureDuration = beatDuration * editModel.BeatA;
        float newTime = Mathf.Max(0f, editModel.ThisTime - measureDuration);
        this.SendCommand(new SetAudioEditThisTimeCommand(newTime));
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
}
