using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GUI;

public class mUIAttributeSelectList : MonoBehaviour
{
    // ───────── 统一步长（集中配置） ─────────
    [Header("步长管理（统一配置）")]
    [SerializeField] private float valueStepVertical = 0.01f;
    [SerializeField] private float valueStepHorizontal = 0.10f;
    [SerializeField] private int valueDecimals = 2;
    [SerializeField] private float sliderStepVertical = 0.01f;
    [SerializeField] private float sliderStepHorizontal = 0.10f;

    // ───────── 一级：Group（每组含自身 Targets） ─────────
    [System.Serializable]
    public class GroupDef
    {
        public string name;
        public RectTransform topAnchor;
        public List<GameObject> targets = new();
    }

    [Header("一级：Group 列表")]
    public List<GroupDef> groups = new();

    [Header("切换策略")]
    [SerializeField] private bool loopGroups = true;
    [SerializeField] private bool loopItems = true;
    [SerializeField] private bool skipInactive = true;
    [SerializeField] private bool autoRepairOnNavigate = true;

    [Header("滚动（同一个 ScrollRect）")]
    [SerializeField] private ScrollRect scrollRectOverride;
    [SerializeField] private float smoothDuration = 0.12f;

    // ───────── 第三级：资源选择（FileAttribute 用） ─────────
    [Header("第三级（资源选择）")]
    [SerializeField] private UIResourceSelectList resourceList;
    [SerializeField] private GameObject resourcePanelRoot;
    [SerializeField] private bool openResourceWithInternal = true;

    [Header("资源浏览偏好")]
    [Tooltip("进入/切换第三级时：true=朗读Name；false=直接试听")]
    [SerializeField] private bool resourceNavigateSpeakElsePreview = true;
    [Tooltip("是否允许“聚焦(悬停)自动试听”；默认关闭（被动试听）")]
    [SerializeField] private bool resourceEnableHoverPreview = false;
    // ───── 帮助提示（按 Z 进入编辑/替换时） ─────
    [Header("帮助提示（按Z进入编辑时）")]
    [SerializeField] private bool enableHelpHints = false;
    [SerializeField, TextArea]
    private string hintForSlider = "滑动条，请用方向键调整音量大小，后按Z键退出。";
    [SerializeField, TextArea]
    private string hintForInput = "输入框，请用方向键调整数值大小，或直接输入，输入后请按回车键确定，最后按Z键退出";
    [SerializeField, TextArea]
    private string hintForFile = "音频替换，请用方向键选择需要替换的音频，连续两次按下\\键可预览音频，按Z后确定，按X键返回。";

    // ───────── 条目封装 ─────────
    class Entry
    {
        public GameObject go;
        public UIFileAttribute fileAttr;
        public UIValueAttribute valueAttr;
        public UISliderAttribute sliderAttr;

        public Entry(GameObject go) { this.go = go; Rebind(); }
        public void Rebind()
        {
            if (!go) { fileAttr = null; valueAttr = null; sliderAttr = null; return; }
            fileAttr = go.GetComponent<UIFileAttribute>();
            valueAttr = go.GetComponent<UIValueAttribute>();
            sliderAttr = go.GetComponent<UISliderAttribute>();
        }

        public bool IsValid => go && (fileAttr || valueAttr || sliderAttr);
        public bool IsUsable(bool skip) => IsValid && (!skip || go.activeInHierarchy);

        public void ApplyCentralSteps(float vSmall, float vLarge, int vDecimals, float sSmall, float sLarge)
        {
            if (valueAttr) { valueAttr.SetSteps(vSmall, vLarge); valueAttr.SetDecimals(vDecimals); }
            if (sliderAttr) sliderAttr.SetSteps(sSmall, sLarge);
        }

        public void EnterInput()
        {
            // 仅负责让具体控件进入编辑态，
            // 资源面板/Scope 切换/提示播报 都放到外层处理
            if (valueAttr) valueAttr.EnterInput();
            else if (sliderAttr) sliderAttr.EnterInput();
            else if (fileAttr) fileAttr.EnterInput();
        }

