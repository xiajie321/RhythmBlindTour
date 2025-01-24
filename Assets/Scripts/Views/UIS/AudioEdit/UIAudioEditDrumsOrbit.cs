using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models.AudioEdit;
using Qf.Querys.AudioEdit;
using QFramework;
using System.Collections.Generic;
using UnityEngine;

public class UIAudioEditDrumsOrbit : MonoBehaviour,IController
{
    [SerializeField]
    GameObject DrumsProfabs;//�ĵ�Ԥ����
    [SerializeField]
    RectTransform[] DrumsUI;
    [SerializeField]
    List<GameObject> DrumsUIInDrums = new ();
    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;//ÿ�����ص�λ
    int _EditHeight = AudioEditConfig.EditHeight;//�༭���ɱ༭��Χ�߶�
    AudioEditModel editModel;
    IArchitecture IBelongToArchitecture.GetArchitecture()
    {
        return GameBody.Interface;
    }
    public void AddDrwms()
    {
        Debug.Log($"���ɹĵ�{editModel.ThisTime}");
        this.SendCommand(new AddAudioEditTimeLineDataCommand(
            editModel.ThisTime,
            new Qf.ClassDatas.AudioEdit.DrumsLoadData()));
    }

    public void RemoveDrwms(int index = -1)
    {
        Debug.Log($"ɾ���ĵ�{editModel.ThisTime}");
        this.SendCommand(new RemoveAudioEditTimeLineDataCommand(
            editModel.ThisTime,
            index
            ));
    }
    public void PlayAllDrwmsUI()
    {

    }
    void Init()
    {
        editModel = this.GetModel<AudioEditModel>();
        StartLength();
        this.RegisterEvent<OnUpdateAudioEditDrumsUI>(v => UpDateDrwmsUI()).UnRegisterWhenGameObjectDestroyed(gameObject);
        this.RegisterEvent<MainAudioChangeValue>(v => StartLength()).UnRegisterWhenGameObjectDestroyed(gameObject);

    }
    void UpDateDrwmsUI()//������Ҫ�Ż�<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<(Ŀǰ����ʱʵ�ֹ���)
    {
        //��ʼ���ĵ�����
        UpDateAllDrwmsUI();
    }
    void UpDateAllDrwmsUI()
    {
        //����ĵ�UI
        foreach(var item in DrumsUIInDrums)
        {
            Destroy(item);
        }
        DrumsUIInDrums.Clear();
        //ͨ���ֵ�����ʵ�������йĵ�λ��
        var a = this.SendQuery(new QueryAudioEditTimeLineAllData());
        Transform go;
        UIAudioEditDrums uiDrums;
        RectTransform gorecttransform;
        foreach (var item in a.Keys)
        {
            go = null;
            for (int i=0; i<a[item].Count;i++)
            {
                go = Instantiate(DrumsProfabs).transform;
                go.SetParent(DrumsUI[i].transform);
                gorecttransform = go.GetComponent<RectTransform>();
                go.transform.position = new Vector3(transform.position.x, DrumsUI[i].transform.position.y- (_EditHeight / DrumsUI.Length/2),transform.position.z);
                gorecttransform.anchoredPosition = new Vector2(item * _PixelUnitsPerSecond, gorecttransform.anchoredPosition.y);
                uiDrums = go.GetComponent<UIAudioEditDrums>();
                uiDrums.SetColor(new Color(Random.Range(0,101)/(float)100,Random.Range(0, 101) / (float)100, Random.Range(0, 101) / (float)100, 1));
                uiDrums.ThisTime = item;
                uiDrums.Index = i;
                DrumsUIInDrums.Add(go.gameObject);
            }
        }
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
