using Qf.Models.AudioEdit;
using Qf.Querys.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

public class UIAudioEditBottonPlane : MonoBehaviour ,IController
{
    [SerializeField]
    RectTransform bottonPlane; //�²����
    [SerializeField]
    Image buttonImage; //��ťͼƬ
    [SerializeField]
    RectTransform slideRegin; //��������

    void Start()
    {
        Init();
    }
    void Init()
    {
        float SongTime = this.SendQuery(new QueryAudioEditAudioClipLength());
        slideRegin.sizeDelta = new Vector2(SongTime * 100, slideRegin.sizeDelta.y);
    }
    public void ToGglebottonPlaneShow()
    {
        if(bottonPlane.anchoredPosition == new Vector2(0, -400))
        {
            bottonPlane.anchoredPosition = new Vector2(0,0);
            Debug.Log("1");
        }
        else
        {
            bottonPlane.anchoredPosition = new Vector2(0,-400);
            Debug.Log("2");
        }
    }
    void Update()
    {
        
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