        public void ConfirmInput()
        {
            if (valueAttr) valueAttr.ConfirmInput();
            else if (sliderAttr) sliderAttr.ConfirmInput();
        }

        public void CancelInput()
        {
            if (valueAttr) valueAttr.CancelInput();      // 还原
            else if (sliderAttr) sliderAttr.ExitInput(); // 不还原
        }

        public void NudgeVertical(int dir)
        {
            if (valueAttr && valueAttr.IsEditing) valueAttr.NudgeVertical(dir);
            else if (sliderAttr && sliderAttr.IsEditing) sliderAttr.NudgeVertical(dir);
        }

        public void NudgeHorizontal(int dir)
        {
            if (valueAttr && valueAttr.IsEditing) valueAttr.NudgeHorizontal(dir);
            else if (sliderAttr && sliderAttr.IsEditing) sliderAttr.NudgeHorizontal(dir);
        }

        public bool IsEditing =>
            (fileAttr && fileAttr.EnterSelected) ||
            (valueAttr && valueAttr.IsEditing) ||
            (sliderAttr && sliderAttr.IsEditing);

        public void ResetEnterSelected()
        {
            if (fileAttr) fileAttr.ResetEnterSelected();
            if (valueAttr) valueAttr.ResetEnterSelected();
            if (sliderAttr) sliderAttr.ResetEnterSelected();
        }

        public void SimulateSelect()
        {
            if (EventSystem.current == null || !go) return;
            var data = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute<IPointerClickHandler>(go, data, ExecuteEvents.pointerClickHandler);
            EventSystem.current.SetSelectedGameObject(go);
        }

        public RectTransform RT => go ? go.GetComponent<RectTransform>() : null;
    }

    private readonly List<List<Entry>> _entriesPerGroup = new();

    public enum NavScope { Group = 0, Item = 1, Input = 2, Resource = 3 }
    public NavScope Scope { get; private set; } = NavScope.Group;

    public int CurrentGroup { get; private set; } = -1;
    public int CurrentItem { get; private set; } = -1;

    void OnEnable()
    {
        RebuildBindings();
        if (groups.Count > 0)
        {
            // 静默选到第 0 组（不朗读）
            SelectGroup(0, alignTop: true, speak: false);
        }
    }

    public void RebuildBindings()
    {
        _entriesPerGroup.Clear();
        foreach (var g in groups)
        {
            var list = new List<Entry>();
            foreach (var go in g.targets) list.Add(new Entry(go));
            _entriesPerGroup.Add(list);
        }
        Scope = NavScope.Group;
        CurrentGroup = Mathf.Clamp(CurrentGroup, -1, groups.Count - 1);
        CurrentItem = -1;
    }

    // 第三级显隐
    GameObject ResourceGO => resourcePanelRoot ? resourcePanelRoot : (resourceList ? resourceList.gameObject : null);

    void OpenResourcePicker()
    {
        if (!resourceList) return;

        // 同步“悬停试听开关”
        UIResourceItem.SetHoverPreviewEnabled(resourceEnableHoverPreview);

        if (ResourceGO) ResourceGO.SetActive(true);
        if (openResourceWithInternal) resourceList.ShowInternalResources();

        // 进入第三级先确保选中第 0 个资源
        if (resourceList.UIItems != null && resourceList.UIItems.Count > 0)
        {
            resourceList.SelectIndex(0, simulateHover: true);
        }

        // 进入第三级时按偏好立即执行一次（读名/试听）
        DoResourceNavigateAction();
    }

    void CloseResourcePicker()
    {
        if (ResourceGO) ResourceGO.SetActive(false);
    }

    // 外部输入对接（六个方法）

