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
using Qf.Models;
using System.Linq;

public class mUIDrumsInspectorPanel : MonoBehaviour, IController
{
    public GameObject ItemPrefab;
    public Transform ContentRoot;
    private AudioEditModel editModel;
    private List<DrumRow> rows = new();
    private Dictionary<DrumsLoadData, int> drumIdLookup = new();

    // 唯一选中鼓点的 DrumCode
    private string selectedDrumCode = null;
    readonly int[] typeOrder = { 4, 2, 3, 0, 1 };

    public enum SortMode { ByIndex, ByTime }
    public SortMode currentPrimaryMode = SortMode.ByIndex;
    public bool typeSortEnabled = false;

    void Start()
    {
        StartCoroutine(InitEditModelIfNeeded());
        this.RegisterEvent<OnUpdateThisTime>(OnTimeNeedCheck)
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // 监听鼓点唯一高亮事件
        this.RegisterEvent<UIAudioEditDrumsOrbit.OnSelectDrumByCode>(OnDrumCodeSelected)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    IEnumerator InitEditModelIfNeeded()
    {
        yield return new WaitUntil(() => GameBody.Interface != null);

        while (editModel == null)
        {
            try { editModel = this.GetModel<AudioEditModel>(); }
            catch { }
            yield return null;
        }
    }

    public void EnsureReadyAndRefresh()
    {
        if (editModel == null)
        {
            try { editModel = this.GetModel<AudioEditModel>(); }
            catch { Debug.LogError("[mUIDrumsInspectorPanel] editModel 获取失败"); return; }
        }

        if (editModel != null) RefreshList();
    }

    public void RefreshList()
    {
        if (AudioEditManager.Instance != null && AudioEditManager.Instance.IsControlRunning) return;
        if (ItemPrefab == null || ContentRoot == null || editModel == null)
        {
            Debug.LogError("[mUIDrumsInspectorPanel] 组件未就绪，刷新失败");
            return;
        }
        foreach (var row in rows) Destroy(row.GameObject);
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
            if (kv.Value > 1) duplicatedTimeStrings.Add(kv.Key);

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
                if (result == 0) result = drumIdLookup[a.data].CompareTo(drumIdLookup[b.data]);
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
            string drumCode = d.data.DrwmsData.DrumCode;

            var go = Instantiate(ItemPrefab, ContentRoot);
            var row = new DrumRow(go, drumIdLookup[d.data], roundedTime, d.data.DrwmsData.DtheTypeOfOperation, drumCode);

            float localTime = roundedTime;
            int localIndex = d.index;

            row.Init(
                onDelete: () => row.SetPendingDelete(true),
                onUndo: () => row.SetPendingDelete(false),
                onConfirm: () => row.DeleteThisDrum()
            );

            if (duplicatedTimeStrings.Contains(timeStr))
            {
                row.MarkAsDuplicatedTime();
            }

            // 鼓点条目被点击时，唯一选中并发事件
            var button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
                if (timeHand != null)
                    timeHand.SetTime(roundedTime, true);

                selectedDrumCode = drumCode;
                GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode() { DrumCode = drumCode });
                OnTimeNeedCheck(new OnUpdateThisTime() { ThisTime = roundedTime });
                row.TryPreviewSucceedSound(drumCode);
            });

            rows.Add(row);
        }

        if (editModel.Mode != SystemModeData.PlayMode)
        {
            OnTimeNeedCheck(new OnUpdateThisTime() { ThisTime = editModel.ThisTime });
        }
    }

    // 只高亮唯一 DrumCode 条目
    void OnTimeNeedCheck(OnUpdateThisTime evt)
    {
        if (editModel == null || editModel.Mode == SystemModeData.PlayMode) return;

        string currentTime = evt.ThisTime.ToString("0.00");
        // 仅在未选中情况下自动选一个（用于首次高亮），不再发事件
        if (string.IsNullOrEmpty(selectedDrumCode))
        {
            var drumRows = rows.Where(r => r.GetTimeTextValue() == currentTime).ToList();
            if (drumRows.Any())
            {
                selectedDrumCode = drumRows
                    .OrderBy(r => Array.IndexOf(typeOrder, int.Parse(r.DrumCode.Substring(r.DrumCode.Length - 1))))
                    .First().DrumCode;
            }
        }

        foreach (var row in rows)
            row.SetHighlighted(row.DrumCode == selectedDrumCode);

        // 不再这里重复发送 DrumCode 事件，避免死循环
    }


    // 监听唯一高亮事件
    private void OnDrumCodeSelected(UIAudioEditDrumsOrbit.OnSelectDrumByCode evt)
    {
        if (selectedDrumCode == evt.DrumCode) return;
        selectedDrumCode = evt.DrumCode;
        DecodeDrumCode(selectedDrumCode, out float selectTime, out int typeIndex);
        // 只刷新自己高亮，绝不再发事件
        OnTimeNeedCheck(new OnUpdateThisTime { ThisTime = selectTime });
    }




    // DrumCode拆解工具
    public static void DecodeDrumCode(string drumCode, out float thisTime, out int typeIndex)
    {
        thisTime = 0f;
        typeIndex = 0;
        if (string.IsNullOrEmpty(drumCode) || drumCode.Length < 6) return;
        string timeStr = drumCode.Substring(0, 5);
        if (int.TryParse(timeStr, out int timeInt)) thisTime = timeInt / 100f;
        string idxStr = drumCode.Substring(drumCode.Length - 1, 1);
        int.TryParse(idxStr, out typeIndex);
    }

    public void RemoveCurrentSelectedDrumByDrumCode(string drumCode)
    {
        if (editModel == null || string.IsNullOrEmpty(drumCode))
            return;
        DecodeDrumCode(drumCode, out float time, out int index);
        this.SendCommand(new RemoveAudioEditTimeLineDataCommand(time, index));
        RefreshList();
    }

    class DrumRow
    {
        public GameObject GameObject;
        TMP_Text IndexText, TimeText, TypeText;
        Button ActionButton, UndoButton, ConfirmButton;

        public string DrumCode;

        public DrumRow(GameObject go, int index, float time, TheTypeOfOperation type, string drumCode)
        {
            GameObject = go;
            DrumCode = drumCode;
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
        }

        public void Init(Action onDelete, Action onUndo, Action onConfirm)
        {
            ActionButton?.onClick.AddListener(() =>
            {
                onDelete?.Invoke();
                UpdateTextColor();
            });
            UndoButton?.onClick.AddListener(() =>
            {
                onUndo?.Invoke();
                UpdateTextColor();
            });
            ConfirmButton?.onClick.AddListener(() =>
            {
                onConfirm?.Invoke();
            });
        }

        public void DeleteThisDrum()
        {
            var panel = GameObject.FindObjectOfType<mUIDrumsInspectorPanel>();
            if (panel != null && !string.IsNullOrEmpty(DrumCode))
                panel.RemoveCurrentSelectedDrumByDrumCode(DrumCode);
        }

        public void SetPendingDelete(bool pending)
        {
            ActionButton?.gameObject.SetActive(!pending);
            UndoButton?.gameObject.SetActive(pending);
            ConfirmButton?.gameObject.SetActive(pending);
            UpdateTextColor();
        }
        public void MarkAsDuplicatedTime() { UpdateTextColor(); }
        public void SetHighlighted(bool highlight)
        {
            isHighlighted = highlight;
            UpdateTextColor();
        }
        bool isHighlighted = false;
        void UpdateTextColor()
        {
            Color color = Color.white;
            if (isHighlighted)
                color = Color.green;
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
        public void TryPreviewSucceedSound(string drumCode)
        {
            var editModel = GameBody.Interface.GetModel<AudioEditModel>();
            var drum = UIAttributeSetPanel.FindDrumByCode(editModel, drumCode);
            if (drum == null) return;

            var cModel = GameBody.Interface.GetModel<DataCachingModel>();
            var clip = cModel.GetAudioClip(drum.DrwmsData.FSucceedAudioClipPath);

            var op = drum.DrwmsData.DtheTypeOfOperation;
            AudioEditManager.Instance.PlayVFXWithFallback(op, clip, 1f);
        }
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

    public void MoveToNearestDrum()
    {
        if (editModel == null || editModel.TimeLineData == null || editModel.TimeLineData.Count == 0)
            return;

        float now = (float)Math.Round(editModel.ThisTime, 2);
        float? nearest = null;

        foreach (var time in editModel.TimeLineData.Keys)
        {
            float roundedTime = (float)Math.Round(time, 2);
            if (nearest == null || Mathf.Abs(roundedTime - now) < Mathf.Abs(nearest.Value - now))
            {
                nearest = roundedTime;
            }
        }

        if (nearest.HasValue)
        {
            var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
            if (timeHand != null)
            {
                timeHand.SetTime(nearest.Value, true);
                OnTimeNeedCheck(new OnUpdateThisTime() { ThisTime = nearest.Value });
                TriggerCurrentRowClick(nearest.Value);
                TryPreviewSucceedSound(selectedDrumCode);

            }
        }
    }

    public void MoveToPreviousDrum()
    {
        if (editModel == null || editModel.TimeLineData == null || editModel.TimeLineData.Count == 0)
            return;

        float now = (float)Math.Round(editModel.ThisTime, 2);
        float? previous = null;

        foreach (var time in editModel.TimeLineData.Keys)
        {
            float roundedTime = (float)Math.Round(time, 2);
            if (roundedTime < now && (!previous.HasValue || roundedTime > previous.Value))
            {
                previous = roundedTime;
            }
        }

        if (previous.HasValue)
        {
            var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
            if (timeHand != null)
            {
                timeHand.SetTime(previous.Value, true);
                OnTimeNeedCheck(new OnUpdateThisTime() { ThisTime = previous.Value });
                TriggerCurrentRowClick(previous.Value);
                TryPreviewSucceedSound(selectedDrumCode);

            }
        }

    }

    public void MoveToNextDrum()
    {
        if (editModel == null || editModel.TimeLineData == null || editModel.TimeLineData.Count == 0)
            return;

        var times = editModel.TimeLineData.Keys
            .Select(t => (float)Math.Round(t, 2, MidpointRounding.ToEven))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        float now = (float)Math.Round(editModel.ThisTime, 2, MidpointRounding.ToEven);

        // 找到第一个严格大于 now 的鼓点时间
        var next = times.SkipWhile(t => t <= now).FirstOrDefault();

        // 注意 FirstOrDefault 如果找不到是0，要额外判断
        if (next > 0 || (next == 0 && times.Count > 0 && times[0] > now))
        {
            var nextTime = next > now ? next : times.FirstOrDefault(t => t > now);
            if (nextTime > now)
            {
                var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
                if (timeHand != null)
                {
                    timeHand.SetTime(nextTime, true);
                    OnTimeNeedCheck(new OnUpdateThisTime { ThisTime = nextTime });
                    TriggerCurrentRowClick(nextTime);
                    TryPreviewSucceedSound(selectedDrumCode);

                }
            }
        }

    }


    // 在同一ThisTime下获取所有鼓点DrumCode和typeIndex
    List<(string DrumCode, int TypeIndex)> GetDrumCodesInCurrentTime(string timeStr)
    {
        return rows
            .Where(r => r.GetTimeTextValue() == timeStr)
            .Select(r => (r.DrumCode, int.Parse(r.DrumCode.Substring(r.DrumCode.Length - 1))))
            .ToList();
    }

    // 顺序切换鼓点（下一个）
    public void SelectNextDrumInCurrentTime()
    {
        string timeStr = editModel.ThisTime.ToString("0.00");
        var drumList = GetDrumCodesInCurrentTime(timeStr);

        if (drumList.Count == 0) return;

        var ordered = drumList.OrderBy(d => Array.IndexOf(typeOrder, d.TypeIndex)).ToList();

        int idx = ordered.FindIndex(d => d.DrumCode == selectedDrumCode);
        int nextIdx = (idx + 1) % ordered.Count;
        selectedDrumCode = ordered[nextIdx].DrumCode;

        OnTimeNeedCheck(new OnUpdateThisTime { ThisTime = editModel.ThisTime });

        // 【核心补充】同步 DrumCode 唯一事件
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode() { DrumCode = selectedDrumCode });
        TryPreviewSucceedSound(selectedDrumCode);

    }

    // 逆序切换鼓点（上一个）
    public void SelectPrevDrumInCurrentTime()
    {
        string timeStr = editModel.ThisTime.ToString("0.00");
        var drumList = GetDrumCodesInCurrentTime(timeStr);

        if (drumList.Count == 0) return;

        var ordered = drumList.OrderBy(d => Array.IndexOf(typeOrder, d.TypeIndex)).ToList();

        int idx = ordered.FindIndex(d => d.DrumCode == selectedDrumCode);
        int prevIdx = (idx - 1 + ordered.Count) % ordered.Count;
        selectedDrumCode = ordered[prevIdx].DrumCode;

        OnTimeNeedCheck(new OnUpdateThisTime { ThisTime = editModel.ThisTime });

        // 【核心补充】同步 DrumCode 唯一事件
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode() { DrumCode = selectedDrumCode });
        TryPreviewSucceedSound(selectedDrumCode);

    }
    void TriggerCurrentRowClick(float targetTime)
    {
        string timeStr = targetTime.ToString("0.00");
        var drumList = GetDrumCodesInCurrentTime(timeStr);

        if (drumList.Count == 0) return;

        var ordered = drumList.OrderBy(d => Array.IndexOf(typeOrder, d.TypeIndex)).ToList();
        selectedDrumCode = ordered[0].DrumCode;
        OnTimeNeedCheck(new OnUpdateThisTime() { ThisTime = targetTime });

        // #redgin 同步唯一 DrumCode 全局事件（强制让属性面板、Orbit等刷新！）-- 2024-07-14
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode() { DrumCode = selectedDrumCode });
    }
    void TryPreviewSucceedSound(string drumCode)
    {
        if (string.IsNullOrEmpty(drumCode)) return;
        var editModel = this.GetModel<AudioEditModel>();
        DrumsLoadData drum = UIAttributeSetPanel.FindDrumByCode(editModel, drumCode);
        if (drum == null) return;
        var cModel = this.GetModel<DataCachingModel>();
        var clip = cModel.GetAudioClip(drum.DrwmsData.FSucceedAudioClipPath);

        var op = drum.DrwmsData.DtheTypeOfOperation;
        if (clip != null)
            AudioEditManager.Instance.PlayVFXWithFallback(op, clip, 1f);
    }


    internal void ClearAll()
    {
        RefreshList();
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
}
