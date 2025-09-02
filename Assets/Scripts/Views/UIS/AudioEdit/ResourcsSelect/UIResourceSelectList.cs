using Qf.Models;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIResourceSelectList : MonoBehaviour, IController
{
    public enum AudioSourceType { Internal, External }

    [Header("预制体与容器")]
    [SerializeField] private GameObject UIItemProfab;
    [SerializeField] private GameObject ObjectParent;

    [Header("（可选）手动指定 ScrollRect；留空则自动向上查找")]
    [SerializeField] private ScrollRect scrollRectOverride;

    [Header("选择行为")]
    [SerializeField] private bool centerOnSelect = true;
    [SerializeField] private float centerSmoothDuration = 0.12f;

    [Header("TTS（代播报）")]
    [SerializeField] private bool enableSelectionTTS = false;

    [Header("试听设置（统一）")]
    [SerializeField] private float auditionSeconds = 1.5f;
    [SerializeField] private AudioSource auditionSource;

    [Header("公开：生成后的所有条目（按显示顺序）")]
    public List<UIResourceItem> UIItems = new();

    private readonly List<AudioClip> currentClips = new();
    private AudioSourceType currentSource = AudioSourceType.Internal;
    private Coroutine previewCR;
    private int cycleStep = 0; // 1=读名, 2=试听, 3=读名

    public int CurrentIndex { get; private set; } = -1;

    void OnEnable()
    {
        if (CurrentIndex < 0) SelectIndex(FirstValid(), simulateHover: true);
        else SelectIndex(CurrentIndex, simulateHover: true);
    }

    void OnDisable()
    {
        cycleStep = 0;
        StopPreviewIfAny();
    }

    // ======== 对外接口：构建/切换数据源 ========
    public void ShowInternalResources()
    {
        var modelClips = this.GetModel<DataCachingModel>().GetListAudioClips();
        RebuildUI(modelClips, AudioSourceType.Internal);
    }

    public void ShowExternalResources(List<AudioClip> externalClips)
    {
        RebuildUI(externalClips, AudioSourceType.External);
    }

    // ======== 导航 ========
    private int FirstValid()
    {
        if (UIItems == null) return -1;
        for (int i = 0; i < UIItems.Count; i++)
        {
            var item = UIItems[i];
            if (item != null && item.gameObject.activeInHierarchy)
                return i;
        }
        return -1;
    }

    public void MoveSelectionVertical(int direction)
    {
        if (UIItems == null || UIItems.Count == 0) return;
        if (direction == 0) return;

        int next = CurrentIndex;
        if (next < 0) next = direction > 0 ? 0 : UIItems.Count - 1;
        else
        {
            next += direction;
            if (next < 0) next = UIItems.Count - 1;
            if (next >= UIItems.Count) next = 0;
        }

        SelectIndex(next, simulateHover: true);
    }

    public void ConfirmSelection()
    {
        if (UIItems == null || UIItems.Count == 0) return;
        if (CurrentIndex < 0 || CurrentIndex >= UIItems.Count) return;

        var item = UIItems[CurrentIndex];
        EmulateClick(item);
    }

    public void BackWithoutConfirm() { /* 保留位置，不做更改 */ }

    public void SelectIndex(int index, bool simulateHover)
    {
        if (UIItems == null || UIItems.Count == 0) return;
        if (index < 0 || index >= UIItems.Count) return;

        int prevIndex = CurrentIndex;

        if (CurrentIndex >= 0 && CurrentIndex < UIItems.Count && CurrentIndex != index)
        {
            var prev = UIItems[CurrentIndex];
            EmulateExit(prev);
        }

        CurrentIndex = index;
        var cur = UIItems[CurrentIndex];

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(cur.gameObject);

        if (centerOnSelect) EnsureCentered(cur, smooth: centerSmoothDuration > 0f);
        else EnsureCentered(cur, smooth: false);

        if (simulateHover) EmulateHover(cur);

        if (enableSelectionTTS && prevIndex != CurrentIndex)
        {
            SpeakSelectedItemName(cur);
        }

        if (prevIndex != CurrentIndex) cycleStep = 0;
    }

    // ======== 重建 UI ========
    private void RebuildUI(List<AudioClip> clips, AudioSourceType source)
    {
        currentSource = source;
        currentClips.Clear();
        if (clips != null) currentClips.AddRange(clips);

        if (!ObjectParent || !UIItemProfab)
        {
            Debug.LogWarning("[UIResourceSelectList] Prefab 或 Parent 未设置。");
            return;
        }

        for (int i = ObjectParent.transform.childCount - 1; i >= 0; i--)
            Destroy(ObjectParent.transform.GetChild(i).gameObject);

        UIItems.Clear();
        CurrentIndex = -1;

        if (currentClips.Count == 0) return;

        foreach (var clip in currentClips)
        {
            var go = Instantiate(UIItemProfab);
            var newItem = go.GetComponent<UIResourceItem>();
            newItem.transform.SetParent(ObjectParent.transform, false);

            // UI显示名 & TTS名分别设置（TTS名不受 UI 截断/滚动影响）
            newItem.SetName(clip.name);
            newItem.SetTTSName(clip.name);

            newItem.SetAudioClip(clip);
            UIItems.Add(newItem);
        }
    }

    // ======== 私有：TTS & 试听 ========
    UIResourceItem GetCurrentItem()
    {
        if (UIItems == null || UIItems.Count == 0) return null;
        if (CurrentIndex < 0 || CurrentIndex >= UIItems.Count) return null;
        return UIItems[CurrentIndex];
    }

    void ReadSelectedName()
    {
        var item = GetCurrentItem();
        if (!item) return;

        string nameText = ResolveItemNameText(item);
        if (!string.IsNullOrEmpty(nameText))
            mTTS.Speak(nameText);
    }

    void PreviewSelectedAudio()
    {
        var item = GetCurrentItem();
        if (!item) return;

        var clip = item.GetAudioClip();
        if (!clip) return;

        if (!auditionSource)
        {
            auditionSource = gameObject.AddComponent<AudioSource>();
            auditionSource.playOnAwake = false;
        }

        StopPreviewIfAny();

        auditionSource.clip = clip;
        auditionSource.time = 0f;
        auditionSource.volume = 1f;
        auditionSource.loop = false;
        auditionSource.Play();

        float dur = Mathf.Min(Mathf.Max(0.01f, auditionSeconds), clip.length);
        previewCR = StartCoroutine(StopAfter(dur));
    }

    void StopPreviewIfAny()
    {
        if (previewCR != null) { StopCoroutine(previewCR); previewCR = null; }
        if (auditionSource && auditionSource.isPlaying) auditionSource.Stop();
    }

    IEnumerator StopAfter(float secs)
    {
        yield return new WaitForSecondsRealtime(secs);
        if (auditionSource && auditionSource.isPlaying) auditionSource.Stop();
        previewCR = null;
    }

    // ======== 公共：顺序播报/试听 & 单项功能 ========
    public void CycleNamePreviewName()
    {
        if (UIItems == null || UIItems.Count == 0) return;
        cycleStep = (cycleStep % 3) + 1;
        switch (cycleStep)
        {
            case 1: ReadSelectedName(); break;
            case 2: PreviewSelectedAudio(); break;
            case 3: ReadSelectedName(); break;
        }
    }

    public void ReadNameOnce() => ReadSelectedName();
    public void PreviewOnce() => PreviewSelectedAudio();

    // ======== TTS：解析“用于朗读的名称” ========
    private void SpeakSelectedItemName(UIResourceItem item)
    {
        if (item == null) return;
        string displayName = ResolveItemNameText(item);
        if (!string.IsNullOrEmpty(displayName)) mTTS.Speak(displayName);
    }

    private static string ResolveItemNameText(UIResourceItem item)
    {
        // 1) 首选：TTS 专用名称（与 UI 显示解耦）
        string tts = item.GetTTSName();
        if (!string.IsNullOrEmpty(tts)) return tts;

        // 2) 兜底策略：尝试 UI 内的 Name 文本 / 第一个文本 / GameObject 名称
        var tmpName = item.GetComponentsInChildren<TMP_Text>(true)
                         .FirstOrDefault(t => t.gameObject.name == "Name");
        if (tmpName != null && !string.IsNullOrEmpty(tmpName.text))
            return tmpName.text;

        var uguiName = item.GetComponentsInChildren<Text>(true)
                           .FirstOrDefault(t => t.gameObject.name == "Name");
        if (uguiName != null && !string.IsNullOrEmpty(uguiName.text))
            return uguiName.text;

        var anyTMP = item.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
        if (anyTMP != null && !string.IsNullOrEmpty(anyTMP.text))
            return anyTMP.text;

        var anyUGUI = item.GetComponentsInChildren<Text>(true).FirstOrDefault();
        if (anyUGUI != null && !string.IsNullOrEmpty(anyUGUI.text))
            return anyUGUI.text;

        return item.gameObject.name;
    }

    // ======== 居中滚动 & Pointer 事件模拟 ========
    private ScrollRect GetScrollRect()
    {
        if (scrollRectOverride != null) return scrollRectOverride;
        return ObjectParent ? ObjectParent.GetComponentInParent<ScrollRect>() : null;
    }

    private void EnsureCentered(UIResourceItem item, bool smooth)
    {
        var sr = GetScrollRect();
        if (sr == null || sr.content == null || item == null) return;

        var content = sr.content;
        var viewport = sr.viewport != null ? sr.viewport : sr.GetComponent<RectTransform>();

        float contentH = content.rect.height;
        float viewportH = viewport.rect.height;
        float scrollRange = contentH - viewportH;
        if (scrollRange <= 0.0001f) return;

        var childRT = item.GetComponent<RectTransform>();
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, childRT);

        float contentTopY = content.rect.height * (1f - content.pivot.y);
        float itemCenterFromTop = contentTopY - bounds.center.y;

        float targetViewTop = Mathf.Clamp(itemCenterFromTop - viewportH * 0.5f, 0f, scrollRange);
        float targetVNP = 1f - (targetViewTop / Mathf.Max(0.0001f, scrollRange));

        if (smooth && centerSmoothDuration > 0f) StartCoroutine(SmoothVNP(sr, targetVNP, centerSmoothDuration));
        else sr.verticalNormalizedPosition = targetVNP;
    }

    private System.Collections.IEnumerator SmoothVNP(ScrollRect sr, float target, float dur)
    {
        float start = sr.verticalNormalizedPosition;
        float t = 0f;
        dur = Mathf.Max(0.0001f, dur);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            sr.verticalNormalizedPosition = Mathf.Lerp(start, target, s);
            yield return null;
        }
        sr.verticalNormalizedPosition = target;
    }

    private static void EmulateHover(UIResourceItem item)
    {
        if (EventSystem.current == null || item == null) return;
        var data = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute<IPointerEnterHandler>(item.gameObject, data, ExecuteEvents.pointerEnterHandler);
    }

    private static void EmulateExit(UIResourceItem item)
    {
        if (EventSystem.current == null || item == null) return;
        var data = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute<IPointerExitHandler>(item.gameObject, data, ExecuteEvents.pointerExitHandler);
    }

    private static void EmulateClick(UIResourceItem item)
    {
        if (EventSystem.current == null || item == null) return;
        var data = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            clickCount = 1
        };
        ExecuteEvents.Execute<IPointerClickHandler>(item.gameObject, data, ExecuteEvents.pointerClickHandler);
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
    // 0 = 读 “变量名，值名”；1 = 试听
    private readonly Dictionary<GameObject, int> _fileReadCycle = new();

}
