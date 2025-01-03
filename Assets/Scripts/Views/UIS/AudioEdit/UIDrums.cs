using Qf.ClassDatas.AudioEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDrums : MonoBehaviour,IPointerClickHandler
{
    Dictionary<float, DrumsLoadData> _data = new();
    [SerializeField]
    Image image;

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
        Debug.Log("时间轴中的数据");
    }
}
