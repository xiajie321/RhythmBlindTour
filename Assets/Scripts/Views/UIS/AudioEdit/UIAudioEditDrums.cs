using Qf.ClassDatas.AudioEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIAudioEditDrumsOrbit;

public class UIAudioEditDrums : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image image;

    // [红色条：起始与结束] -- mixyao/25/07/04
    [SerializeField] Image startBarImage;
    [SerializeField] Image endBarImage;

    public float ThisTime;
    public int Index;
    public bool IsTip = false;

    private UIAudioEditTimeHand timeHand;

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

    public void ShowData()
    {
        if (timeHand != null)
            timeHand.SetTime(this.ThisTime);
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
            rect.anchoredPosition = new Vector2(-tipWidth, 0); //  向左偏移

            bool hasExistence = !Mathf.Approximately(existence, 0f);
            float height = rect.sizeDelta.y;

            if (startBarImage != null)
            {
                var barRect = startBarImage.rectTransform;
                barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, height);
                barRect.anchoredPosition = new Vector2(-tipWidth * 0.5f, 0); // 左侧红线 x 偏移
                startBarImage.gameObject.SetActive(hasExistence);
            }

            if (endBarImage != null)
            {
                var barRect = endBarImage.rectTransform;
                barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, height);
                barRect.anchoredPosition = new Vector2(+tipWidth * 0.5f, 0); // 右侧红线 x 偏移
                endBarImage.gameObject.SetActive(hasExistence);
            }

        }
        else
        {
            if (style.DrumSprite) image.sprite = style.DrumSprite;
            image.color = style.DrumColor;
            Index = index;

            float drumWidth = existence * pixelUnitsPerSecond;
            rect.sizeDelta = new Vector2(drumWidth, rect.sizeDelta.y);
            rect.anchoredPosition = new Vector2(-drumWidth / 2f, 0); // 中心对齐：向左偏移一半

            if (startBarImage) startBarImage.gameObject.SetActive(false);
            if (endBarImage) endBarImage.gameObject.SetActive(false);
        }
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
