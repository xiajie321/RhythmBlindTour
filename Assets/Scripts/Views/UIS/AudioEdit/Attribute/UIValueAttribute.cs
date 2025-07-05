using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIValueAttribute : UIAttributeBase, IPointerClickHandler
{
    [SerializeField]
    TMP_InputField inputField;
    private void Start()
    {
        SetParameterType(ParameterType.Value);
        inputField.onEndEdit.AddListener(v =>
        {
            RunAction(v);
        });
    }

    public void SetValueShow(string Value)
    {
        if (inputField != null)
            inputField.text = Value;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        SelectManager.SetAttribute(this);
    }
}
