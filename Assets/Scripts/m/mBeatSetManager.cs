using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Qf.Models.AudioEdit;
using Qf.Events;
using QFramework;
using System.Collections.Generic;
using System;

public class mBeatSetManager : MonoBehaviour, IController
{
    [Header("节拍BPM UI")]
    public TMP_Dropdown dropdownBeatA;
    public TMP_Dropdown dropdownBeatB;
    public TMP_InputField inputBPM;

    [Header("节拍（每小节除第一拍） 小节（每小节第一拍） 列表")]
    public string[] beatsTime;   // 储放节拍位置精准到0.01，不包含每小节第一拍
    public string[] measureTime; // 储放小节位置精准到0.01，每小节第一拍

    public int lastBeatA = -1;
    public int lastBeatB = -1;
    public int lastBPM = -1;

    private float lastTime = -1f; // 上一次 ThisTime，用于节拍穿过判定

    [Header("节拍扬声器")]
    public AudioSource beatSource;
    public AudioSource measureSource;



    void Start()
    {

        // 监听 OnUpdateThisTime
        this.RegisterEvent<OnUpdateThisTime>(OnUpdateTime)
            .UnRegisterWhenGameObjectDestroyed(gameObject);


        if (dropdownBeatA != null)
        {
            dropdownBeatA.onValueChanged.AddListener(i =>
            {
                var model = GameBody.Interface.GetModel<AudioEditModel>();
                if (int.TryParse(dropdownBeatA.options[i].text, out int newValue) && newValue != model.BeatA)
                {
                    model.BeatA = newValue;
                    RefreshBeatMeasureList();
                }
            });
        }
        if (dropdownBeatB != null)
        {
            dropdownBeatB.onValueChanged.AddListener(i =>
            {
                var model = GameBody.Interface.GetModel<AudioEditModel>();
                if (int.TryParse(dropdownBeatB.options[i].text, out int newValue) && newValue != model.BeatB)
                {
                    model.BeatB = newValue;
                    RefreshBeatMeasureList();
                }
            });
        }
        if (inputBPM != null)
        {
            inputBPM.onEndEdit.AddListener(str =>
            {
                var model = GameBody.Interface.GetModel<AudioEditModel>();
                if (int.TryParse(str, out int newBpm) && newBpm != model.BPM)
                {
                    model.BPM = newBpm;
                    RefreshBeatMeasureList();
                }
            });
        }
    }


    public void TryCacheInitialValues()
    {
        if (dropdownBeatA != null && int.TryParse(dropdownBeatA.options[dropdownBeatA.value].text, out int beatA))
            lastBeatA = beatA;

        if (dropdownBeatB != null && int.TryParse(dropdownBeatB.options[dropdownBeatB.value].text, out int beatB))
            lastBeatB = beatB;

        if (inputBPM != null && int.TryParse(inputBPM.text, out int bpm))
            lastBPM = bpm;
    }

    public void RefreshBeatMeasureList()
    {
        var model = GameBody.Interface.GetModel<AudioEditModel>();

        // ***只从Model读取，不要再从UI取值！***
        int beatA = model.BeatA;
        int beatB = model.BeatB;
        int bpm = model.BPM;

        GenerateBeatAndMeasureTimes(model.EditAudioClip, bpm, beatA, beatB);
    }


    public void GenerateBeatAndMeasureTimes(AudioClip clip, int bpm, int beatA, int beatB)
    {
        if (clip == null || bpm <= 0 || beatA <= 0 || beatB <= 0)
        {
            Debug.LogWarning("参数非法，无法生成节拍和小节数据");
            return;
        }

        float beatDuration = 60f / bpm;
        float audioLength = clip.length;
        List<string> beatList = new();
        List<string> measureList = new();

        int beatIndex = 0;
        float time = 0f;

        while (time < audioLength)
        {
            bool isMeasureFirstBeat = (beatIndex % beatA == 0);
            string timeStr = Math.Round(time, 2).ToString("0.00");

            if (isMeasureFirstBeat)
                measureList.Add(timeStr);
            else
                beatList.Add(timeStr);

            time += beatDuration;
            beatIndex++;
        }

        beatsTime = beatList.ToArray();
        measureTime = measureList.ToArray();
    }

    // 节拍检测：监听 ThisTime 前进并 log 出节拍/小节穿越
    private void OnUpdateTime(OnUpdateThisTime evt)
    {
        if (beatsTime == null || measureTime == null) return;

        float now = (float)Math.Round(evt.ThisTime, 2);
        float prev = lastTime;
        lastTime = now;

        // 防止回退导致多次重复触发
        if (now < prev) return;

        foreach (string t in measureTime)
        {
            if (float.TryParse(t, out float checkpoint))
            {
                if (checkpoint > prev && checkpoint <= now)
                {
                    // 小节播放
                    if (measureSource != null)
                    {
                        measureSource.Play();
                    }
                    else
                    {
                        Debug.Log("找不到！！！");
                    }

                //    Debug.Log($"[小节] time={checkpoint:0.00}");
                }
            }
        }

        foreach (string t in beatsTime)
        {
            if (float.TryParse(t, out float checkpoint))
            {
                if (checkpoint > prev && checkpoint <= now)
                {
                    // 节拍播放
                    if (beatSource != null)
                    {
                        beatSource.Play();
                    }
                    else
                    {
                        Debug.Log("找不到！！！");
                    }

                //    Debug.Log($"[节拍] time={checkpoint:0.00}");
                }
            }
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
