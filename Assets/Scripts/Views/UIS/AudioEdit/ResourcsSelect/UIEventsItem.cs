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
    [SerializeField] UnityEvent clickevent;

    public void AddAction(UnityAction unityAction) => clickevent.AddListener(unityAction);

    public void SetName(string Name) => _Name.text = Name;
    public void SetImage(Sprite Sprite) => _Image.sprite = Sprite;

    public void OnPointerClick(PointerEventData eventData) => TriggerClick();
    public void OnPointerDown(PointerEventData eventData) { /* 可拓展 */ }
    public void OnPointerUp(PointerEventData eventData) { /* 可拓展 */ }
    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }

    public void TriggerClick()
    {
        transform.DOScale(Vector3.one * 1.1f, 0.1f).SetEase(Ease.Linear).OnComplete(() =>
        {
            transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.Linear);
        });
        clickevent?.Invoke();
    }
}
