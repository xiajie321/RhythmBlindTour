using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestMeasure_MeasureSettingManager : MonoBehaviour, IController
{
    #region Inspector Fields
    [Header("References")]
    [Tooltip("父级容器，包含所有小节（Measure）的容器")]
    public RectTransform progressBar;

    [Header("Dropdown Inputs")]
    [Tooltip("每小节的拍数（分子）的TMP_Dropdown")]
    public TMP_Dropdown beatsPerMeasureDropdown;
    [Tooltip("拍号分母的TMP_Dropdown，例如4代表四分音符为一拍")]
    public TMP_Dropdown beatDenominatorDropdown;

    [Header("Settings")]
    [Tooltip("默认刻度颜色（非第一拍）")]
    public Color scaleColor = Color.black;
    [Tooltip("第一拍的颜色")]
    public Color firstBeatColor = Color.red;
    [Tooltip("每秒的像素单位（控制时长与像素的换算比例）")]
    public float pixelUnitsPerSecond = 100f;

    [Header("Scale Settings")]
    [Tooltip("刻度的高度")]
    public float scaleHeight = 80f;

    [Header("应用于切换选项的设置")]
    [Tooltip("默认刻度宽度")]
    public float defaultBeatWidth = 5f;
    [Tooltip("选中刻度宽度")]
    public float selectedBeatWidth = 10f;
    [Tooltip("选中刻度颜色（默认绿色）")]
    public Color selectedBeatColor = Color.green;
    #endregion

    AudioEditModel editModel;

    // 存储生成的小节容器及每个小节下的节拍标记
    List<GameObject> measureGameObjects = new List<GameObject>();
    List<List<GameObject>> beatGameObjects = new List<List<GameObject>>();

    // 当前时间针所在的小节和节拍索引（默认初始为0）
    private int currentMeasureIndex = 0;
    private int currentBeatIndex = 0;

    #region Event Subscription
    void Start()
    {
        editModel = this.GetModel<AudioEditModel>();
        SubscribeToEvents();
        // 初始不生成刻度，等待 BPM 改变时生成
    }

    private void SubscribeToEvents()
    {
        this.RegisterEvent<BPMChangeValue>(v =>
        {
            GenerateScales();
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    #endregion

    #region Calculation Methods
    /// <summary>
    /// 根据 BPM 和拍号分母计算单拍时长（秒）  
    /// BPM 通常以四分音符计，所以公式为：beatDuration = 60 / BPM * (4 / beatDenom)
    /// </summary>
    float CalculateBeatDuration(float bpm, int beatDenom)
    {
        return 60f / bpm * (4f / beatDenom);
    }

    /// <summary>
    /// 根据 BPM 和时间签名计算小节时长（秒）  
    /// measureDuration = beatDuration * beatsPerMeasure
    /// </summary>
    float CalculateMeasureDuration(float bpm, int beatsPerMeasure, int beatDenom)
    {
        float beatDuration = CalculateBeatDuration(bpm, beatDenom);
        return beatDuration * beatsPerMeasure;
    }
    #endregion

    #region Scale Generation Methods
    /// <summary>
    /// 清理 progressBar 下已有的小节和节拍标记
    /// </summary>
    void ClearExistingScales()
    {
        foreach (var measure in measureGameObjects)
        {
            Destroy(measure);
        }
        measureGameObjects.Clear();
        beatGameObjects.Clear();
    }

    /// <summary>
    /// 根据 BPM 与时间签名设置，生成小节（Measure）和节拍（Beat）刻度展示  
    /// 算法思路：  
    /// 1. 从 Dropdown 中读取每小节拍数和拍号分母  
    /// 2. 计算单拍时长和小节时长  
    /// 3. 根据音频总时长计算小节数量  
    /// 4. 为每个小节生成容器，并在其下生成 beatsPerMeasure 个 Beat  
    /// 5. 每个 Beat 的位置按小节宽度均分，第一拍使用特殊颜色  
    /// 生成完成后，重置当前时间针为第一个小节的第一个节拍，并更新选中样式
    /// </summary>
    public void GenerateScales()
    {
        if (editModel == null || editModel.EditAudioClip == null)
        {
            Debug.LogError("EditAudioClip or AudioEditModel not set.");
            return;
        }

        // 从 Dropdown 中读取拍号设置
        int beatsPerMeasure;
        if (!int.TryParse(beatsPerMeasureDropdown.options[beatsPerMeasureDropdown.value].text, out beatsPerMeasure))
        {
            Debug.LogError("无法解析每小节拍数");
            return;
        }

        int beatDenom;
        if (!int.TryParse(beatDenominatorDropdown.options[beatDenominatorDropdown.value].text, out beatDenom))
        {
            Debug.LogError("无法解析拍号分母");
            return;
        }

        // 获取当前 BPM
        float bpm = editModel.BPM;
        // 计算单拍时长与小节时长
        float beatDuration = CalculateBeatDuration(bpm, beatDenom);
        float measureDuration = CalculateMeasureDuration(bpm, beatsPerMeasure, beatDenom);

        // 根据音频总时长计算小节数量（向上取整确保覆盖整个音频）
        int numberOfMeasures = Mathf.CeilToInt(editModel.EditAudioClip.length / measureDuration);

        // 先清理已有刻度
        ClearExistingScales();

        // 生成小节和节拍刻度
        for (int m = 0; m < numberOfMeasures; m++)
        {
            // 小节的X位置
            float measurePosX = m * measureDuration * pixelUnitsPerSecond;
            // 若超出音频长度则停止生成
            if (measurePosX > editModel.EditAudioClip.length * pixelUnitsPerSecond)
                break;

            // 创建小节容器
            GameObject measureGO = new GameObject("Measure" + (m + 1));
            measureGO.transform.SetParent(progressBar, false);
            RectTransform measureRT = measureGO.AddComponent<RectTransform>();
            // 小节宽度：小节时长对应的像素值
            measureRT.sizeDelta = new Vector2(measureDuration * pixelUnitsPerSecond, scaleHeight);
            measureRT.anchoredPosition = new Vector2(measurePosX, 0);
            measureRT.anchorMax = Vector2.zero;
            measureRT.anchorMin = Vector2.zero;
            measureRT.pivot = new Vector2(0, 0); // 左下角对齐

            measureGameObjects.Add(measureGO);

            // 生成该小节内的节拍标记
            List<GameObject> beatsInMeasure = new List<GameObject>();
            for (int b = 0; b < beatsPerMeasure; b++)
            {
                // 计算当前节拍在小节内的X位置
                float beatPosX = b * (measureDuration * pixelUnitsPerSecond / beatsPerMeasure);

                GameObject beatGO = new GameObject("Beat" + (b + 1));
                beatGO.transform.SetParent(measureGO.transform, false);
                RectTransform beatRT = beatGO.AddComponent<RectTransform>();
                // 第一拍全高，其余节拍半高（可根据需要调整）
                float beatHeight = (b == 0) ? scaleHeight : scaleHeight / 2f;
                // 使用默认宽度
                beatRT.sizeDelta = new Vector2(defaultBeatWidth, beatHeight);
                beatRT.anchoredPosition = new Vector2(beatPosX, 0);
                beatRT.anchorMax = Vector2.zero;
                beatRT.anchorMin = Vector2.zero;
                beatRT.pivot = new Vector2(0.5f, 0);

                // 添加 Image 并设置颜色
                Image beatImage = beatGO.AddComponent<Image>();
                beatImage.color = (b == 0) ? firstBeatColor : scaleColor;

                beatsInMeasure.Add(beatGO);
            }
            beatGameObjects.Add(beatsInMeasure);
        }

        // 重置时间针为第一个小节的第一个节拍
        currentMeasureIndex = 0;
        currentBeatIndex = 0;
        // 更新选中刻度的样式
        UpdateSelectedBeatAppearance();
    }
    #endregion

    #region Test_时间针移动方法
    /// <summary>
    /// 切换到当前所在小节的前一个小节的第一个节拍处  
    /// 如果已经在第一个小节，则不进行操作
    /// </summary>
    public void Test_SwitchToPreviousMeasureFirstBeat()
    {
        if (currentMeasureIndex > 0)
        {
            MoveMeasure(-1);
            Debug.Log("切换到小节 " + (currentMeasureIndex + 1) + " 的第一个节拍");
            UpdateSelectedBeatAppearance();
        }
        else
        {
            Debug.Log("已经在第一个小节，无法向前切换");
        }
    }

    /// <summary>
    /// 切换到当前所在小节的后一个小节的第一个节拍处  
    /// 如果已经在最后一个小节，则不进行操作
    /// </summary>
    public void Test_SwitchToNextMeasureFirstBeat()
    {
        if (currentMeasureIndex < measureGameObjects.Count - 1)
        {
            MoveMeasure(+1);
            Debug.Log("切换到小节 " + (currentMeasureIndex + 1) + " 的第一个节拍");
            UpdateSelectedBeatAppearance();
        }
        else
        {
            Debug.Log("已经在最后一个小节，无法向后切换");
        }
    }

    /// <summary>
    /// 切换到当前所在节拍的前一个节拍处  
    /// 如果在第一个小节的第一个节拍，则不进行操作
    /// </summary>
    public void Test_SwitchToPreviousBeat()
    {
        if (currentMeasureIndex == 0 && currentBeatIndex == 0)
        {
            Debug.Log("已经在第一个节拍，无法向前切换");
            return;
        }
        MoveBeat(-1);
        Debug.Log("切换到小节 " + (currentMeasureIndex + 1) + " 的节拍 " + (currentBeatIndex + 1));
        UpdateSelectedBeatAppearance();
    }

    /// <summary>
    /// 切换到当前所在节拍的后一个节拍处  
    /// 如果在最后一个小节的最后一个节拍，则不进行操作
    /// </summary>
    public void Test_SwitchToNextBeat()
    {
        if (currentMeasureIndex == measureGameObjects.Count - 1 &&
            currentBeatIndex == beatGameObjects[currentMeasureIndex].Count - 1)
        {
            Debug.Log("已经在最后一个节拍，无法向后切换");
            return;
        }
        MoveBeat(+1);
        Debug.Log("切换到小节 " + (currentMeasureIndex + 1) + " 的节拍 " + (currentBeatIndex + 1));
        UpdateSelectedBeatAppearance();
    }

    /// <summary>
    /// 私有方法：移动小节  
    /// 参数 direction 为 -1 或 +1，表示向前或向后移动  
    /// 移动后将当前节拍索引重置为 0
    /// </summary>
    private void MoveMeasure(int direction)
    {
        int newMeasureIndex = currentMeasureIndex + direction;
        if (newMeasureIndex >= 0 && newMeasureIndex < measureGameObjects.Count)
        {
            currentMeasureIndex = newMeasureIndex;
            currentBeatIndex = 0;
        }
    }

    /// <summary>
    /// 私有方法：移动节拍  
    /// 参数 direction 为 -1 或 +1，表示向前或向后移动  
    /// 如果移动超出当前小节范围，则自动切换到相邻小节的对应节拍处
    /// </summary>
    private void MoveBeat(int direction)
    {
        int newBeatIndex = currentBeatIndex + direction;
        if (newBeatIndex < 0)
        {
            // 若存在前一个小节，则切换到前一小节的最后一个节拍
            if (currentMeasureIndex > 0)
            {
                currentMeasureIndex--;
                currentBeatIndex = beatGameObjects[currentMeasureIndex].Count - 1;
            }
        }
        else if (newBeatIndex >= beatGameObjects[currentMeasureIndex].Count)
        {
            // 若存在下一个小节，则切换到下一个小节的第一个节拍
            if (currentMeasureIndex < measureGameObjects.Count - 1)
            {
                currentMeasureIndex++;
                currentBeatIndex = 0;
            }
        }
        else
        {
            currentBeatIndex = newBeatIndex;
        }
    }
    #endregion

    #region 选中刻度样式更新方法
    /// <summary>
    /// 更新选中（时间针所在）的刻度样式  
    /// 被选中的 Beat 使用选中宽度和选中颜色，其余恢复默认宽度并显示原有颜色（小节第一拍：firstBeatColor，其它：scaleColor）
    /// </summary>
    private void UpdateSelectedBeatAppearance()
    {
        // 遍历所有小节
        for (int m = 0; m < beatGameObjects.Count; m++)
        {
            List<GameObject> beats = beatGameObjects[m];
            for (int b = 0; b < beats.Count; b++)
            {
                GameObject beatGO = beats[b];
                RectTransform rt = beatGO.GetComponent<RectTransform>();
                Image img = beatGO.GetComponent<Image>();
                // 获取原有高度（不改变）
                float height = rt.sizeDelta.y;
                // 如果当前为选中状态
                if (m == currentMeasureIndex && b == currentBeatIndex)
                {
                    rt.sizeDelta = new Vector2(selectedBeatWidth, height);
                    img.color = selectedBeatColor;
                }
                else
                {
                    rt.sizeDelta = new Vector2(defaultBeatWidth, height);
                    // 非选中状态，第一拍使用 firstBeatColor，其它使用 scaleColor
                    img.color = (b == 0) ? firstBeatColor : scaleColor;
                }
            }
        }
    }
    #endregion

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
