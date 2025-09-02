using Qf.Commands.AudioEdit;
using Qf.Models.AudioEdit;
using Qf.Managers;              // SelectManager
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Qf.Events;
using System.Linq;
using Qf.Models;

public class UIFileAttribute : UIAttributeBase, IPointerClickHandler
{
    [Header("显示名称")]
    [SerializeField] private TMP_Text ShowFileName;

    [SerializeField] private bool isSelected = false;
    [SerializeField] private bool isCapturingInput = false;
    [SerializeField] public bool EnterSelected = false;

    private IUnRegister _selEvtUnreg;

    private void Start()
    {
        SetParameterType(ParameterType.File);
        if (!ShowFileName) ShowFileName = GetComponentInChildren<TMP_Text>(true);
        ApplyGlobalMode();
    }

    public void ApplyGlobalMode()
    {
        // FileAttribute 没有 interactable；这里只保留占位，方便管理器统一调用
        // 若有特殊直改行为，可在这里扩展
    }

    public void SetShowFileName(string name)
    {
        if (ShowFileName != null) ShowFileName.text = name ?? "";
    }

    public void EnterInput()
    {
        if (!isCapturingInput) BeginWaitingSelection();
        else CancelWaitingSelection();
    }

    public void ResetEnterSelected() => EnterSelected = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!ttsClickEnabled) return; // ← 总开关关闭时不响应点击

        isSelected = true;
        SelectManager.SetAttribute(this);
        if (mUIAttributeManager.DisableAttributeGating && !isCapturingInput)
            EnterInput();
    }

    private void BeginWaitingSelection()
    {
        isCapturingInput = true;
        EnterSelected = false;

        _selEvtUnreg?.UnRegister();
        _selEvtUnreg = this.RegisterEvent<SelectOptions>(OnResourceSelected);
    }

    private void OnResourceSelected(SelectOptions e)
    {
        if (!isCapturingInput || !isSelected) return;

        var clip = e.SelectValue as AudioClip;
        RunAction(clip);
        SetShowFileName(clip ? clip.name : null);

        isCapturingInput = false;
        EnterSelected = true;

        _selEvtUnreg?.UnRegister();
        _selEvtUnreg = null;
    }

    private void CancelWaitingSelection()
    {
        isCapturingInput = false;
        _selEvtUnreg?.UnRegister();
        _selEvtUnreg = null;
    }

    private void OnDisable()
    {
        if (_selEvtUnreg != null)
        {
            _selEvtUnreg.UnRegister();
            _selEvtUnreg = null;
        }
        isCapturingInput = false;
    }

    private bool ttsClickEnabled = true;

    public override void ApplyTTSEnabled(bool enabled)
    {
        ttsClickEnabled = enabled;
        if (!enabled) // 正在等待选择时，关掉更稳
            CancelWaitingSelection();
    }
    public void PreviewCurrent()
    {
        // 1) 取当前显示的文件名（优先 ShowFileName；否则找名为 "Nr" 的 TMP_Text）
        string name = null;
        if (ShowFileName) name = ShowFileName.text;
        if (string.IsNullOrEmpty(name))
        {
            var nr = GetComponentsInChildren<TMPro.TMP_Text>(true)
                     .FirstOrDefault(t => t.gameObject.name == "Nr");
            name = nr ? nr.text : null;
        }
        if (string.IsNullOrEmpty(name)) return;

        // 2) 从你的缓存/模型里拿 AudioClip（按你工程实际改这段）
        var cache = this.GetModel<DataCachingModel>();
        var clip = cache != null ? cache.GetAudioClip(name) : null;
        if (clip == null) return;

        // 3) 播一次试听（按你们统一的预览通道改这行）
        if (AudioEditManager.Instance != null)
            AudioEditManager.Instance.PlayVFXWithFallback(TheTypeOfOperation.Click, clip, 1f);
    }


}