    // 外部输入对接（六个方法）
    public void EnterInput()
    {
        if (Scope == NavScope.Group)
        {
            // 进入二级：自动选中第 0 个条目并朗读（SetScope 内已处理）
            SetScope((int)NavScope.Item);
            return;
        }

        if (Scope == NavScope.Item)
        {
            var e = GetEntry(CurrentGroup, CurrentItem);
            if (e == null || !e.IsUsable(skipInactive)) return;

            // File：进入第三级（资源面板）
            if (e.fileAttr)
            {
                e.fileAttr.EnterInput();
                OpenResourcePicker();
                Scope = NavScope.Resource;

                // ★ 进入资源选择后播“音频替换”提示
                TTS_ReadHint(hintForFile);

                return;
            }

            // 进入编辑前，下发统一步长
            e.ApplyCentralSteps(
                valueStepVertical,
                valueStepHorizontal,
                valueDecimals,
                sliderStepVertical,
                sliderStepHorizontal
            );

            // 进入输入态
            e.EnterInput();
            Scope = NavScope.Input;

            // ★ 根据控件类型播“输入框 / 滑动条”提示
            if (e.valueAttr) TTS_ReadHint(hintForInput);
            else if (e.sliderAttr) TTS_ReadHint(hintForSlider);

            return;
        }

        if (Scope == NavScope.Input)
        {
            // 确认编辑：朗读 “变量名 + 值(秒/百分比等)”
            var e = GetEntry(CurrentGroup, CurrentItem);
            e?.ConfirmInput();
            e?.ResetEnterSelected();

            string varName = GetCurrentItemName();
            string valText = BuildCurrentValueSpeech();
            if (!string.IsNullOrEmpty(varName) || !string.IsNullOrEmpty(valText))
            {
                string combo =
                    string.IsNullOrEmpty(varName) ? valText :
                    string.IsNullOrEmpty(valText) ? varName :
                    (varName + "，" + valText);
                TTS_ReadName(combo);
            }

            Scope = NavScope.Item;
            return;
        }

        if (Scope == NavScope.Resource)
        {
            // 第三级确认：应用选择并返回二级后，朗读二级当前项名称
            resourceList?.ConfirmSelection();
            CloseResourcePicker();

            string itemName = GetCurrentItemName();
            if (!string.IsNullOrEmpty(itemName)) TTS_ReadName(itemName);

            Scope = NavScope.Item;
            return;
        }
    }


    public void ExitInput()
    {
        if (Scope == NavScope.Input)
        {
            var e = GetEntry(CurrentGroup, CurrentItem);

            // 取消编辑（Value 会还原；Slider 只是退出）
            e?.CancelInput();
            e?.ResetEnterSelected();

            // 组合播报：变量名 + 值(秒)
            string varName = GetCurrentItemName();
            string valText = BuildCurrentValueSpeech(); // 已加“秒”
            if (!string.IsNullOrEmpty(varName) || !string.IsNullOrEmpty(valText))
            {
                string combo = string.IsNullOrEmpty(varName) ? valText :
                               string.IsNullOrEmpty(valText) ? varName :
                               (varName + " " + valText);
                TTS_ReadName(combo);
            }

            Scope = NavScope.Item;
            return;
        }

        if (Scope == NavScope.Item)
        {
            // 二级返回一级：朗读当前 Group 名
            if (CurrentGroup >= 0 && CurrentGroup < groups.Count)
            {
                string gname = groups[CurrentGroup].name;
                if (!string.IsNullOrEmpty(gname)) TTS_ReadName(gname);
            }
            Scope = NavScope.Group;
            return;
        }

        if (Scope == NavScope.Resource)
        {
            // 第三级直接返回：不确认修改，朗读二级当前项名称
            resourceList?.BackWithoutConfirm();
            CloseResourcePicker();

            string itemName = GetCurrentItemName();
            if (!string.IsNullOrEmpty(itemName)) TTS_ReadName(itemName);

            Scope = NavScope.Item;
            return;
        }
    }

