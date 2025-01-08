using Qf.Models.AudioEdit;
using Qf.Querys.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAudioDrumsOrbit : MonoBehaviour,IController
{
    [SerializeField]
    GameObject DrumsProfabs;//�ĵ�Ԥ����
    [SerializeField]
    RectTransform[] DrumsUI;
    IArchitecture IBelongToArchitecture.GetArchitecture()
    {
        return GameBody.Interface;
    }
    public void AddDrwms()
    {
        Debug.Log("���ɹĵ�");
        //�жϵ�ǰλ���Ƿ��Ѿ����ڹĵ�
        //���ɹĵ�
        //���ùĵ�λ��
    }
    public void RemoveDrwms()
    {
        Debug.Log("ɾ���ĵ�");
        //�жϵ�ǰλ���Ƿ��Ѿ����ڹĵ�
        //��ǰ�ĵ�
        //����ĵ�
    }
    void Init()
    {
        //��ʼ���������
        float SongTime = this.SendQuery(new QueryAudioEditAudioClipLength());
        for (int i = 0; i < DrumsUI.Length; i++)
        {
            DrumsUI[i].sizeDelta = new Vector2(SongTime*100,80);
        }

        //��ʼ���ĵ�����
        //ûд
    }
    void Start()
    {
        Init();
    }

}
