using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIValueAttribute : UIAttributeBase, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        SelectManager.SetAttribute(this);
    }
}
