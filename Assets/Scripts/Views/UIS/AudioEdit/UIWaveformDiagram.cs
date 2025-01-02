using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.UI;

public class UIWaveformDiagram : MonoBehaviour, IController
{
    [SerializeField]
    Image musicWaveFormDiagramShow;//���ֲ���ͼ��ʾ
    [SerializeField]
    Color waveFormDiagramColor = Color.yellow;//����ͼ��ɫ
    [SerializeField]
    Color notWaveFormDiagramColor = Color.clear;//�ǲ���ͼ������ɫ



    void Init()
    {
        AudioClip music = this.GetModel<AudioEditModel>().EditAudioClip;
        Debug.Log(music.name);
        Debug.Log(music.samples * music.channels);
        if (music == null) { Debug.Log("û�д���������Ƶ��ʼ��"); return; }
        int musicwaveformwidth = Mathf.CeilToInt(music.length * 100);// ���ο���
        int dataSum = music.frequency / 100; //��������
        float[] samplingData = new float[music.samples * music.channels];//��������
        music.GetData(samplingData, 0);
        float[] waveformValue = new float[samplingData.Length / dataSum];
        float waveformMax = 0;
        for (int i = 0; i < waveformValue.Length; i++)
        {
            waveformValue[i] = 0;
            for (int j = 0; j < dataSum; j++)
            {
                waveformValue[i] += Mathf.Abs(samplingData[(i * dataSum) + j]);
            }
            if (waveformValue[i] > waveformMax)
            {
                waveformMax = waveformValue[i];
            }
        }
        Texture2D waveformTexture = new Texture2D(musicwaveformwidth, 400);
        Color[] Colors = new Color[musicwaveformwidth * 400];
        for (int i = 0; i < Colors.Length; ++i)
        {
            Colors[i] = notWaveFormDiagramColor;
        }
        waveformTexture.SetPixels(Colors, 0);

        float hscaled = (float)musicwaveformwidth / (float)waveformValue.Length;
        float vscaled = 1;
        int waveformhalfhight = (int)(400 / 2.0f);
        if (waveformMax > waveformhalfhight)
        {
            vscaled = waveformhalfhight / waveformMax;
        }
        for (int i = 0; i < waveformValue.Length; ++i)
        {
            int x = (int)(i * hscaled);
            int drawWaveformlength = (int)(waveformValue[i] * hscaled);
            int drawWaveformStartVector = waveformhalfhight - drawWaveformlength;
            int drawWaveformEndVector = waveformhalfhight + drawWaveformlength;
            for (int y = drawWaveformStartVector; y <= drawWaveformEndVector; ++y)
            {
                waveformTexture.SetPixel(x, y, waveFormDiagramColor);
            }
        }
        waveformTexture.Apply();
        musicWaveFormDiagramShow.sprite= Sprite.Create(waveformTexture,new Rect(0,0,waveformTexture.width,waveformTexture.height),new Vector2(0.5f,0.5f));
    }
    void Start()
    {
        Init();
    }
    void Update()
    {

    }
    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}