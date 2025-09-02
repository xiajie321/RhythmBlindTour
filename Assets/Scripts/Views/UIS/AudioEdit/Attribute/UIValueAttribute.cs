using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Globalization;

public class UIValueAttribute : UIAttributeBase, IPointerClickHandler
{
    [Header("TMP_InputField")]
    [SerializeField] private TMP_InputField inputField;

    [SerializeField] private bool isSelected = false;
    [SerializeField] public bool EnterSelected = false;
    public bool IsEditing => EnterSelected;

    [SerializeField] private float smallStep = 0.01f;
    [SerializeField] private float largeStep = 0.10f;
    [SerializeField] private int decimals = 2;

    private string originalText = "";

    private void Start()
    {
        SetParameterType(ParameterType.Value);
        if (!inputField) inputField = GetComponentInChildren<TMP_InputField>(true);

        if (inputField)
        {
            inputField.interactable = false;             // 默认拦截原生输入
            // 不依赖 onValueChanged 做业务/朗读
            inputField.onEndEdit.AddListener(_ => { });
        }
        ApplyGlobalMode(); // 初始根据管理器设置一次
    }

    public void ApplyGlobalMode()
    {
        if (!inputField) return;

        if (mUIAttributeManager.DisableAttributeGating)
        {
            inputField.interactable = true;
        }
        else
        {
            inputField.interactable = IsEditing;
            if (!IsEditing) inputField.DeactivateInputField();
        }
    }

    public void SetSteps(float small, float large)
    {
        smallStep = Mathf.Abs(small);
        largeStep = Mathf.Abs(large);
    }
    public void SetDecimals(int d) => decimals = Mathf.Clamp(d, 0, 6);

    public void SetValueShow(string value)
    {
        if (inputField != null) inputField.SetTextWithoutNotify(value); // 静默展示
    }

    public void ResetEnterSelected() => EnterSelected = false;

    public void EnterInput()
    {
        if (!inputField) return;
        EnterSelected = true;
        originalText = inputField.text;

        inputField.interactable = true;
        FocusAndSelectAll();
    }

    public void ConfirmInput()
    {
        if (!inputField) { EnterSelected = false; return; }
        string txt = inputField.text;

        // 提交
        RunAction(txt);

        inputField.DeactivateInputField();
        EnterSelected = false;

        if (!mUIAttributeManager.DisableAttributeGating)
            inputField.interactable = false;
    }

    public void CancelInput()
    {
        if (!inputField) { EnterSelected = false; return; }
        inputField.SetTextWithoutNotify(originalText);
        inputField.DeactivateInputField();
        EnterSelected = false;

        if (!mUIAttributeManager.DisableAttributeGating)
            inputField.interactable = false;
    }

    public void NudgeVertical(int dir)
    {
        if (!IsEditing || !inputField || dir == 0) return;
        ApplyStep(dir, Mathf.Abs(smallStep));
    }
    public void NudgeHorizontal(int dir)
    {
        if (!IsEditing || !inputField || dir == 0) return;
        ApplyStep(dir, Mathf.Abs(largeStep));
    }

    private void ApplyStep(int dir, float step)
    {
        inputField.DeactivateInputField();

        float cur;
        if (!float.TryParse(inputField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out cur))
            cur = 0f;

        float v = cur + Mathf.Sign(dir) * step;
        string fmt = "F" + Mathf.Clamp(decimals, 0, 6);

        // 1) 改UI（静默）
        inputField.SetTextWithoutNotify(v.ToString(fmt, CultureInfo.InvariantCulture));

        // 2) 提交业务
        RunAction(inputField.text);

        // 3) 朗读当前值（加“秒”）
        if (mTTSManager.Instance == null || mTTSManager.Instance.TTSEnabled)
        {
            string speak = string.IsNullOrEmpty(inputField.text) ? null :
                           (inputField.text.EndsWith("秒") ? inputField.text : (inputField.text + "秒"));
            if (!string.IsNullOrEmpty(speak)) mTTS.Speak(speak);
        }

        FocusAndSelectAll();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isSelected = true;
        SelectManager.SetAttribute(this);

        if (mUIAttributeManager.DisableAttributeGating && inputField)
        {
            inputField.interactable = true;
            inputField.ActivateInputField();
        }
        else
        {
            if (inputField) inputField.DeactivateInputField();
        }
    }

    private void FocusAndSelectAll()
    {
        if (!inputField) return;
        inputField.ActivateInputField();

        int len = inputField.text != null ? inputField.text.Length : 0;
        inputField.caretPosition = len;
        inputField.stringPosition = len;
        inputField.selectionStringAnchorPosition = 0;
        inputField.selectionStringFocusPosition = len;
    }

    public override void ApplyTTSEnabled(bool enabled)
    {
        if (!inputField) return;

        if (!enabled)
        {
            inputField.interactable = false;
            inputField.DeactivateInputField();
            return;
        }

        if (mUIAttributeManager.DisableAttributeGating)
            inputField.interactable = true;
        else
            inputField.interactable = IsEditing;
    }

}
