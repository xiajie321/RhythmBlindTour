using Qf.ClassDatas.AudioEdit;
using Qf.Managers;
using Qf.Models;
using Qf.Models.AudioEdit;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIAudioEditDrumsOrbit;

public class UIAudioEditDrums : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image image;
    [SerializeField] Image startBarImage;
    [SerializeField] Image endBarImage;

    [Header("只发声鼓点用特殊Sprite")]
    [SerializeField] private Sprite specialSprite;

    public float ThisTime;
    public int Index;
    public bool IsTip = false;
    private UIAudioEditTimeHand timeHand;
    public string DrumCode;
    public AudioSource PreviewAudioSource; // 由Orbit传入

    public void SetTimeHand(UIAudioEditTimeHand timeHandRef)
    {
        timeHand = timeHandRef;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ShowData();
    }

    public void SetColor(Color color)
    {
        image.color = color;
    }

    public void InitUI(
        float thisTime,
        int index,
        bool isTip,
        TrackStyle style,
        float tipOffset,
        float existence,
        float pixelUnitsPerSecond)
    {
        IsTip = isTip;
        ThisTime = isTip && Mathf.Approximately(existence, 0f)
            ? thisTime
            : isTip ? thisTime - tipOffset : thisTime;

        var rect = GetComponent<RectTransform>();

        float trackHeight = 0f;
        if (rect.parent != null && rect.parent is RectTransform parentRect)
            trackHeight = parentRect.rect.height;
        else
            trackHeight = rect.sizeDelta.y;

        if (isTip)
        {
            if (style.PreTipSprite) image.sprite = style.PreTipSprite;

            Color tipColor = Mathf.Approximately(existence, 0f)
                ? style.DrumColor * 0.75f
                : style.PreTipColor;
            if (Mathf.Approximately(existence, 0f))
                tipColor.a = 200f / 255f;
            image.color = tipColor;

            float tipWidth = tipOffset * pixelUnitsPerSecond;
            rect.sizeDelta = new Vector2(tipWidth, rect.sizeDelta.y);
            rect.anchoredPosition = new Vector2(-tipWidth, 0);

            bool hasExistence = !Mathf.Approximately(existence, 0f);

            if (startBarImage != null)
            {
                var barRect = startBarImage.rectTransform;
                barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, trackHeight);
                barRect.anchoredPosition = new Vector2(-tipWidth * 0.5f, 0);
                startBarImage.gameObject.SetActive(hasExistence);
            }

            if (endBarImage != null)
            {
                var barRect = endBarImage.rectTransform;
                barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, trackHeight);
                barRect.anchoredPosition = Vector2.zero;
                endBarImage.gameObject.SetActive(true);
            }
        }
        else
        {
            bool isOnlySoundDrum = Mathf.Approximately(tipOffset, 0f) && Mathf.Approximately(existence, 0f);

            if (isOnlySoundDrum && specialSprite != null)
            {
                image.sprite = specialSprite;
                rect.sizeDelta = new Vector2(18f, 18f);
            }
            else
            {
                if (style.DrumSprite) image.sprite = style.DrumSprite;
                float drumWidth = existence * pixelUnitsPerSecond;
                rect.sizeDelta = new Vector2(drumWidth, rect.sizeDelta.y);
            }
            image.color = style.DrumColor;
            Index = index;

            float width = rect.sizeDelta.x;
            rect.anchoredPosition = new Vector2(-width / 2f, 0);

            if (endBarImage != null)
            {
                var barRect = endBarImage.rectTransform;
                barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, trackHeight);
                barRect.anchoredPosition = Vector2.zero;
                endBarImage.gameObject.SetActive(true);
            }
            if (startBarImage != null)
                startBarImage.gameObject.SetActive(false);
        }
    }

    // [功能说明] 鼓点UI点击唯一选中高亮和列表同步 -- 2024-07-14
    public void ShowData()
    {
        if (timeHand != null)
            timeHand.SetTime(this.ThisTime);

        // 只发一次事件，由所有面板监听这个事件（不重复发多种事件）
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode() { DrumCode = DrumCode });

        TryPreviewSound();
    }


    public void TryPreviewSound()
    {
        var editModel = GameBody.Interface.GetModel<AudioEditModel>();
        DrumsLoadData drum = UIAttributeSetPanel.FindDrumByCode(editModel, DrumCode);
        if (drum == null) return;

        var cModel = GameBody.Interface.GetModel<DataCachingModel>();
        string clipPath = IsTip
            ? drum.DrwmsData.FPreAdventAudioClipPath
            : drum.DrwmsData.FSucceedAudioClipPath;
        var clip = cModel.GetAudioClip(clipPath);

        var op = drum.DrwmsData.DtheTypeOfOperation;
        AudioEditManager.Instance.PlayVFXWithFallback(op, clip, 1f);
    }

    public void InitVisual(
        RectTransform parent,
        bool isTip,
        Vector2 anchoredPos,
        Vector2 size,
        Vector2 anchor,
        Vector2 pivot)
    {
        transform.SetParent(parent, false);
        var rect = GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.localScale = Vector3.one;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }
}