    // 四方向键：编辑态步进；非编辑态在同级切换
    public void OnKeyUp() { if (Scope == NavScope.Input) { GetEntry(CurrentGroup, CurrentItem)?.NudgeVertical(+1); } else MoveSelectionVertical(-1); }
    public void OnKeyDown() { if (Scope == NavScope.Input) { GetEntry(CurrentGroup, CurrentItem)?.NudgeVertical(-1); } else MoveSelectionVertical(+1); }
    public void OnKeyLeft() { if (Scope == NavScope.Input) { GetEntry(CurrentGroup, CurrentItem)?.NudgeHorizontal(-1); } else MoveSelectionVertical(-1); }
    public void OnKeyRight() { if (Scope == NavScope.Input) { GetEntry(CurrentGroup, CurrentItem)?.NudgeHorizontal(+1); } else MoveSelectionVertical(+1); }

    // 手动朗读：在第三级时改为调用资源列表的“读名-试听-读名”循环
    public void TTS_ReadValue()
    {
        if (Scope == NavScope.Resource)
        {
            resourceList?.CycleNamePreviewName();
            return;
        }

        if (Scope == NavScope.Group)
        {
            string all = BuildGroupValueSpeech(); // 这里仍可保留你原有的聚合读法
            if (!string.IsNullOrEmpty(all)) mTTS.Speak(all);
            return;
        }

        // —— 二级（当前条目） —— //
        var e = GetEntry(CurrentGroup, CurrentItem);
        if (e == null || e.go == null) return;

        // ★ File：按“空值→读‘变量名+值名’/试听循环”的新规则处理
        if (e.fileAttr)
        {
            // 取显示出来的“文件名”（Nr 文本或 ShowFileName）
            var nr = e.go.GetComponentsInChildren<TMPro.TMP_Text>(true)
                         .FirstOrDefault(t => t.gameObject.name == "Nr");
            string valueName = nr ? nr.text : null;

            // 规则1（更新）：空值 → 只朗读“标题/变量名”
            if (string.IsNullOrEmpty(valueName))
            {
                string itemName = GetCurrentItemName(); // 例如“关卡音乐音频”
                if (!string.IsNullOrEmpty(itemName)) TTS_ReadName(itemName);
                return;
            }

            // 规则2：有值 → 第一次读“变量名 + ， + 值名”，第二次试听，之后循环
            int step = 0;
            if (_fileReadCycle.TryGetValue(e.go, out var old)) step = old;

            if (step == 0)
            {
                string itemName = GetCurrentItemName(); // e.g. “关卡音乐音频”
                string combo = string.IsNullOrEmpty(itemName) ? valueName : $"{itemName}，{valueName}";
                TTS_ReadName(combo);
            }
            else
            {
                // 试听（见第2步在 UIFileAttribute 里补一个 PreviewCurrent()）
                e.fileAttr.PreviewCurrent();
            }

            _fileReadCycle[e.go] = (step + 1) % 2;
            return;
        }


        // 其他类型维持原行为：只读当前值
        string v = BuildCurrentValueSpeech();
        if (!string.IsNullOrEmpty(v)) mTTS.Speak(v);
    }


