using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Qf.Models.AudioEdit;
using QFramework;
using Qf.ClassDatas.AudioEdit;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Managers;

public class mUIDrumsInspectorPanel : MonoBehaviour, IController
{
    public GameObject ItemPrefab;
    public Transform ContentRoot;
    private AudioEditModel editModel;
    private List<DrumRow> rows = new();
    private Dictionary<DrumsLoadData, int> drumIdLookup = new();

    public enum SortMode
    {
        ByIndex,
        ByTime
    }

    public SortMode currentPrimaryMode = SortMode.ByIndex;
    public bool typeSortEnabled = false;

    void Start()
    {
        StartCoroutine(InitEditModelIfNeeded());

        this.RegisterEvent<OnUpdateThisTime>(OnTimeNeedCheck)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    IEnumerator InitEditModelIfNeeded()
    {
        yield return new WaitUntil(() => GameBody.Interface != null);

        while (editModel == null)
        {
            try
            {
                editModel = this.GetModel<AudioEditModel>();
            }
            catch { }
            yield return null;
        }
    }

    public void EnsureReadyAndRefresh()
    {
        if (editModel == null)
        {
            try
            {
                editModel = this.GetModel<AudioEditModel>();
            }
            catch
            {
                Debug.LogError("[mUIDrumsInspectorPanel] editModel 获取失败");
                return;
            }
        }

        if (editModel != null)
        {
            RefreshList();
        }
    }

    public void RefreshList()
    {
        if (AudioEditManager.Instance.IsControlRunning) return;
        if (ItemPrefab == null || ContentRoot == null || editModel == null)
        {
            Debug.LogError("[mUIDrumsInspectorPanel] 组件未就绪，刷新失败");
            return;
        }

        foreach (var row in rows)
            Destroy(row.GameObject);
        rows.Clear();

        var timeDict = editModel.TimeLineData;
        drumIdLookup.Clear();

        List<(float time, int index, DrumsLoadData data)> allDrums = new();
        foreach (var time in timeDict.Keys)
        {
            var list = timeDict[time];
            for (int i = 0; i < list.Count; i++)
            {
                drumIdLookup[list[i]] = drumIdLookup.Count + 1;
                allDrums.Add((time, i, list[i]));
            }
        }

        Dictionary<string, int> timeTextCount = new();
        foreach (var d in allDrums)
        {
            string timeStr = Math.Round(d.time, 2).ToString("0.00");
            if (!timeTextCount.ContainsKey(timeStr)) timeTextCount[timeStr] = 0;
            timeTextCount[timeStr]++;
        }

        HashSet<string> duplicatedTimeStrings = new();
        foreach (var kv in timeTextCount)
        {
            if (kv.Value > 1)
                duplicatedTimeStrings.Add(kv.Key);
        }

        allDrums.Sort((a, b) =>
        {
            int result = 0;

            if (typeSortEnabled)
            {
                result = GetTypeOrder(a.data.DrwmsData.DtheTypeOfOperation)
                         .CompareTo(GetTypeOrder(b.data.DrwmsData.DtheTypeOfOperation));
                if (result != 0) return result;
            }

            if (currentPrimaryMode == SortMode.ByTime)
            {
                result = a.time.CompareTo(b.time);
                if (result == 0)
                    result = drumIdLookup[a.data].CompareTo(drumIdLookup[b.data]);
            }
            else
            {
                result = drumIdLookup[a.data].CompareTo(drumIdLookup[b.data]);
            }

            return result;
        });

        foreach (var d in allDrums)
        {
            float roundedTime = (float)Math.Round(d.time, 2);
            string timeStr = roundedTime.ToString("0.00");

            var go = Instantiate(ItemPrefab, ContentRoot);
            var row = new DrumRow(go, drumIdLookup[d.data], roundedTime, d.data.DrwmsData.DtheTypeOfOperation);

            float localTime = roundedTime;
            int localIndex = d.index;

            row.Init(
                onDelete: () => row.SetPendingDelete(true),
                onUndo: () => row.SetPendingDelete(false),
                onConfirm: () =>
                {
                    this.SendCommand(new RemoveAudioEditTimeLineDataCommand(localTime, localIndex));
                    RefreshList();
                }
            );

            if (duplicatedTimeStrings.Contains(timeStr))
            {
                row.MarkAsDuplicatedTime();
            }

            rows.Add(row);
        }

        if (editModel.Mode != SystemModeData.PlayMode)
        {
            OnTimeNeedCheck(new OnUpdateThisTime() { ThisTime = editModel.ThisTime });
        }
    }

    void OnTimeNeedCheck(OnUpdateThisTime evt)
    {
        if (editModel == null || editModel.Mode == SystemModeData.PlayMode) return;

        string currentTime = evt.ThisTime.ToString("0.00");

        foreach (var row in rows)
        {
            row.SetHighlighted(row.GetTimeTextValue() == currentTime);
        }
    }

    class DrumRow
    {
        public GameObject GameObject;
        TMP_Text IndexText, TimeText, TypeText;
        Button ActionButton, UndoButton, ConfirmButton;

        bool isDuplicatedTime = false;
        bool isDeleted = false;
        bool isHighlighted = false;

        public DrumRow(GameObject go, int index, float time, TheTypeOfOperation type)
        {
            GameObject = go;

            IndexText = go.transform.Find("Index")?.GetComponent<TMP_Text>();
            TimeText = go.transform.Find("Time")?.GetComponent<TMP_Text>();
            TypeText = go.transform.Find("Type")?.GetComponent<TMP_Text>();

            ActionButton = go.transform.Find("DeleteBtn")?.GetComponent<Button>();
            UndoButton = go.transform.Find("UndoBtn")?.GetComponent<Button>();
            ConfirmButton = go.transform.Find("ConfirmBtn")?.GetComponent<Button>();

            IndexText.text = index.ToString("D3");
            TimeText.text = time.ToString("0.00");
            TypeText.text = ConvertType(type);

            UpdateTextColor();

            ActionButton?.gameObject.SetActive(true);
            UndoButton?.gameObject.SetActive(false);
            ConfirmButton?.gameObject.SetActive(false);

            var button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
                if (timeHand != null)
                    timeHand.SetTime(time, true);
            });
        }

