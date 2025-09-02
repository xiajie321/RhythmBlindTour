using System.Collections;
using UnityEngine;
public class UIAttributeGet : MonoBehaviour
{
    [SerializeField]
    UIAttributeBase attributeBase;
    public object GetUIAttribute()
    {
        switch (attributeBase.GetParameterType())
        {
            case ParameterType.Value:
                return GetTypeUI<UIValueAttribute>(attributeBase);
            case ParameterType.File:
                return GetTypeUI<UIFileAttribute>(attributeBase);
            case ParameterType.Slider:
                return GetTypeUI<UISliderAttribute>(attributeBase);
            case ParameterType.Drop:
                return GetTypeUI<UIDorpAttribute>(attributeBase);
            default:
                return null;
        }
    }
    T GetTypeUI<T>(UIAttributeBase uIAttributeBase) where T: UIAttributeBase
    {
        return (T)uIAttributeBase;
    } 
}