    // 构造：当前 Group 内每个条目的 “Name,Value.” 用半角符号拼接成一整句
    string BuildGroupValueSpeech()
    {
        if (CurrentGroup < 0 || CurrentGroup >= _entriesPerGroup.Count) return null;
        var list = _entriesPerGroup[CurrentGroup];
        if (list == null || list.Count == 0) return null;

        System.Text.StringBuilder sb = new System.Text.StringBuilder(128);

        foreach (var e in list)
        {
            if (e == null || !e.IsUsable(skipInactive) || e.go == null) continue;

            string name = ResolveItemDisplayName(e.go);
            string val = BuildValueSpeechForEntry(e);          // ← 这里已给 InputField 值加“秒”
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(val)) continue;

            if (string.IsNullOrEmpty(name)) name = "";
            if (string.IsNullOrEmpty(val)) val = "";//未设置

            sb.Append(name).Append(',').Append(val).Append('.');
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    // 按控件类型生成“值”的朗读文本；
    // Value(InputField)：在此处统一添加“秒”
    string BuildValueSpeechForEntry(Entry e)
    {
        if (e == null || e.go == null) return null;

        // Value（InputField）→ 加“秒”
        if (e.valueAttr != null)
        {
            var tmp = e.go.GetComponentsInChildren<TMP_InputField>(true).FirstOrDefault();
            string txt = tmp ? tmp.text : null;
            if (string.IsNullOrEmpty(txt)) return "";//未设置
            return txt.EndsWith("秒") ? txt : (txt + "秒");
        }

        // Slider（百分比）
        if (e.sliderAttr != null)
        {
            var sld = e.go.GetComponentsInChildren<Slider>(true).FirstOrDefault();
            if (!sld) return "";//未设置
            int p = Mathf.RoundToInt(Mathf.Clamp01(sld.value) * 100f);
            return $"百分之{p}";
        }

        // File（取名为 “Nr” 的 TMP_Text）
        if (e.fileAttr != null)
        {
            var nr = e.go.GetComponentsInChildren<TMP_Text>(true)
                         .FirstOrDefault(t => t.gameObject.name == "Nr");
            string txt = nr ? nr.text : null;
            return string.IsNullOrEmpty(txt) ? "" : txt;//未设置
        }

        return "";//未设置
    }

    // 当前选中条目“值”的朗读文本（用于二级/编辑态）
    string BuildCurrentValueSpeech()
    {
        var e = GetEntry(CurrentGroup, CurrentItem);
        if (e == null || e.go == null) return null;

        // Value（InputField）→ 加“秒”
        if (e.valueAttr != null)
        {
            var tmp = e.go.GetComponentsInChildren<TMP_InputField>(true).FirstOrDefault();
            if (!tmp) return null;
            var txt = tmp.text;
            if (string.IsNullOrEmpty(txt)) return null;
            return txt.EndsWith("秒") ? txt : (txt + "秒");
        }

        // Slider（百分比）
        if (e.sliderAttr != null)
        {
            var sld = e.go.GetComponentsInChildren<Slider>(true).FirstOrDefault();
            if (sld)
            {
                int p = Mathf.RoundToInt(Mathf.Clamp01(sld.value) * 100f);
                return $"百分之{p}";
            }
        }

        // File
        if (e.fileAttr != null)
        {
            var nr = e.go.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.gameObject.name == "Nr");
            if (nr) return nr.text;
        }
        return null;
    }

    // 解析条目显示名：优先子物体里名为 "Name" 的 TMP_Text；否则 GameObject.name
    string ResolveItemDisplayName(GameObject go)
    {
        if (!go) return null;
        var nameTMP = go.GetComponentsInChildren<TMP_Text>(true)
                        .FirstOrDefault(t => t.gameObject.name == "Name");
        if (nameTMP && !string.IsNullOrEmpty(nameTMP.text)) return nameTMP.text;
        return go.name;
    }

    // ———— 二级/一级切换与滚动定位（保持不变） ————

    public void SetScope(int scope)
    {
        scope = Mathf.Clamp(scope, 0, 3);
        var newScope = (NavScope)scope;
        if (Scope == NavScope.Input && newScope != NavScope.Input) return;
        Scope = newScope;

        if (Scope == NavScope.Item)
        {
            if (CurrentGroup < 0 && groups.Count > 0) CurrentGroup = 0;
            if (CurrentItem < 0) CurrentItem = FirstValidItem(CurrentGroup);
            if (CurrentItem >= 0) SelectItem(CurrentItem, adjustDir: 0);
        }
    }

    void MoveSelectionVertical(int direction)
    {
        if (direction == 0) return;

        if (Scope == NavScope.Resource)
        {
            resourceList?.MoveSelectionVertical(direction);
            DoResourceNavigateAction();
            return;
        }

        if (Scope == NavScope.Group)
        {
            int next = NextGroup(CurrentGroup, direction);
            if (next >= 0) SelectGroup(next, alignTop: true);
        }
        else if (Scope == NavScope.Item)
        {
            int next = NextItem(CurrentGroup, CurrentItem, direction);
            if (next >= 0) SelectItem(next, adjustDir: direction);
        }
    }

