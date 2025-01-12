using Qf.Commands.AudioEdit;
using Qf.Events;
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
    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;//ÿ�����ص�λ
    int _EditHeight = AudioEditConfig.EditHeight;//�༭���ɱ༭��Χ�߶�
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
        StartLength();
        this.RegisterEvent<OnUpdateAudioEditDrumsUI>(v => UpDateDrwmsUI()).UnRegisterWhenGameObjectDestroyed(gameObject);
        this.RegisterEvent<MainAudioChangeValue>(v => StartLength()).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    void UpDateDrwmsUI()
    {
        //��ʼ���ĵ�����
    }
    void StartLength()
    {
        //��ʼ���������
        float SongTime = this.SendQuery(new QueryAudioEditAudioClipLength());
        for (int i = 0; i < DrumsUI.Length; i++)
        {
            DrumsUI[i].sizeDelta = new Vector2(SongTime * _PixelUnitsPerSecond, _EditHeight/ DrumsUI.Length);
        }
    }
    void Start()
    {
        Init();
    }

}
