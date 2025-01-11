using Qf.Events;
using Qf.Models.AudioEdit;
using Qf.Querys.AudioEdit;
using QFramework;
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
    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;//ÿ�����ص�λ
    int _EditHeight = AudioEditConfig.EditHeight;//�༭���ɱ༭��Χ�߶�
    void Start()
    {
        Init();
        this.RegisterEvent<MainAudioChangeValue>(v=>Init()).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    void Init()
    {
        float SongTime = this.SendQuery(new QueryAudioEditAudioClipLength());
        slideRegin.sizeDelta = new Vector2(SongTime * _PixelUnitsPerSecond, slideRegin.sizeDelta.y);
    }
    public void ToGglebottonPlaneShow()
    {
        if(bottonPlane.anchoredPosition == new Vector2(0, -_EditHeight))
        {
            bottonPlane.anchoredPosition = new Vector2(0,0);
        }
        else
        {
            bottonPlane.anchoredPosition = new Vector2(0,-_EditHeight);
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