        public void Init(Action onDelete, Action onUndo, Action onConfirm)
        {
            ActionButton?.onClick.AddListener(() =>
            {
                isDeleted = true;
                onDelete?.Invoke();
                UpdateTextColor();
            });

            UndoButton?.onClick.AddListener(() =>
            {
                isDeleted = false;
                onUndo?.Invoke();
                UpdateTextColor();
            });

            ConfirmButton?.onClick.AddListener(() =>
            {
                isDeleted = false;
                onConfirm?.Invoke();
            });
        }

        public void SetPendingDelete(bool pending)
        {
            isDeleted = pending;
            ActionButton?.gameObject.SetActive(!pending);
            UndoButton?.gameObject.SetActive(pending);
            ConfirmButton?.gameObject.SetActive(pending);
            UpdateTextColor();
        }

        public void MarkAsDuplicatedTime()
        {
            isDuplicatedTime = true;
            UpdateTextColor();
        }

        public void SetHighlighted(bool highlight)
        {
            isHighlighted = highlight;
            UpdateTextColor();
        }

        void UpdateTextColor()
        {
            if (isHighlighted)
            {
                IndexText.color = Color.green;
                TimeText.color = Color.green;
                TypeText.color = Color.green;
                return;
            }

            Color color = isDeleted ? Color.red :
                           isDuplicatedTime ? Color.yellow :
                           Color.white;

            IndexText.color = color;
            TimeText.color = color;
            TypeText.color = color;
        }

        public string GetTimeTextValue() => TimeText.text;

        string ConvertType(TheTypeOfOperation op) => op switch
        {
            TheTypeOfOperation.Click => "单击",
            TheTypeOfOperation.SwipeUp => "上滑",
            TheTypeOfOperation.SwipeDown => "下滑",
            TheTypeOfOperation.SwipeLeft => "左滑",
            TheTypeOfOperation.SwipeRight => "右滑",
            _ => "未知"
        };
    }

    public void SortByIndex()
    {
        currentPrimaryMode = SortMode.ByIndex;
        RefreshList();
    }

    public void SortByTime()
    {
        currentPrimaryMode = SortMode.ByTime;
        RefreshList();
    }

    public void ToggleTypeSort()
    {
        typeSortEnabled = !typeSortEnabled;
        RefreshList();
    }

    int GetTypeOrder(TheTypeOfOperation op) => op switch
    {
        TheTypeOfOperation.Click => 0,
        TheTypeOfOperation.SwipeLeft => 1,
        TheTypeOfOperation.SwipeRight => 2,
        TheTypeOfOperation.SwipeUp => 3,
        TheTypeOfOperation.SwipeDown => 4,
        _ => 99
    };
    // #redgin // [跳转到最近鼓点] -- mixyao/07/04
    public void MoveToNearestDrum()
    {
        if (editModel == null || editModel.TimeLineData == null || editModel.TimeLineData.Count == 0)
            return;

        float now = editModel.ThisTime;
        float? nearest = null;

        foreach (var time in editModel.TimeLineData.Keys)
        {
            if (nearest == null || Mathf.Abs(time - now) < Mathf.Abs(nearest.Value - now))
            {
                nearest = time;
            }
        }

        if (nearest.HasValue)
        {
            var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
            if (timeHand != null)
            {
                timeHand.SetTime((float)Math.Round(nearest.Value, 2), true);
            }
        }
    }

    // #redgin // [跳转到上一个鼓点] -- mixyao/07/04
    public void MoveToPreviousDrum()
    {
        if (editModel == null || editModel.TimeLineData == null || editModel.TimeLineData.Count == 0)
            return;

        float now = editModel.ThisTime;
        float? previous = null;

        foreach (var time in editModel.TimeLineData.Keys)
        {
            if (time < now && (!previous.HasValue || time > previous.Value))
            {
                previous = time;
            }
        }

        if (previous.HasValue)
        {
            var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
            if (timeHand != null)
                timeHand.SetTime((float)Math.Round(previous.Value, 2), true);
        }
    }

    // #redgin // [跳转到下一个鼓点] -- mixyao/07/04
    public void MoveToNextDrum()
    {
        if (editModel == null || editModel.TimeLineData == null || editModel.TimeLineData.Count == 0)
            return;

        float now = editModel.ThisTime;
        float? next = null;

        foreach (var time in editModel.TimeLineData.Keys)
        {
            if (time > now && (!next.HasValue || time < next.Value))
            {
                next = time;
            }
        }

        if (next.HasValue)
        {
            var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
            if (timeHand != null)
                timeHand.SetTime((float)Math.Round(next.Value, 2), true);
        }
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
}
