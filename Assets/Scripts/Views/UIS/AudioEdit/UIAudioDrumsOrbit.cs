using Qf.Models.AudioEdit;
using Qf.Querys.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAudioDrumsOrbit : MonoBehaviour,IController
{
    [SerializeField]
    GameObject DrumsProfabs;
    [SerializeField]
    UITimeHand UITimeHand;
    [SerializeField]
    RectTransform[] DrumsUI;
    IArchitecture IBelongToArchitecture.GetArchitecture()
    {
        return GameBody.Interface;
    }
    public void AddDrwms()
    {
        //�жϵ�ǰλ���Ƿ��Ѿ����ڹĵ�
        //���ɹĵ�
        //���ùĵ�λ��
    }
    public void RemoveDrwms()
    {
        //�жϵ�ǰλ���Ƿ��Ѿ����ڹĵ�
        //��ǰ�ĵ�
        //����ĵ�
    }
    void Init()
    {
        float SongTime = this.SendQuery(new QueryAudioEditAudioClipLength());
        for (int i = 0; i < DrumsUI.Length; i++)
        {
            DrumsUI[i].sizeDelta = new Vector2(SongTime*100,80);
        }
    }
    void Start()
    {
        Init();
    }

}
