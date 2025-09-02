using DG.Tweening;
using Qf.Events;
using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIEventsItem : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("响应方式")]
    public InputTriggerType TriggerType = InputTriggerType.Click;

    [SerializeField] TMP_Text _Name;
    [SerializeField] Image _Image;
    [SerializeField] public UnityEvent clickevent;

    [Header("触发开关")]
    [SerializeField] bool isClickOn = true;

    [Tooltip("为 true 时：OnEnable 自动 SetClickOn；OnDisable 自动 SetClickOff")]
    [SerializeField] bool toggleByEnableState = true;

    [Tooltip("为 true 时：OnDisable 重置缩放并清理 DOTween 动画")]
    [SerializeField] bool resetScaleOnDisable = true;

    [Tooltip("阻止触发时打印日志（仅调试）")]
    [SerializeField] bool logWhenBlocked = false;

    public void AddAction(UnityAction unityAction) => clickevent.AddListener(unityAction);

    public void SetName(string Name) => _Name.text = Name;
    public void SetImage(Sprite Sprite) => _Image.sprite = Sprite;

    private void OnEnable()
    {
        if (toggleByEnableState)
            SetClickOn();
    }

    private void OnDisable()
    {
        if (toggleByEnableState)
            SetClickOff();

        if (resetScaleOnDisable)
        {
            // 停止可能残留的补间，避免再次启用时缩放异常
            try { transform.DOKill(complete: false); } catch { /* ignore */ }
            transform.localScale = Vector3.one;
        }
    }

    public void OnPointerClick(PointerEventData eventData) => TriggerClick();
    public void OnPointerDown(PointerEventData eventData) { /* 可拓展 */ }
    public void OnPointerUp(PointerEventData eventData) { /* 可拓展 */ }
    public void OnPointerEnter(PointerEventData eventData) { /* 可拓展 */ }
    public void OnPointerExit(PointerEventData eventData) { /* 可拓展 */ }

    public void TriggerClick()
    {
        if (!isClickOn)
        {
            if (logWhenBlocked)
                Debug.Log($"[UIEventsItem] 被禁用，阻止触发：{gameObject.name}");
            return;
        }

        transform.DOScale(Vector3.one * 1.1f, 0.1f).SetEase(Ease.Linear).OnComplete(() =>
        {
            transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.Linear);
        });

        clickevent?.Invoke();
    }

    // TODO[同按键切换功能] 配合 UIEventSwitcher 使用
    public void SetClickOff()
    {
        if (isClickOn) isClickOn = false;
    }

    public void SetClickOn()
    {
        if (!isClickOn) isClickOn = true;
    }
}
