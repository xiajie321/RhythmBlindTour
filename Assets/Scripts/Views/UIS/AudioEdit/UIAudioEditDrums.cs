using Qf.ClassDatas.AudioEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAudioEditDrums : MonoBehaviour,IPointerClickHandler
{
    [SerializeField]
    Image image;
    public float ThisTime;
    public int Index;

    public void OnPointerClick(PointerEventData eventData)
    {
        ShowData();
    }

    public void SetColor(Color color)
    {
        image.color = color;
    }
    /// <summary>
    /// ����򿪵����ĵ�༭��
    /// </summary>
    public void ShowData()
    {
        Debug.Log("ʱ�����е�����");
    }
}
