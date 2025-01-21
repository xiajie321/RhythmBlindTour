using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIAttributeBase : MonoBehaviour
{
    [SerializeField]
    TMP_Text _Name;//��������
    [SerializeField]
    ParameterType _ParameterType;//��������
    public void SetName(string Name)
    {
        _Name.text = Name;
    }
    public ParameterType GetParameterType()
    {
        return _ParameterType;
    }
}
/// <summary>
/// ��������
/// </summary>
public enum ParameterType
{
    Value,
    File,
}