    void DoResourceNavigateAction()
    {
        if (!resourceList) return;
        if (resourceNavigateSpeakElsePreview) resourceList.ReadNameOnce();
        else resourceList.PreviewOnce();
    }

    public void SelectGroup(int groupIndex, bool alignTop, bool speak = true)
    {
        if (groups.Count == 0) return;
        groupIndex = Mathf.Clamp(groupIndex, 0, groups.Count - 1);

        CurrentGroup = groupIndex;
        CurrentItem = -1;
        Scope = NavScope.Group;

        if (alignTop && groups[groupIndex].topAnchor)
            EnsureTopAligned(groups[groupIndex].topAnchor, smoothDuration);

        if (speak && !string.IsNullOrEmpty(groups[groupIndex].name))
            TTS_ReadName(groups[groupIndex].name);
    }

    public void SelectItem(int itemIndex, int adjustDir)
    {
        if (CurrentGroup < 0 || CurrentGroup >= _entriesPerGroup.Count) return;
        var list = _entriesPerGroup[CurrentGroup];
        if (list == null || list.Count == 0) return;

        itemIndex = Mathf.Clamp(itemIndex, 0, list.Count - 1);
        var e = list[itemIndex];

        if (autoRepairOnNavigate && (e.fileAttr == null && e.valueAttr == null && e.sliderAttr == null)) e.Rebind();
        if (!e.IsUsable(skipInactive)) return;

        int firstIdx = FirstValidItem(CurrentGroup);
        int lastIdx = LastValidItem(CurrentGroup);

        CurrentItem = itemIndex;

        // ★ 切到新的 File 条目时，重置循环计数（只保留这一处）
        if (e != null && e.fileAttr != null)
            _fileReadCycle[e.go] = 0;

        e.SimulateSelect();

        var sr = GetScrollRect(); if (!sr) return;
        RectTransform topAnchor = groups[CurrentGroup].topAnchor;
        RectTransform lastRT = FindLastVisibleRT(CurrentGroup);
        RectTransform selRT = e.RT;

        bool wrappedDownToTop = (adjustDir > 0) && (itemIndex == firstIdx) && (firstIdx != -1) && (lastIdx != -1);
        bool wrappedUpToBottom = (adjustDir < 0) && (itemIndex == lastIdx) && (firstIdx != -1) && (lastIdx != -1);

        if (wrappedDownToTop) { if (topAnchor) EnsureTopAligned(topAnchor, smoothDuration); }
        else if (wrappedUpToBottom) { if (lastRT) EnsureBottomAligned(lastRT, smoothDuration); }
        else
        {
            if (itemIndex == firstIdx && topAnchor) EnsureTopAligned(topAnchor, smoothDuration);
            else if (itemIndex == lastIdx && lastRT) EnsureBottomAligned(lastRT, smoothDuration);
            else EnsureCenteredBounded(topAnchor, lastRT, selRT, smoothDuration);
        }

        // 进入条目后播报：变量名 + 值（值里你的 BuildCurrentValueSpeech 已加“秒/百分比”）
        string varName = GetCurrentItemName();
        string valText = BuildCurrentValueSpeech();

        // ★ 统一用中文逗号
        string combo =
            string.IsNullOrEmpty(varName) ? valText :
            string.IsNullOrEmpty(valText) ? varName :
            (varName + "，" + valText);

        if (!string.IsNullOrEmpty(combo)) TTS_ReadName(combo);
    }

    public bool GetCurrentEnterSelected()
    {
        var e = GetEntry(CurrentGroup, CurrentItem);
        return e != null && e.IsEditing;
    }
    public void ResetCurrentEnterSelected() => GetEntry(CurrentGroup, CurrentItem)?.ResetEnterSelected();

