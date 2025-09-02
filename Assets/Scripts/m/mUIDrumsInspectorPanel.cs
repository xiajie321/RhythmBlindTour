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
using System.Globalization;

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

    // 脱离选中模式：不自动回选、不高亮
    private bool _noSelectionMode = false;
    // 用于拍点落在边界时，向右推进一点点，避免 1.00 被判成上一个拍
    [SerializeField] private float boundaryEpsilonSec = 1e-4f;
    [SerializeField] private bool _navigateByTip = false; // true: 用Tip；false: 用Center


    void Start()
    {
        StartCoroutine(InitEditModelIfNeeded());
        this.RegisterEvent<OnUpdateThisTime>(OnTimeNeedCheck)
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // 监听鼓点唯一高亮事件
        this.RegisterEvent<UIAudioEditDrumsOrbit.OnSelectDrumByCode>(OnDrumCodeSelected)
            .UnRegisterWhenGameObjectDestroyed(gameObject);


        // ★ 监听“导航锚点模式”变更（来自 Orbit）
        this.RegisterEvent<UIAudioEditDrumsOrbit.OnPlacementNavigationModeChanged>(e =>
        {
            _navigateByTip = e.UseTipForNavigation;
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

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
        if (ItemPrefab == null)
        {
            Debug.LogError("[ItemPrefab] 组件未就绪，刷新失败");
            return;
        }
        if (ContentRoot == null)
        {
            Debug.LogError("[ContentRoot] 组件未就绪，刷新失败");
            return;
        }
        if (editModel == null)
        {
            Debug.LogError("[editModel] 组件未就绪，刷新失败");
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

                _noSelectionMode = false;   // ← 点任何行＝重新进入“可选中”模式
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
        this.SendEvent(new OnInspectorListReady());

    }
    public struct OnInspectorListReady { }


    // 只高亮唯一 DrumCode 条目
    void OnTimeNeedCheck(OnUpdateThisTime evt)
    {
        if (editModel == null || editModel.Mode == SystemModeData.PlayMode) return;

        string currentTime = evt.ThisTime.ToString("0.00");
        // 仅在未选中情况下自动选一个（用于首次高亮），不再发事件
        if (string.IsNullOrEmpty(selectedDrumCode) && !_noSelectionMode)
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
        if (_noSelectionMode) return;  // ← 在“脱离模式”里忽略任何外部选中事件
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
        if (editModel == null) return;
        var items = BuildNavItems();
        if (items.Count == 0) return;

        float now = (float)Math.Round(editModel.ThisTime, 2);
        NavItem? best = null;
        float bestDist = float.MaxValue;

        foreach (var it in items)
        {
            float t = (float)Math.Round(_navigateByTip ? it.Tip : it.Center, 2);
            float dist = Mathf.Abs(t - now);
            if (dist < bestDist) { bestDist = dist; best = it; }
        }
        if (!best.HasValue) return;

        float target = (float)Math.Round(_navigateByTip ? best.Value.Tip : best.Value.Center, 2);

        var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
        if (timeHand != null) timeHand.SetTime(target, true);
        else this.SendEvent(new OnUpdateThisTime { ThisTime = target });

        // 高亮：直接用 DrumCode 唯一事件，不再依赖“时间字符串”匹配行
        _noSelectionMode = false;
        selectedDrumCode = best.Value.Code;
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode { DrumCode = selectedDrumCode });

        TryPreviewSucceedSound(selectedDrumCode);
    }

    public void MoveToPreviousDrum()
    {
        if (editModel == null) return;
        var items = BuildNavItems();
        if (items.Count == 0) return;

        float now = (float)Math.Round(editModel.ThisTime, 2);

        // 取目标时间并排序
        var ordered = items
            .Select(it => new
            {
                t = (float)Math.Round(_navigateByTip ? it.Tip : it.Center, 2),
                code = it.Code,
                center = it.Center,
                tip = it.Tip
            })
            .OrderBy(x => x.t)
            .ToList();

        // 找到严格小于 now 的最大 t
        var cand = ordered.Where(x => x.t < now).LastOrDefault();
        if (cand == null)
            cand = ordered.LastOrDefault(); // 如果没有更小的，就跳到最后一个（循环式）

        if (cand == null) return;

        float target = cand.t;
        var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
        if (timeHand != null) timeHand.SetTime(target, true);
        else this.SendEvent(new OnUpdateThisTime { ThisTime = target });

        _noSelectionMode = false;
        selectedDrumCode = cand.code;
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode { DrumCode = selectedDrumCode });

        TryPreviewSucceedSound(selectedDrumCode);
    }

    public void MoveToNextDrum()
    {
        if (editModel == null) return;
        var items = BuildNavItems();
        if (items.Count == 0) return;

        float now = (float)Math.Round(editModel.ThisTime, 2);

        var ordered = items
            .Select(it => new
            {
                t = (float)Math.Round(_navigateByTip ? it.Tip : it.Center, 2),
                code = it.Code,
                center = it.Center,
                tip = it.Tip
            })
            .OrderBy(x => x.t)
            .ToList();

        // 找到严格大于 now 的最小 t
        var cand = ordered.FirstOrDefault(x => x.t > now);
        if (cand == null)
            cand = ordered.FirstOrDefault(); // 若没有更大的，从头循环

        if (cand == null) return;

        float target = cand.t;
        var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
        if (timeHand != null) timeHand.SetTime(target, true);
        else this.SendEvent(new OnUpdateThisTime { ThisTime = target });

        _noSelectionMode = false;
        selectedDrumCode = cand.code;
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode { DrumCode = selectedDrumCode });

        TryPreviewSucceedSound(selectedDrumCode);
    }



    // 在同一ThisTime下获取所有鼓点DrumCode和typeIndex
    List<(string DrumCode, int TypeIndex)> GetDrumCodesInCurrentTime(string timeStr)
    {
        return rows
            .Where(r => r.GetTimeTextValue() == timeStr)
            .Select(r => (r.DrumCode, int.Parse(r.DrumCode.Substring(r.DrumCode.Length - 1))))
            .ToList();
    }

    // 顺序切换鼓点（下一个）——按当前导航锚点(_navigateByTip)在“同一时间组”内循环
    public void SelectNextDrumInCurrentTime()
    {
        if (_noSelectionMode || editModel == null) return;

        // 当前时间（按 0.00 对齐）
        float now = (float)Math.Round(editModel.ThisTime, 2);

        // 用导航锚点构建“同一时间”的分组：Tip 模式按提示点时间分组；Center 模式按判定点时间分组
        var items = BuildNavItems();
        var group = items
            .Select(it =>
            {
                float anchor = (float)Math.Round(_navigateByTip ? it.Tip : it.Center, 2);
                int typeIdxFromCode = 0;
                if (!string.IsNullOrEmpty(it.Code))
                {
                    var last = it.Code[it.Code.Length - 1];
                    int.TryParse(last.ToString(), out typeIdxFromCode);
                }
                return new { code = it.Code, anchor, typeIdx = typeIdxFromCode };
            })
            .Where(x => Mathf.Abs(x.anchor - now) <= 1e-3f) // 同一 ThisTime 组
            .OrderBy(x => Array.IndexOf(typeOrder, x.typeIdx)) // 按你的 typeOrder 排序
            .ToList();

        if (group.Count == 0) return;

        // 计算下一条（支持当前未选中时从首条开始）
        int curIdx = group.FindIndex(g => g.code == selectedDrumCode);
        int nextIdx = (curIdx < 0) ? 0 : (curIdx + 1) % group.Count;

        selectedDrumCode = group[nextIdx].code;

        // 保守地把时间针对齐到该锚点（一般与 now 相同，避免因四舍五入漂移）
        float target = group[nextIdx].anchor;
        var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
        if (timeHand != null) timeHand.SetTime(target, true);
        else this.SendEvent(new OnUpdateThisTime { ThisTime = target });

        // 刷新高亮 & 广播唯一 DrumCode 事件 & 预览声音
        OnTimeNeedCheck(new OnUpdateThisTime { ThisTime = target });
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode { DrumCode = selectedDrumCode });
        TryPreviewSucceedSound(selectedDrumCode);
    }
    // 逆序切换鼓点（上一个）——按当前导航锚点(_navigateByTip)在“同一时间组”内循环
    public void SelectPrevDrumInCurrentTime()
    {
        if (_noSelectionMode || editModel == null) return;

        float now = (float)Math.Round(editModel.ThisTime, 2);

        var items = BuildNavItems();
        var group = items
            .Select(it =>
            {
                float anchor = (float)Math.Round(_navigateByTip ? it.Tip : it.Center, 2);
                int typeIdxFromCode = 0;
                if (!string.IsNullOrEmpty(it.Code))
                {
                    var last = it.Code[it.Code.Length - 1];
                    int.TryParse(last.ToString(), out typeIdxFromCode);
                }
                return new { code = it.Code, anchor, typeIdx = typeIdxFromCode };
            })
            .Where(x => Mathf.Abs(x.anchor - now) <= 1e-3f)
            .OrderBy(x => Array.IndexOf(typeOrder, x.typeIdx))
            .ToList();

        if (group.Count == 0) return;

        int curIdx = group.FindIndex(g => g.code == selectedDrumCode);
        int prevIdx = (curIdx < 0) ? (group.Count - 1) : (curIdx - 1 + group.Count) % group.Count;

        selectedDrumCode = group[prevIdx].code;

        float target = group[prevIdx].anchor;
        var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
        if (timeHand != null) timeHand.SetTime(target, true);
        else this.SendEvent(new OnUpdateThisTime { ThisTime = target });

        OnTimeNeedCheck(new OnUpdateThisTime { ThisTime = target });
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode { DrumCode = selectedDrumCode });
        TryPreviewSucceedSound(selectedDrumCode);
    }

    void TriggerCurrentRowClick(float targetTime)
    {
        if (_noSelectionMode) return;

        string timeStr = targetTime.ToString("0.00");
        var drumList = GetDrumCodesInCurrentTime(timeStr);

        if (drumList.Count == 0) return;

        var ordered = drumList.OrderBy(d => Array.IndexOf(typeOrder, d.TypeIndex)).ToList();
        selectedDrumCode = ordered[0].DrumCode;
        OnTimeNeedCheck(new OnUpdateThisTime() { ThisTime = targetTime });

        // #redgin 同步唯一 DrumCode 全局事件（强制让属性面板、Orbit等刷新！）-- 2024-07-14
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode() { DrumCode = selectedDrumCode });
    }
    // 统一预览：按当前导航锚点(_navigateByTip)选择“提示音/回答音”
    void TryPreviewSucceedSound(string drumCode)
    {
        if (string.IsNullOrEmpty(drumCode)) return;

        var editModel = this.GetModel<AudioEditModel>();
        DrumsLoadData drum = UIAttributeSetPanel.FindDrumByCode(editModel, drumCode);
        if (drum == null || drum.DrwmsData == null) return;

        // ★关键：Tip 导航就播提示音；Center 导航播回答音
        string clipPath = _navigateByTip
            ? drum.DrwmsData.FPreAdventAudioClipPath   // 提示音路径
            : drum.DrwmsData.FSucceedAudioClipPath;    // 回答音路径

        var cModel = this.GetModel<DataCachingModel>();
        var clip = cModel.GetAudioClip(clipPath);

        var op = drum.DrwmsData.DtheTypeOfOperation;
        if (clip != null)
            AudioEditManager.Instance.PlayVFXWithFallback(op, clip, 1f);
    }



    internal void ClearAll()
    {
        RefreshList();
    }
    #region TTS for Inspector Rows

    // 朗读循环状态
    private string _ttsLastTimeStr = null;
    private int _ttsStep = 0; // 0 -> 数量句；1 -> 类型清单；2 -> 数量句

    // 类型顺序与名称（按你指定的 4,0,1,2,3）
    private static readonly int[] TTS_ORDER = new[] { 4, 0, 1, 2, 3 };
    private static readonly Dictionary<int, string> TTS_TYPE_NAME = new()
{
    {4, "点击"},
    {0, "上滑"},
    {1, "下滑"},
    {2, "左滑"},
    {3, "右滑"},
};

    /// <summary>
    /// 朗读：无高亮→“ThisTime + 数量 + 类型清单”；有高亮→“CenterTime + 当前选中{类型}”
    /// 不再做循环步进，仅按当前状态读一条。
    /// </summary>
    public void TTS_ReadValue()
    {
        if (editModel == null)
        {
            try { editModel = this.GetModel<AudioEditModel>(); }
            catch { return; }
        }

        // ========== 分支1：有唯一选中（且非“脱离选中模式”） -> 读选中 ==========
        if (!_noSelectionMode && !string.IsNullOrEmpty(selectedDrumCode))
        {
            // 统一以“当前时间(编辑针)”作为朗读时间口径
            float now = (float)System.Math.Round(editModel.ThisTime, 2);
            string secText = now.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            string beatText = FormatMeasureBeatText(now, out bool okBeat);
            if (!okBeat) beatText = string.Empty;

            var drum = UIAttributeSetPanel.FindDrumByCode(editModel, selectedDrumCode);
            string typeCN = drum != null ? OpToSpeakCN(drum.DrwmsData.DtheTypeOfOperation) : "鼓点";

            // 例：第4小节第1拍，17.00秒，当前选中左滑鼓点
            string prefix = string.IsNullOrEmpty(beatText) ? $"{secText}秒" : $"{beatText}，{secText}秒";
            mTTS.Speak($"{prefix}，当前选中{typeCN}");
            return;
        }

        // ========== 分支2：无选中 -> 读当前时间的统计 ==========
        string timeStr = editModel.ThisTime.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var drumList = GetDrumCodesInCurrentTime(timeStr);
        int count = drumList.Count;

        float now2 = (float)System.Math.Round(editModel.ThisTime, 2);
        string secText2 = now2.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        string beatText2 = FormatMeasureBeatText(now2, out bool okBeat2);
        if (!okBeat2) beatText2 = string.Empty;

        // 例：第4小节第1拍，17.00秒，当前时间存在1个鼓点
        string header = string.IsNullOrEmpty(beatText2) ? $"{secText2}秒" : $"{beatText2}，{secText2}秒";
        mTTS.Speak($"{header}，当前时间存在{count}个鼓点");
    }


    /// <summary>朗读高亮鼓点的详细信息：{类型}，判定点{ThisTime}秒，提示点{Tip}秒。</summary>
    /// <summary>朗读高亮鼓点的详细信息：{类型}，判定点{ThisTime}秒，提示点{Tip}秒。</summary>
    private bool TrySpeakSelectedDrumDetail(string drumCode)
    {
        var em = editModel ?? this.GetModel<AudioEditModel>();
        var drum = UIAttributeSetPanel.FindDrumByCode(em, drumCode);
        if (drum == null) return false;

        // 类型中文
        string typeCN = OpToSpeakCN(drum.DrwmsData.DtheTypeOfOperation);

        // 判定点：由 DrumCode 解码
        DecodeDrumCode(drumCode, out float judgeTime, out _);

        // 提示点：CenterTime - VPreAdventAudioClipOffsetTime
        float tip = GetTipTime(drum);

        string judgeTxt = mTTS.FormatSecondsForTTS(judgeTime, mTTS.TimeReadStyle.NumericTokens);
        string tipTxt = mTTS.FormatSecondsForTTS(tip, mTTS.TimeReadStyle.NumericTokens);

        // 示例：点击鼓点，判定点10点50秒，提示点10点00秒。
        // （TTS 通常会读为“十点五零秒 / 十点零零秒”）
        mTTS.Speak($"{typeCN}，判定点{judgeTxt}秒，提示点{tipTxt}秒。");
        return true;
    }


    private static string OpToSpeakCN(TheTypeOfOperation op) => op switch
    {
        TheTypeOfOperation.Click => "点击鼓点",
        TheTypeOfOperation.SwipeLeft => "左滑鼓点",
        TheTypeOfOperation.SwipeRight => "右滑鼓点",
        TheTypeOfOperation.SwipeUp => "上滑鼓点",
        TheTypeOfOperation.SwipeDown => "下滑鼓点",
        _ => "未知鼓点"
    };

    /// <summary>
    /// 通过反射尝试读取 float 字段/属性（兼容不同数据结构版本）。
    /// </summary>
    private static float TryGetFloatViaReflection(object obj, string name, float fallback)
    {
        if (obj == null) return fallback;
        var t = obj.GetType();

        // 字段
        var fi = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fi != null && fi.FieldType == typeof(float))
            return (float)fi.GetValue(obj);

        // 属性
        var pi = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (pi != null && pi.PropertyType == typeof(float))
            return (float)pi.GetValue(obj, null);

        return fallback;
    }
    // Tip = CenterTime - VPreAdventAudioClipOffsetTime
    private static float GetTipTime(Qf.ClassDatas.AudioEdit.DrumsLoadData drum)
    {
        if (drum == null || drum.DrwmsData == null) return 0f;
        float center = drum.DrwmsData.CenterTime;
        float offset = drum.DrwmsData.VPreAdventAudioClipOffsetTime;
        // 可选：防止出现负数（若业务允许为负可去掉这行）
        return Mathf.Max(0f, center - offset);
    }

    #endregion

    /// <summary>
    /// 脱离选中：清除当前高亮，并禁止自动回选；同时重置 TTS 循环。
    /// </summary>
    public void ExitSelectionHighlight()
    {
        _noSelectionMode = true;           // 进入“脱离选中模式”
        selectedDrumCode = null;           // 清掉当前选中
        foreach (var r in rows) r.SetHighlighted(false);  // 取消所有高亮

        // 关键：立刻按当前 ThisTime 同步一遍“无高亮”到 UI（避免后续事件的残留）
        if (editModel != null)
            OnTimeNeedCheck(new OnUpdateThisTime { ThisTime = editModel.ThisTime });
    }
    /// <summary>
    /// 将 seconds 按 60/BPM*(4/BeatB) 量化为 “第{小节}小节第{拍}拍” 文本。
    /// 采用半开区间 [起点, 下一拍起点)，并在边界 +epsilon 归到后一拍。
    /// </summary>
    private string FormatMeasureBeatText(float seconds, out bool ok)
    {
        ok = false;
        var m = editModel ?? this.GetModel<AudioEditModel>();
        if (m == null || m.BPM <= 0 || m.BeatA <= 0 || m.BeatB <= 0) return string.Empty;

        float beatDuration = 60f / m.BPM * (4f / m.BeatB);
        if (beatDuration <= 0f) return string.Empty;

        float t = Mathf.Max(0f, seconds + Mathf.Max(0f, boundaryEpsilonSec));  // 半开区间，边界右推
        int totalBeatIndex = Mathf.FloorToInt(t / beatDuration);               // 全局拍序(0基)
        int measureIndex = totalBeatIndex / m.BeatA;                          // 小节索引(0基)
        int beatInMeasure = totalBeatIndex % m.BeatA;                          // 小节内拍(0基)

        ok = true;
        return $"第{(measureIndex + 1)}小节第{(beatInMeasure + 1)}拍";
    }

    // 放在类里任意位置
    private struct NavItem
    {
        public float Center;   // 判定点（isTip=false）
        public float Tip;      // 提示点（isTip=true）
        public string Code;    // DrumCode
    }

    // 从模型构建导航列表（一次调用时现算，足够快）
    private List<NavItem> BuildNavItems()
    {
        var res = new List<NavItem>();
        var dict = editModel?.TimeLineData;
        if (dict == null) return res;

        foreach (var kv in dict)
        {
            float centerKey = kv.Key;                // 你的字典键就是“中心时间”
            foreach (var d in kv.Value)
            {
                if (d?.DrwmsData == null) continue;
                float center = d.DrwmsData.CenterTime;
                float tip = GetTipTime(d);           // 已有工具：Center - VPreAdventAudioClipOffsetTime
                res.Add(new NavItem { Center = center, Tip = tip, Code = d.DrwmsData.DrumCode });
            }
        }
        return res;
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
}
