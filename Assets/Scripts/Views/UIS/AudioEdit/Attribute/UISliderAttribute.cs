using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Qf.Managers;

public class UISliderAttribute : UIAttributeBase, IPointerClickHandler, IPointerUpHandler
{
    [SerializeField] private Slider Slider;

    [SerializeField] private bool isSelected = false;
    [SerializeField] public bool EnterSelected = false;
    public bool IsEditing => EnterSelected;

    [SerializeField] private float smallStep = 0.01f;
    [SerializeField] private float largeStep = 0.10f;

    private const float EXPECT_MIN = 0f;
    private const float EXPECT_MAX = 1f;

    private void Awake()
    {
        if (!Slider) Slider = GetComponentInChildren<Slider>(true);
        if (Slider)
        {
            Slider.minValue = EXPECT_MIN;
            Slider.maxValue = EXPECT_MAX;
            Slider.wholeNumbers = false;
            Slider.value = Mathf.Clamp01(Slider.value);

            // 直改模式外默认拦截原生拖动
            Slider.interactable = false;

            // ！！不再依赖 onValueChanged 回调去提交/朗读
             Slider.onValueChanged.AddListener(v => RunAction(v));
        }
        SetParameterType(ParameterType.Slider);

        ApplyGlobalMode(); // 初始根据管理器设置一次
    }

    public void ApplyGlobalMode()
    {
        if (!Slider) return;

        if (mUIAttributeManager.DisableAttributeGating)
        {
            // 直改模式：允许直接拖动/点击
            Slider.interactable = true;
        }
        else
        {
            // 键盘/列表模式：只有编辑态才放开
            Slider.interactable = IsEditing;
        }
    }

    public void SetSteps(float small, float large)
    {
        smallStep = Mathf.Abs(small);
        largeStep = Mathf.Abs(large);
    }

    public void SetValueShow(float value)
    {
        if (Slider != null)
            Slider.SetValueWithoutNotify(Mathf.Clamp01(value)); // 静默展示
    }

    public void EnterInput()
    {
        if (!Slider) return;
        EnterSelected = true;
        Slider.interactable = true;
    }

    public void ConfirmInput()
    {
        EnterSelected = false;
        if (!mUIAttributeManager.DisableAttributeGating && Slider)
            Slider.interactable = false;
    }

    public void ExitInput()
    {
        EnterSelected = false;
        if (!mUIAttributeManager.DisableAttributeGating && Slider)
            Slider.interactable = false;
    }

    public void ResetEnterSelected() => EnterSelected = false;

    // —— 新：统一改值→提交→朗读 —— //
    private void ApplySliderValue(float v, bool speak = true)
    {
        if (!Slider) return;
        v = Mathf.Clamp01(v);

        // 1) 改UI值（静默，不触发外部监听）
        Slider.SetValueWithoutNotify(v);

        // 2) 提交到业务（取代过去 onValueChanged 里的 RunAction）
        RunAction(v);

        // 3) 朗读当前值（一次朗读）
        if (speak && (mTTSManager.Instance == null || mTTSManager.Instance.TTSEnabled))
        {
            int p = Mathf.RoundToInt(v * 100f);
            mTTS.Speak($"百分之{p}");
        }
    }

    public void NudgeVertical(int dir)
    {
        if (!IsEditing || !Slider || dir == 0) return;
        float v = Slider.value + Mathf.Sign(dir) * Mathf.Abs(smallStep);
        ApplySliderValue(v, speak: true);
    }
    public void NudgeHorizontal(int dir)
    {
        if (!IsEditing || !Slider || dir == 0) return;
        float v = Slider.value + Mathf.Sign(dir) * Mathf.Abs(largeStep);
        ApplySliderValue(v, speak: true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isSelected = true;

        // 直改模式：点击即可操作
        if (mUIAttributeManager.DisableAttributeGating && Slider)
            Slider.interactable = true;
        else if (Slider && !EnterSelected)
            Slider.interactable = false;

        SelectManager.SetAttribute(this);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }
    public override void ApplyTTSEnabled(bool enabled)
    {
        if (!Slider) return;
        if (!enabled)
        {
            Slider.interactable = false;
            return;
        }
        // 恢复原有模式判断
        if (mUIAttributeManager.DisableAttributeGating)
            Slider.interactable = true;
        else
            Slider.interactable = IsEditing;
    }

    public void OnPointerUp(PointerEventData eventData) { }
}
