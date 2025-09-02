// File: mBeatBPMSetList.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

// 与模型/事件交互
using QFramework;
using Qf.Managers;
using Qf.Models.AudioEdit;
using Qf.Commands.AudioEdit;
using Qf.Events;

public class mBeatBPMSetList : MonoBehaviour, IController
{
    #region Inspector

    [Header("Time Signature / BPM")]
    [SerializeField] private TMP_Dropdown denominatorDropdown; // 分母（4、8、…）
    [SerializeField] private TMP_Dropdown numeratorDropdown;   // 每小节拍数（3、4、7…）
    [SerializeField] private TMP_InputField bpmInput;          // BPM（默认首选）

    [Header("视觉放大（DOTween）")]
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float scaleDuration = 0.12f;
    [SerializeField] private Ease scaleEase = Ease.OutQuad;

    [Header("循环策略")]
    [SerializeField] private bool loopItems = true;    // 三项循环
    [SerializeField] private bool loopOptions = true;  // 下拉选项循环

    [Header("TTS（朗读名称）")]
    [SerializeField] private string ttsNameBPM = "BPM，";
    [SerializeField] private string ttsNameDenominator = "分母，";
    [SerializeField] private string ttsNameNumerator = "每小节拍数，";

    [Header("帮助提示（按Z进入编辑时）")]
    [SerializeField] private bool enableHelpHints = false;
    [SerializeField, TextArea]
    private string hintForDropdown = "选项切换，请用方向键切换后按Z确定。";
    [SerializeField, TextArea]
    private string hintForInput = "输入框，请用方向键调整数值大小，或直接输入，输入后请按回车键确定，最后按Z键退出";

    [Header("BPM 步长设置")]
    [SerializeField] private int bpmStepVertical = 10;   // 上/下
    [SerializeField] private int bpmStepHorizontal = 1;  // 左/右
    [SerializeField] private int bpmMin = 1;             // <=0 表不限制
    [SerializeField] private int bpmMax = 400;           // <=0 表不限制

    #endregion

    private enum Scope { Item, EditDropdown, EditBPM }
    private Scope scope = Scope.Item;

    private readonly List<GameObject> visualList = new();
    private int currentIndex = -1;

    private int prevDropdownValue = -1;
    private string bpmBeforeEdit = "";

    private readonly Dictionary<Transform, Vector3> originalScale = new();

    // —— 新增：UI 同步 & 事件绑定控制 —— //
    private bool _isSyncingUI = false;
    private bool _eventsBound = false;

    #region Lifecycle

