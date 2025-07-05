using UnityEngine;
using Qf.Events;
using Qf.Commands.AudioEdit;
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class mDefaultAudioInitializer : MonoBehaviour, IController
{
    [Header("属性目标顺序对应（主 → 提示）")]
    public List<UIFileAttribute> targetAttributes = new(); // 12个元素
    [Header("所有 Audio 数据组")]
    public List<mAudioDataSO> audioDataList = new();
    [Header("默认选择编号")]
    public static int selectedIndex = 0;

    [Header("初始化使用的 BPM")]
    public int initialBPM = 120;

    [Header("初始化节拍设置")]
    public TMP_Dropdown beatADropdown;
    public TMP_Dropdown beatBDropdown;
    public int defaultBeatA = 4;
    public int defaultBeatB = 4;

    [Header("主音频波形图设置")]
    public Image waveformImage;
    public Color waveformColor = Color.yellow;
    public Color waveformBGColor = Color.clear;
    public int waveformHeight = 80;
    [SerializeField] private Vector2Int waveformResolution = new Vector2Int(1024, 128);



    void Start()
    {
        var model = this.GetModel<AudioEditModel>();
        model.BeatA = defaultBeatA;
        model.BeatB = defaultBeatB;

        // 设置 UI dropdown 默认值
        if (beatADropdown != null)
            beatADropdown.value = Mathf.Clamp(defaultBeatA - 1, 0, beatADropdown.options.Count - 1);

        if (beatBDropdown != null)
        {
            int[] bOptions = { 1, 2, 4, 8 };
            for (int i = 0; i < bOptions.Length; i++)
            {
                if (bOptions[i] == defaultBeatB)
                {
                    beatBDropdown.value = i;
                    break;
                }
            }
        }

        // 确保索引合法
        if (audioDataList == null || audioDataList.Count == 0 || selectedIndex >= audioDataList.Count)
        {
            Debug.LogWarning("无效的音频设置索引");
            return;
        }

        mAudioDataSO audioSet = audioDataList[selectedIndex];
        if (audioSet == null || targetAttributes.Count < 12)
        {
            Debug.LogWarning("未正确配置 Target Attributes 或 AudioSet");
            return;
        }

        // 构建音频数组顺序
        AudioClip[] clips = new AudioClip[]
        {
            audioSet.MainAudio,          // 0
            audioSet.FailAudio,          // 1
            audioSet.SucceedUp,          // 2
            audioSet.SucceedDown,        // 3
            audioSet.SucceedLeft,        // 4
            audioSet.SucceedRight,       // 5
            audioSet.SucceedClick,       // 6
            audioSet.TipsUp,             // 7
            audioSet.TipsDown,           // 8
            audioSet.TipsLeft,           // 9
            audioSet.TipsRight,          //10
            audioSet.TipsClick           //11
        };

        for (int i = 0; i < clips.Length; i++)
        {
            var clip = clips[i];
            var attr = targetAttributes[i];
            if (clip == null || attr == null) continue;

            SelectManager.SetAttribute(attr);
            attr.SetShowFileName(clip.name);
            attr.RunAction(clip);

            switch (i)
            {
                case 0:
                    this.SendCommand(new SetAudioEditAudioCommand(clip));
                    StartCoroutine(DelayedBPMInit(initialBPM));
                    StartCoroutine(GenerateWaveformFrom(clip));
                    break;
                case 1:
                    this.SendCommand(new SetAudioEditAudioLoseAudioCommand(clip));
                    break;
                case 2:
                    this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.SwipeUp, clip));
                    break;
                case 3:
                    this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.SwipeDown, clip));
                    break;
                case 4:
                    this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.SwipeLeft, clip));
                    break;
                case 5:
                    this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.SwipeRight, clip));
                    break;
                case 6:
                    this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.Click, clip));
                    break;
                case 7:
                    model.UpTipsAudioClip = clip;
                    break;
                case 8:
                    model.DownTipsAudioClip = clip;
                    break;
                case 9:
                    model.LeftTipsAudioClip = clip;
                    break;
                case 10:
                    model.RightTipsAudioClip = clip;
                    break;
                case 11:
                    model.ClickTipsAudioCLip = clip;
                    break;
            }
        }
    }

    IEnumerator DelayedBPMInit(int bpm)
    {
        yield return null;
        this.SendCommand(new SetAudioEditAudioBPMCommand(bpm));
        this.SendEvent(new BPMChangeValue { BPM = bpm });
    }

    private IEnumerator GenerateWaveformFrom(AudioClip clip, float pps = 100f)
    {
        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip.LoadAudioData();
            yield return new WaitUntil(() => clip.loadState == AudioDataLoadState.Loaded);
        }

        int width = Mathf.CeilToInt(clip.length * pps);
        int height = waveformResolution.y;
        int halfHeight = height / 2;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        // 初始化背景颜色
        Color[] bgColors = new Color[width * height];
        for (int i = 0; i < bgColors.Length; i++)
            bgColors[i] = waveformBGColor;
        tex.SetPixels(bgColors);

        // 获取音频数据
        int channels = clip.channels;
        int samples = clip.samples;
        float[] data = new float[samples * channels];
        clip.GetData(data, 0);
        if (data.Length < 10)
            yield break;

        int packSize = Mathf.Max(1, (int)(clip.frequency / pps));
        float[] waveform = new float[width];
        float max = 0f;

        for (int i = 0; i < width; i++)
        {
            int startIndex = i * packSize * channels;
            float sumSquares = 0f;
            int count = 0;

            for (int j = 0; j < packSize; j++)
            {
                for (int c = 0; c < channels; c++)
                {
                    int index = startIndex + j * channels + c;
                    if (index >= data.Length) break;
                    float sample = data[index];
                    sumSquares += sample * sample;
                    count++;
                }
            }

            float rms = Mathf.Sqrt(sumSquares / Mathf.Max(1, count));
            float logRms = Mathf.Pow(rms, 0.6f); // 非线性放大低能区
            waveform[i] = logRms;

            if (logRms > max) max = logRms;
        }

        if (max == 0f)
            max = 1f;

        // 绘制波形（上下对称）
        for (int x = 0; x < width; x++)
        {
            float value = waveform[x] / max;
            int yExtent = Mathf.Clamp(Mathf.RoundToInt(value * halfHeight), 1, halfHeight);
            for (int y = halfHeight - yExtent; y <= halfHeight + yExtent; y++)
            {
                tex.SetPixel(x, y, waveformColor);
            }
        }

        tex.Apply();

        waveformImage.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        waveformImage.preserveAspect = true;
        waveformImage.SetNativeSize();

        if (initialBPM > 0)
        {
            waveformImage.rectTransform.sizeDelta = new Vector2(clip.length * pps, waveformResolution.y);
        }

        UpdateWaveformImageLayout(clip, pps);
    }


    // 更新 Image 尺寸与位置
    void UpdateWaveformImageLayout(AudioClip clip, float pps)
    {
        RectTransform parentRect = waveformImage.transform.parent.GetComponent<RectTransform>();
        float parentHeight = parentRect.rect.height;

        waveformImage.rectTransform.sizeDelta = new Vector2(clip.length * pps, parentHeight);
        waveformImage.rectTransform.anchorMin = new Vector2(0, 0.5f);
        waveformImage.rectTransform.anchorMax = new Vector2(0, 0.5f);
        waveformImage.rectTransform.pivot = new Vector2(0, 0.5f);
        waveformImage.rectTransform.anchoredPosition = Vector2.zero;
    }


    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