    Entry GetEntry(int g, int i)
    {
        if (g < 0 || g >= _entriesPerGroup.Count) return null;
        var list = _entriesPerGroup[g];
        if (list == null || i < 0 || i >= list.Count) return null;
        return list[i];
    }

    int FirstValidItem(int g)
    {
        if (g < 0 || g >= _entriesPerGroup.Count) return -1;
        var list = _entriesPerGroup[g]; if (list == null) return -1;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (autoRepairOnNavigate && (e.fileAttr == null && e.valueAttr == null && e.sliderAttr == null)) e.Rebind();
            if (e.IsUsable(skipInactive)) return i;
        }
        return -1;
    }
    int LastValidItem(int g)
    {
        if (g < 0 || g >= _entriesPerGroup.Count) return -1;
        var list = _entriesPerGroup[g]; if (list == null) return -1;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var e = list[i];
            if (autoRepairOnNavigate && (e.fileAttr == null && e.valueAttr == null && e.sliderAttr == null)) e.Rebind();
            if (e.IsUsable(skipInactive)) return i;
        }
        return -1;
    }
    int NextItem(int g, int start, int step)
    {
        if (g < 0 || g >= _entriesPerGroup.Count) return -1;
        var list = _entriesPerGroup[g]; if (list == null || list.Count == 0) return -1;

        int i = start;
        if (i < 0 || i >= list.Count) i = step > 0 ? -1 : list.Count;

        int count = list.Count;
        for (int k = 0; k < count; k++)
        {
            i += step;
            if (loopItems) { if (i < 0) i = count - 1; if (i >= count) i = 0; }
            else { if (i < 0 || i >= count) return -1; }

            var e = list[i];
            if (autoRepairOnNavigate && (e.fileAttr == null && e.valueAttr == null && e.sliderAttr == null)) e.Rebind();
            if (e.IsUsable(skipInactive)) return i;
        }
        return -1;
    }
    int NextGroup(int start, int step)
    {
        int n = groups.Count; if (n == 0) return -1;
        int i = start; for (int k = 0; k < n; k++)
        {
            i += step;
            if (loopGroups) { if (i < 0) i = n - 1; if (i >= n) i = 0; }
            else { if (i < 0 || i >= n) return -1; }
            return i;
        }
        return -1;
    }

    ScrollRect GetScrollRect() => scrollRectOverride ? scrollRectOverride : GetComponentInParent<ScrollRect>();
    RectTransform GetViewport(ScrollRect sr) => sr.viewport ? sr.viewport : sr.GetComponent<RectTransform>();

    void EnsureTopAligned(RectTransform anchor, float dur)
    {
        var sr = GetScrollRect(); if (!sr || !sr.content || !anchor) return;
        float range = sr.content.rect.height - GetViewport(sr).rect.height; if (range <= 0.0001f) return;
        float top = ViewTopFromAnchorTop(sr, anchor); SetVNP(sr, Mathf.Clamp(top, 0f, range), range, dur);
    }
    void EnsureBottomAligned(RectTransform lastItem, float dur)
    {
        var sr = GetScrollRect(); if (!sr || !sr.content || !lastItem) return;
        var content = sr.content; var viewport = GetViewport(sr);
        float range = content.rect.height - viewport.rect.height; if (range <= 0.0001f) return;
        float contentTopY = content.rect.height * (1f - content.pivot.y);
        var lastB = RectTransformUtility.CalculateRelativeRectTransformBounds(content, lastItem);
        float lastBottomFromTop = contentTopY - lastB.min.y;
        float viewTop = Mathf.Clamp(lastBottomFromTop - viewport.rect.height, 0f, range);
        SetVNP(sr, viewTop, range, dur);
    }
    void EnsureCenteredBounded(RectTransform topAnchor, RectTransform lastItem, RectTransform selected, float dur)
    {
        var sr = GetScrollRect(); if (!sr || !sr.content || !selected) return;
        var content = sr.content; var viewport = GetViewport(sr);
        float range = content.rect.height - viewport.rect.height; if (range <= 0.0001f) return;

        float contentTopY = content.rect.height * (1f - content.pivot.y);
        var selB = RectTransformUtility.CalculateRelativeRectTransformBounds(content, selected);
        float selCenterFromTop = contentTopY - selB.center.y;

        float viewMin = 0f;
        if (topAnchor)
        {
            var topB = RectTransformUtility.CalculateRelativeRectTransformBounds(content, topAnchor);
            float anchorTopFromTop = contentTopY - topB.max.y;
            viewMin = Mathf.Clamp(anchorTopFromTop, 0f, range);
        }

        float viewMax = range;
        if (lastItem)
        {
            var lastB = RectTransformUtility.CalculateRelativeRectTransformBounds(content, lastItem);
            float lastBottomFromTop = contentTopY - lastB.min.y;
            viewMax = Mathf.Clamp(lastBottomFromTop - viewport.rect.height, 0f, range);
        }
        if (viewMax < viewMin) viewMax = viewMin;

        float desiredTop = selCenterFromTop - viewport.rect.height * 0.5f;
        float clampTop = Mathf.Clamp(desiredTop, viewMin, viewMax);
        SetVNP(sr, clampTop, range, dur);
    }
    float ViewTopFromAnchorTop(ScrollRect sr, RectTransform anchor)
    {
        var content = sr.content;
        float contentTopY = content.rect.height * (1f - content.pivot.y);
        var b = RectTransformUtility.CalculateRelativeRectTransformBounds(content, anchor);
        return contentTopY - b.max.y;
    }
    void SetVNP(ScrollRect sr, float viewTop, float range, float dur)
    {
        float vnp = 1f - (viewTop / Mathf.Max(0.0001f, range));
        if (dur > 0f) StartCoroutine(SmoothVNP(sr, vnp, dur));
        else sr.verticalNormalizedPosition = vnp;
    }
    System.Collections.IEnumerator SmoothVNP(ScrollRect sr, float target, float dur)
    {
        float start = sr.verticalNormalizedPosition, t = 0f; dur = Mathf.Max(0.0001f, dur);
        while (t < 1f) { t += Time.unscaledDeltaTime / dur; sr.verticalNormalizedPosition = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
        sr.verticalNormalizedPosition = target;
    }
    RectTransform FindLastVisibleRT(int g)
    {
        if (g < 0 || g >= _entriesPerGroup.Count) return null;
        var list = _entriesPerGroup[g]; if (list == null) return null;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var e = list[i]; if (autoRepairOnNavigate && (e.fileAttr == null && e.valueAttr == null && e.sliderAttr == null)) e.Rebind();
            if (!e.go) continue; if (skipInactive && !e.go.activeInHierarchy) continue;
            return e.RT;
        }
        return null;
    }

    // 供外部调用：进入系统时，选中并朗读第 0 个 Group 的名字
    public void EnterSelect()
    {
        if (groups == null || groups.Count == 0) return;

        SelectGroup(0, alignTop: true, speak: false);
        Scope = NavScope.Group;
        CurrentItem = -1; // 不选条目
    }

    // —— TTS：自动读名（私有）、取当前条目名 ——
    void TTS_ReadName(string text) { if (!string.IsNullOrEmpty(text)) mTTS.Speak(text); }

    string GetCurrentItemName()
    {
        var e = GetEntry(CurrentGroup, CurrentItem);
        return e != null && e.go ? e.go.name : null;
    }
    void TTS_ReadHint(string text)
    {
        if (!enableHelpHints) return;
        if (mTTSManager.Instance != null && !mTTSManager.Instance.TTSEnabled) return;
        if (!string.IsNullOrEmpty(text)) mTTS.Speak(text);
    }

    // 在 class mUIAttributeSelectList 内部增加
    // 记录每个 File 条目的循环索引：0=读Name，1=试听
    private readonly Dictionary<GameObject, int> _fileReadCycle = new();

}
