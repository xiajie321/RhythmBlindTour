using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.UI;
using Qf.Events;

public class UIWaveformDiagram : MonoBehaviour, IController
{
    [SerializeField]
    Image musicWaveFormDiagramShow;//音乐波形图显示
    [SerializeField]
    Color waveFormDiagramColor = Color.yellow;//波形图颜色
    [SerializeField]
    Color notWaveFormDiagramColor = Color.clear;//非波形图区域颜色



    void Init()
    {
        AudioClip music = this.GetModel<AudioEditModel>().EditAudioClip;
        if (music == null) { Debug.Log("没有待处理的音频初始化"); return; }
        Debug.Log("初始化音乐波形图名称: " + music.name);
        Debug.Log(music.length);
        int musicwaveformwidth = Mathf.CeilToInt(music.length * 100);// 波形宽度
        int dataSum = music.frequency / 100; //数据容量
        float[] samplingData = new float[music.samples * music.channels];//采样数据
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
            int drawWaveformlength = (int)(waveformValue[i] * vscaled);
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
        this.RegisterEvent<AudioEditChangeEvent>(v => Init());
    }
    void Update()
    {

    }
    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
