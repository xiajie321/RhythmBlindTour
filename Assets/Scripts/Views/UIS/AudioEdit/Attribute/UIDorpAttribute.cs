using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIDorpAttribute : UIAttributeBase, IPointerClickHandler
{
    [SerializeField] TMP_Dropdown Dropdown;
    TheTypeOfOperation TheTypeOfOperation;

    private void Start()
    {
        SetParameterType(ParameterType.Drop);
        // UI交互时仍然正常走 RunAction
        Dropdown.onValueChanged.AddListener(v => { RunAction(v); });
    }

    //  泛型枚举版本：静默设值
    public void SetDropdownVlaue<T>(T value) where T : Enum
    {
        Dropdown.SetValueWithoutNotify(Convert.ToInt32(value));
    }

    //  int 版本：静默设值
    public void SetDropdownVlaue(int value)
    {
        Dropdown.SetValueWithoutNotify(value);
    }

    // 兼容原有写法
    public void SetDropdownVlaue(TheTypeOfOperation theTypeOfOperation)
    {
        Dropdown.SetValueWithoutNotify((int)theTypeOfOperation);
    }

    //  新增：静默设值并“应用”（手动触发你的业务逻辑）
    public void SetDropdownAndApply(int value)
    {
        Dropdown.SetValueWithoutNotify(value);
        RunAction(value); // 直接走你在 SetAction(...) 里注册的回调
    }

    //  可选：枚举重载
    public void SetDropdownAndApply<T>(T value) where T : Enum
    {
        int v = Convert.ToInt32(value);
        Dropdown.SetValueWithoutNotify(v);
        RunAction(v);
    }

    public TheTypeOfOperation GetDropdownVlaue() => TheTypeOfOperation;

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectManager.SetAttribute(this);
    }

    public override void ApplyTTSEnabled(bool enabled)
    {
        if (Dropdown) Dropdown.interactable = enabled;
    }
}
