using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIAttributeBase : MonoBehaviour, IController
{
    [SerializeField]
    TMP_Text _Name;//属性名称
    ParameterType _ParameterType;//参数类型
    Action<object> action;
    public void SetAction(Action<object> action)
    {
        this.action = action;
    }
    public void RunAction(object value)
    {
        action?.Invoke(value);
    }
    /// <summary>
    /// 设置显示的名称
    /// </summary>
    /// <param name="Name"></param>
    public void SetName(string Name)
    {
        _Name.text = Name;
    }
    public ParameterType GetParameterType()
    {
        return _ParameterType;
    }
    public void SetParameterType(ParameterType parameterType)
    {
        _ParameterType = parameterType;
    }
    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
    // 供全局“是否允许交互”下发（如 TTS 关闭）
    public virtual void ApplyTTSEnabled(bool enabled) { }
}
/// <summary>
/// 参数类型
/// </summary>
public enum ParameterType
{
    Drop,
    Value,
    File,
    Slider
}
