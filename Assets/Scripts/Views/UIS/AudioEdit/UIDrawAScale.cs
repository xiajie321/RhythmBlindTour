using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 25/06/30 - mixyao
/// 使用统一结构记录节拍信息，支持 ThisTime 校准跳转节拍，提高体验。
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

    class BeatInfo
    {
        public int MeasureIndex;
        public int BeatInMeasure;
        public int TotalBeatIndex;
        public float Time;
        public GameObject Block;
    }

    List<BeatInfo> beatInfoList = new();
    int currentBeatIndex = 0;

    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;
    float scaleHeight = 80f;

    void Start()
    {
        editModel = this.GetModel<AudioEditModel>();
        this.RegisterEvent<BPMChangeValue>(v =>
        {
            GenerateScales();
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
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
        if (editModel.EditAudioClip == null || editModel.BPM <= 0) return;

        ClearAll();

        int beatA = editModel.BeatA;
        int beatB = editModel.BeatB;
        float beatDuration = 60f / editModel.BPM;
        float measureDuration = beatDuration * beatB;
        float audioLength = editModel.EditAudioClip.length;

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
            CreateScaleLine(time, isMainBeat, beatIndex, beatInMeasure, measureIndexFull);

            time += beatDuration;
            beatIndex++;
        }

        UpdateCurrentIndexToNearest(editModel.ThisTime);
    }

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

    void CreateScaleLine(float time, bool isMainBeat, int totalBeatIndex, int beatInMeasure, int measureIndex)
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

        var block = CreateClickableBlockAbove(time, width, totalBeatIndex, beatInMeasure, measureIndex);
        beatInfoList.Add(new BeatInfo
        {
            MeasureIndex = measureIndex,
            BeatInMeasure = beatInMeasure,
            TotalBeatIndex = totalBeatIndex,
            Time = time,
            Block = block
        });
    }

    GameObject CreateClickableBlockAbove(float time, float width, int totalBeatIndex, int beatInMeasure, int measureIndex)
    {
        GameObject block = new GameObject($"{(measureIndex + 1):D3}-{(beatInMeasure + 1):D3}-{(totalBeatIndex + 1):D3}");
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

        var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener((data) =>
        {
            this.SendCommand(new SetAudioEditThisTimeCommand(tCopy));
            FindObjectOfType<CreateDrumsManager>()?.ResetAllActiveCenters();
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

    void TriggerClick(GameObject block)
    {
        var trigger = block.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            foreach (var entry in trigger.triggers)
            {
                if (entry.eventID == EventTriggerType.PointerClick)
                {
                    entry.callback?.Invoke(new BaseEventData(EventSystem.current));
                    break;
                }
            }
        }
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
        return nearestIndex;
    }

    public void NextBeat()
    {
        for (int i = 0; i < beatInfoList.Count; i++)
        {
            if (beatInfoList[i].Time > editModel.ThisTime)
            {
                currentBeatIndex = i;
                TriggerClick(beatInfoList[i].Block);
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
                TriggerClick(beatInfoList[i].Block);
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
            if (beatInfoList[i].MeasureIndex > currentMeasure)
            {
                currentBeatIndex = i;
                TriggerClick(beatInfoList[i].Block);
                return;
            }
        }
    }

    public void PrevMeasure()
    {
        UpdateCurrentIndexToNearest(editModel.ThisTime);
        int currentMeasure = beatInfoList[currentBeatIndex].MeasureIndex;
        for (int i = currentBeatIndex - 1; i >= 0; i--)
        {
            if (beatInfoList[i].MeasureIndex < currentMeasure)
            {
                currentBeatIndex = i;
                TriggerClick(beatInfoList[i].Block);
                return;
            }
        }
    }
    public void MoveNextBeat()
    {
        float beatDuration = 60f / editModel.BPM;
        float newTime = editModel.ThisTime + beatDuration;
        this.SendCommand(new SetAudioEditThisTimeCommand(newTime));
    }

    public void MovePrevBeat()
    {
        float beatDuration = 60f / editModel.BPM;
        float newTime = Mathf.Max(0f, editModel.ThisTime - beatDuration);
        this.SendCommand(new SetAudioEditThisTimeCommand(newTime));
    }

    public void MoveNextMeasure()
    {
        float beatDuration = 60f / editModel.BPM;
        float measureDuration = beatDuration * editModel.BeatA; // 修正这里
        float newTime = editModel.ThisTime + measureDuration;
        this.SendCommand(new SetAudioEditThisTimeCommand(newTime));
    }

    public void MovePrevMeasure()
    {
        float beatDuration = 60f / editModel.BPM;
        float measureDuration = beatDuration * editModel.BeatA; // 修正这里
        float newTime = Mathf.Max(0f, editModel.ThisTime - measureDuration);
        this.SendCommand(new SetAudioEditThisTimeCommand(newTime));
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
