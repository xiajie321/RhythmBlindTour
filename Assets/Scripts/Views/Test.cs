using Qf.Events;
using Qf.Systems;
using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour,IController
{
    [SerializeField]
    RectTransform progressBar; // ��������RectTransform
    [SerializeField]
    Color scaleColor = Color.black; // �̶ȵ���ɫ
    [SerializeField]
    float _JInterval = 1;
    int numberOfScales = 10; // �̶ȵ�����
    float scaleHeight = 80f; // ÿ���̶ȵĸ߶�
    List<GameObject> _JGameObjects = new();//����
    List<GameObject> _XJGameObjects = new();//С����
    int _PixelUnitsPerSecond = AudioEditConfig.PixelUnitsPerSecond;//ÿ�����ص�λ
    int _EditHeight = AudioEditConfig.EditHeight;//�༭���ɱ༭��Χ�߶�
    void Start()
    {
        this.RegisterEvent<MainAudioChangeValue>(v =>
        {
            numberOfScales = (int)v.Length+2;
            GenerateScales();
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    void GenerateScales()
    {
        for (int i = 0; i < numberOfScales; i++)
        {
            GameObject scale = new GameObject("Scale" + i);
            _JGameObjects.Add(scale);
            scale.transform.SetParent(progressBar, false);
            RectTransform rectTransform = scale.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(5f, scaleHeight); // ��Ⱥ͸߶�
            rectTransform.anchoredPosition = new Vector2(i* _PixelUnitsPerSecond * _JInterval,0); // ��ֱλ��
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0); // ���ĵ�
            Image image = scale.AddComponent<Image>();
            image.color = scaleColor;
        }
    }
    public void SetColor(Color color)
    {
        scaleColor = color;
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}