    private void OnEnable()
    {
        RebuildVisualList();
        currentIndex = Mathf.Clamp(0, 0, visualList.Count - 1);
        ApplySelectVisual(currentIndex, tween: true);
        scope = Scope.Item;

        if (bpmInput)
            bpmInput.onEndEdit.AddListener(OnBPMEndEdit);

        // 启用时先把“当前模型值”回填到 UI（防止初次显示就是旧值）
        SyncUIFromModel();

        // 只绑定一次事件（避免多次 OnEnable 产生重复订阅）
        if (!_eventsBound)
        {
            this.RegisterEvent<AudioEditModelLoad>(_ => SyncUIFromModel())
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<BPMChangeValue>(e =>
            {
                if (_isSyncingUI) return;
                if (bpmInput) bpmInput.SetTextWithoutNotify(e.BPM.ToString());
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            _eventsBound = true;
        }
    }

    private void OnDisable()
    {
        RestoreAllScales();
        if (bpmInput)
            bpmInput.onEndEdit.RemoveListener(OnBPMEndEdit);
        // 事件在 Destroy 时统一由 UnRegisterWhenGameObjectDestroyed 清理
    }

    #endregion

    #region Public Navigation

    public void EnterSelect()
    {
        if (visualList.Count == 0)
        {
            RebuildVisualList();
            if (visualList.Count == 0) return;
        }
        currentIndex = 0;
        scope = Scope.Item;
        ApplySelectVisual(currentIndex, tween: false);
    }

    public void OnKeyUp()
    {
        if (scope == Scope.Item)
        {
            MoveItem(-1);
            TTS_ReadCurrentNameValue();
        }
        else if (scope == Scope.EditDropdown)
        {
            StepDropdown(+1);
            ApplyDropdownToModel();
        }
        else if (scope == Scope.EditBPM)
        {
            NudgeBPM(+bpmStepVertical);
        }
    }

    public void OnKeyDown()
    {
        if (scope == Scope.Item)
        {
            MoveItem(+1);
            TTS_ReadCurrentNameValue();
        }
        else if (scope == Scope.EditDropdown)
        {
            StepDropdown(-1);
            ApplyDropdownToModel();
        }
        else if (scope == Scope.EditBPM)
        {
            NudgeBPM(-bpmStepVertical);
        }
    }

    public void OnKeyLeft()
    {
        if (scope == Scope.Item)
        {
            MoveItem(-1);
            TTS_ReadCurrentNameValue();
        }
        else if (scope == Scope.EditDropdown)
        {
            StepDropdown(-1);
            ApplyDropdownToModel();
        }
        else if (scope == Scope.EditBPM)
        {
            NudgeBPM(-bpmStepHorizontal);
        }
    }

    public void OnKeyRight()
    {
        if (scope == Scope.Item)
        {
            MoveItem(+1);
            TTS_ReadCurrentNameValue();
        }
        else if (scope == Scope.EditDropdown)
        {
            StepDropdown(+1);
            ApplyDropdownToModel();
        }
        else if (scope == Scope.EditBPM)
        {
            NudgeBPM(+bpmStepHorizontal);
        }
    }

    public void EnterInput()
    {
        if (scope == Scope.Item)
        {
            var kind = GetCurrentKind();
            if (kind == Kind.BPM && bpmInput)
            {
                scope = Scope.EditBPM;
                bpmBeforeEdit = bpmInput.text;
                bpmInput.ActivateInputField();
                TTS_ReadHint(hintForInput);
                return;
            }

            var dd = GetCurrentDropdown();
            if (dd != null)
            {
                prevDropdownValue = dd.value;
                scope = Scope.EditDropdown;
                TTS_ReadHint(hintForDropdown);
                return;
            }
            return;
        }

        // 确认编辑
        if (scope == Scope.EditDropdown)
        {
            scope = Scope.Item;
            TTS_ReadBeatCombined();
            ApplyDropdownToModel();
            return;
        }
        if (scope == Scope.EditBPM)
        {
            scope = Scope.Item;
            if (bpmInput) bpmInput.DeactivateInputField();
            ApplyBPMChange();         // ★写回模型 & 广播事件
            TTS_ReadCurrentNameValue();
            return;
        }
    }

    public void ExitInput()
    {
        if (scope == Scope.EditDropdown)
        {
            var dd = GetCurrentDropdown();
            if (dd != null && prevDropdownValue >= 0)
            {
                dd.value = prevDropdownValue; // 撤销
                TTS_ReadBeatCombined();
                ApplyDropdownToModel();       // 撤销后的数值也同步回模型
            }
            scope = Scope.Item;
            return;
        }
        if (scope == Scope.EditBPM)
        {
            if (bpmInput)
            {
                bpmInput.text = bpmBeforeEdit; // 撤销
                bpmInput.DeactivateInputField();
                ApplyBPMChange();              // 以当前文本同步一次
                TTS_ReadCurrentNameValue();
            }
            scope = Scope.Item;
            return;
        }
    }

    #endregion

    #region UI list & visuals

    private enum Kind { Unknown, BPM, Denominator, Numerator }

    private void RebuildVisualList()
    {
        RestoreAllScales();
        visualList.Clear();

        if (bpmInput) AddVisual(bpmInput.gameObject);
        if (denominatorDropdown) AddVisual(denominatorDropdown.gameObject);
        if (numeratorDropdown) AddVisual(numeratorDropdown.gameObject);
    }

    private void AddVisual(GameObject go)
    {
        if (!go) return;
        visualList.Add(go);
        var t = go.transform;
        if (!originalScale.ContainsKey(t)) originalScale[t] = t.localScale;
    }

    private void ApplySelectVisual(int index, bool tween)
    {
        if (visualList.Count == 0) return;
        index = Mathf.Clamp(index, 0, visualList.Count - 1);

        for (int i = 0; i < visualList.Count; i++)
        {
            var t = visualList[i] ? visualList[i].transform : null;
            if (!t) continue;

            t.DOKill();
            var ori = originalScale.TryGetValue(t, out var os) ? os : Vector3.one;

            if (i == index)
            {
                if (tween) t.DOScale(ori * selectedScale, scaleDuration).SetEase(scaleEase);
                else t.localScale = ori * selectedScale;
            }
            else
            {
                if (tween) t.DOScale(ori, scaleDuration).SetEase(scaleEase);
                else t.localScale = ori;
            }
        }
    }

    private void RestoreAllScales()
    {
        foreach (var kv in originalScale)
        {
            var t = kv.Key;
            if (!t) continue;
            t.DOKill();
            t.localScale = kv.Value;
        }
    }

    private Kind GetCurrentKind()
    {
        if (visualList.Count == 0 || currentIndex < 0 || currentIndex >= visualList.Count)
            return Kind.Unknown;

        var go = visualList[currentIndex];
        if (!go) return Kind.Unknown;

        if (bpmInput && go == bpmInput.gameObject) return Kind.BPM;
        if (denominatorDropdown && go == denominatorDropdown.gameObject) return Kind.Denominator;
        if (numeratorDropdown && go == numeratorDropdown.gameObject) return Kind.Numerator;

        return Kind.Unknown;
    }

    private TMP_Dropdown GetCurrentDropdown()
    {
        var kind = GetCurrentKind();
        if (kind == Kind.Denominator) return denominatorDropdown;
        if (kind == Kind.Numerator) return numeratorDropdown;
        return null;
    }

    private void MoveItem(int delta)
    {
        if (visualList.Count == 0) return;
        int next = currentIndex + (delta > 0 ? 1 : -1);
        if (next < 0) next = loopItems ? visualList.Count - 1 : 0;
        if (next >= visualList.Count) next = loopItems ? 0 : visualList.Count - 1;

        currentIndex = Mathf.Clamp(next, 0, visualList.Count - 1);
        ApplySelectVisual(currentIndex, tween: true);
    }

    private void StepDropdown(int dir)
    {
        var dd = GetCurrentDropdown();
        if (dd == null) return;

        int count = dd.options != null ? dd.options.Count : 0;
        if (count <= 0) return;

        int v = dd.value + (dir > 0 ? 1 : -1);
        if (v < 0) v = loopOptions ? count - 1 : 0;
        if (v >= count) v = loopOptions ? 0 : count - 1;

        dd.value = Mathf.Clamp(v, 0, count - 1);  // 数据修改点
        TTS_ReadOptionValueOnly(dd);              // 只读“4”
    }

    #endregion

    #region TTS helpers

    private void TTS_ReadHint(string text)
    {
        if (!enableHelpHints) return;
        if (mTTSManager.Instance != null && !mTTSManager.Instance.TTSEnabled) return;
        if (!string.IsNullOrEmpty(text)) mTTS.Speak(text);
    }

    private void TTS_ReadName(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (mTTSManager.Instance != null && !mTTSManager.Instance.TTSEnabled) return;
        mTTS.Speak(text);
    }

    private void TTS_ReadCurrentNameValue()
    {
        switch (GetCurrentKind())
        {
            case Kind.BPM:
                {
                    string raw = bpmInput ? bpmInput.text : "";
                    if (int.TryParse(raw, out var iv))
                        TTS_ReadName($"{ttsNameBPM} {mTTS.FormatIntegerChinese(iv)}");
                    else
                        TTS_ReadName($"{ttsNameBPM} {raw}");
                    break;
                }
            case Kind.Denominator:
                TTS_ReadName($"{ttsNameDenominator} {GetDropdownValueText(denominatorDropdown)}");
                break;
            case Kind.Numerator:
                TTS_ReadName($"{ttsNameNumerator} {GetDropdownValueText(numeratorDropdown)}");
                break;
        }
    }

    private string GetDropdownValueText(TMP_Dropdown dd)
    {
        if (!dd || dd.options == null || dd.options.Count == 0) return "";
        int idx = Mathf.Clamp(dd.value, 0, dd.options.Count - 1);
        return dd.options[idx].text ?? "";
    }

    private void TTS_ReadBeatCombined()
    {
        string num = GetDropdownValueText(numeratorDropdown);
        string den = GetDropdownValueText(denominatorDropdown);
        if (string.IsNullOrEmpty(num) && string.IsNullOrEmpty(den)) return;
        string phrase = $"{num},{den},拍";
        TTS_ReadName(phrase);
    }

    private void TTS_ReadBPM()
    {
        string raw = bpmInput ? bpmInput.text : "";
        if (string.IsNullOrEmpty(raw)) return;

        if (int.TryParse(raw, out var iv))
            TTS_ReadName($"{ttsNameBPM},{mTTS.FormatIntegerChinese(iv)}");
        else
            TTS_ReadName($"{ttsNameBPM},{raw}");
    }

    public void TTS_ReadValue()
    {
        if (mTTSManager.Instance != null && !mTTSManager.Instance.TTSEnabled) return;

        string num = GetDropdownValueText(numeratorDropdown);     // 分子
        string den = GetDropdownValueText(denominatorDropdown);   // 分母
        string bpm = bpmInput ? bpmInput.text : "";               // BPM

        if (string.IsNullOrEmpty(bpm) && string.IsNullOrEmpty(num) && string.IsNullOrEmpty(den)) return;

        string phrase = $"节拍设置 ，{num}，{den}，拍，BPM，{bpm}";
        mTTS.Speak(phrase);
    }

    private void TTS_ReadOptionValueOnly(TMP_Dropdown dd)
    {
        var val = GetDropdownValueText(dd);
        if (!string.IsNullOrEmpty(val)) TTS_ReadName(val);
    }

    #endregion

    #region Model sync (核心)

    /// <summary>把 bpmInput.text 写入模型，并广播 BPMChangeValue。</summary>
    private void ApplyBPMChange()
    {
        if (!bpmInput) return;
        if (!int.TryParse(bpmInput.text, out int bpmValue)) return;

        var model = this.GetModel<AudioEditModel>();
        if (model == null) return;

        if (bpmMin > 0 && bpmValue < bpmMin) bpmValue = bpmMin;
        if (bpmMax > 0 && bpmValue > bpmMax) bpmValue = bpmMax;

        if (model.BPM != bpmValue)
        {
            model.BPM = bpmValue;
            this.SendEvent(new BPMChangeValue { BPM = bpmValue });
        }
    }

    /// <summary>下拉修改后把 BeatA / BeatB 同步回模型。</summary>
    private void ApplyDropdownToModel()
    {
        if (_isSyncingUI) return; // 纯回填 UI 时不写模型
        var model = this.GetModel<AudioEditModel>();
        if (model == null) return;

        int newBeatA = ParseOption(numeratorDropdown);
        int newBeatB = ParseOption(denominatorDropdown);

        bool changed = false;
        if (newBeatA > 0 && model.BeatA != newBeatA) { model.BeatA = newBeatA; changed = true; }
        if (newBeatB > 0 && model.BeatB != newBeatB) { model.BeatB = newBeatB; changed = true; }

        // 如需立即重建刻度，可选择发送轻量事件；通常不必，UIDrawAScale 有 WatchParamsChanges
        _ = changed;
    }

    /// <summary>从 Model 刷新 UI（不触发事件）。</summary>
    private void SyncUIFromModel()
    {
        var model = this.GetModel<AudioEditModel>();
        if (model == null) return;

        _isSyncingUI = true;

        if (bpmInput) bpmInput.SetTextWithoutNotify(model.BPM.ToString());

        if (numeratorDropdown)
        {
            int idx = FindOptionIndexByText(numeratorDropdown, model.BeatA.ToString());
            if (idx >= 0) numeratorDropdown.SetValueWithoutNotify(idx);
        }
        if (denominatorDropdown)
        {
            int idx = FindOptionIndexByText(denominatorDropdown, model.BeatB.ToString());
            if (idx >= 0) denominatorDropdown.SetValueWithoutNotify(idx);
        }

        _isSyncingUI = false;
    }

    private static int FindOptionIndexByText(TMP_Dropdown dd, string text)
    {
        if (!dd || dd.options == null) return -1;
        for (int i = 0; i < dd.options.Count; i++)
            if (dd.options[i].text == text) return i;
        return -1;
    }

    /// <summary>启用时把当前 UI 值写回模型（可选）。此方法保留以备需要。</summary>
    private void SyncAllToModel(bool silent)
    {
        var model = this.GetModel<AudioEditModel>();
        if (model == null) return;

        if (bpmInput && int.TryParse(bpmInput.text, out int bpmValue))
        {
            if (bpmMin > 0 && bpmValue < bpmMin) bpmValue = bpmMin;
            if (bpmMax > 0 && bpmValue > bpmMax) bpmValue = bpmMax;

            if (model.BPM != bpmValue)
            {
                model.BPM = bpmValue;
                if (!silent) this.SendEvent(new BPMChangeValue { BPM = bpmValue });
            }
        }

        int newBeatA = ParseOption(numeratorDropdown);
        int newBeatB = ParseOption(denominatorDropdown);
        if (newBeatA > 0) model.BeatA = newBeatA;
        if (newBeatB > 0) model.BeatB = newBeatB;
    }

    private void OnBPMEndEdit(string _) => ApplyBPMChange();

    private static int ParseOption(TMP_Dropdown dd)
    {
        if (!dd || dd.options == null || dd.options.Count == 0) return 4;
        if (!int.TryParse(dd.options[dd.value].text, out var x)) x = 4;
        return x;
    }

    #endregion

    #region BPM nudge

    private void NudgeBPM(int delta)
    {
        if (!bpmInput) return;

        int cur;
        if (!int.TryParse(bpmInput.text, out cur)) cur = 120;
        long next = (long)cur + delta;

        if (bpmMin > 0) next = Mathf.Max(bpmMin, (int)next);
        if (bpmMax > 0) next = Mathf.Min(bpmMax, (int)next);

        bpmInput.text = next.ToString();

        TTS_ReadBPM();
        ApplyBPMChange();
    }

    #endregion

    #region IController
    public IArchitecture GetArchitecture() => GameBody.Interface;
    #endregion
}
