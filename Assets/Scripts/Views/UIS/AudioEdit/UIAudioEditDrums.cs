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
    public AudioSource PreviewAudioSource; // 由 Orbit 传入

    // 预设类型标签（由 Orbit 写入）
    public UIAudioEditDrumsOrbit.PrefabDrumType PrefabType;

    // 深色竖线颜色
    private static readonly Color32 DARK_GREEN = new Color32(0, 128, 0, 255);
    private static readonly Color32 DARK_RED = new Color32(139, 0, 0, 255);
    private static readonly Color32 DARK_BLUE = new Color32(0, 0, 139, 255);

    public void SetTimeHand(UIAudioEditTimeHand timeHandRef) => timeHand = timeHandRef;

    public void OnPointerClick(PointerEventData eventData) => ShowData();

    public void SetColor(Color color) => image.color = color;

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
        Index = index;

        // 统一计算：不再因为 existence==0 而改变 ThisTime 的计算方式
        ThisTime = isTip ? (thisTime - tipOffset) : thisTime;

        var rect = GetComponent<RectTransform>();
        float trackHeight = 0f;
        if (rect.parent != null && rect.parent is RectTransform parentRect)
            trackHeight = parentRect.rect.height;
        else
            trackHeight = rect.sizeDelta.y;

        // 小工具：设置竖线
        void SetupBar(Image bar, Color32 color, bool active, float anchoredX)
        {
            if (bar == null) return;
            var br = bar.rectTransform;
            br.sizeDelta = new Vector2(br.sizeDelta.x, trackHeight);
            br.anchoredPosition = new Vector2(anchoredX, 0);
            bar.color = color;
            bar.gameObject.SetActive(active);
        }

        // 小工具：点状显示（用于 TipOnly/AnswerOnly 的点形态）
        void SetupDot(Sprite dotSprite, Color tint)
        {
            if (image != null)
            {
                if (dotSprite != null) image.sprite = dotSprite;
                image.enabled = true;
                image.color = tint;
            }
            rect.sizeDelta = new Vector2(18f, 18f);
            rect.anchoredPosition = new Vector2(-rect.sizeDelta.x * 0.5f, 0);
        }

        // 小工具：零宽显示（仅竖线为视觉锚点）
        void SetupZeroWidth(Color tint)
        {
            if (image != null)
            {
                image.enabled = true;
                image.color = tint;
            }
            rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
            rect.anchoredPosition = Vector2.zero;
        }

        // —— 仅对 TipOnly / AnswerOnly 做特殊处理 —— //
        switch (PrefabType)
        {
            case PrefabDrumType.TipOnly:
                if (isTip)
                {
                    var dotSprite = specialSprite != null
                        ? specialSprite
                        : (style.PreTipSprite ? style.PreTipSprite : image.sprite);

                    SetupDot(dotSprite, style.PreTipColor);
                    SetupBar(startBarImage, DARK_GREEN, true, 0f);
                    SetupBar(endBarImage, DARK_GREEN, false, 0f);
                }
                else
                {
                    gameObject.SetActive(false);
                }
                return;

            case PrefabDrumType.AnswerOnly:
                if (isTip)
                {
                    // 仅回答音：不显示提示端（tip）
                    gameObject.SetActive(false);
                }
                else
                {
                    // 仅回答音：只显示中心端的点 + 右端竖线（EndBar）
                    var dotSprite = specialSprite != null
                        ? specialSprite
                        : (style.DrumSprite ? style.DrumSprite : image.sprite);

                    SetupDot(dotSprite, style.DrumColor);

                    // 关键：打开 EndBar，StartBar 关闭
                    SetupBar(endBarImage, DARK_RED, true, 0f);
                    SetupBar(startBarImage, DARK_RED, false, 0f);
                }
                return;


                // 其他类型不做额外特判（Audition/JudgeOnly/PlaceAtCenter/PlaceAtTip）
                // 如果你后续需要，还可以保留它们的特化；此处按“正常逻辑”继续向下执行
        }

        // —— 正常逻辑（与类型无关，不再根据 existence/tipOffset 做“自处理降级”） —— //
        if (isTip)
        {
            if (style.PreTipSprite) image.sprite = style.PreTipSprite;
            image.color = style.PreTipColor;

            float tipWidth = Mathf.Max(0f, tipOffset) * pixelUnitsPerSecond;
            rect.sizeDelta = new Vector2(tipWidth, rect.sizeDelta.y);
            rect.anchoredPosition = new Vector2(-tipWidth, 0);

            // 竖线：右端（End）始终显示；左端（Start）在有宽度时显示
            if (startBarImage != null)
            {
                var barRect = startBarImage.rectTransform;
                barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, trackHeight);
                barRect.anchoredPosition = new Vector2(-tipWidth * 0.5f, 0);
                startBarImage.gameObject.SetActive(tipWidth > 0.01f);
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
            // 不再因为 tipOffset/existence==0 切换为“点状/仅声音”
            if (style.DrumSprite) image.sprite = style.DrumSprite;

            float drumWidth = Mathf.Max(0f, existence) * pixelUnitsPerSecond;
            rect.sizeDelta = new Vector2(drumWidth, rect.sizeDelta.y);
            image.color = style.DrumColor;

            float width = rect.sizeDelta.x;
            rect.anchoredPosition = new Vector2(-width / 2f, 0);

            // 竖线：中心层默认仅显示 EndBar（右端）
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

    // 鼓点UI点击唯一选中高亮和列表同步 -- 2024-07-14
    public void ShowData()
    {
        if (timeHand != null)
            timeHand.SetTime(this.ThisTime);

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
