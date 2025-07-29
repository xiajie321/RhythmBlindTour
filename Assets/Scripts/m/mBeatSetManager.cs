using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Qf.Models.AudioEdit;

public class mBeatSetManager : MonoBehaviour
{
    [Header("节拍BPM UI")]
    public TMP_Dropdown dropdownBeatA;
    public TMP_Dropdown dropdownBeatB;
    public TMP_InputField inputBPM;

    private AudioEditModel mdl;
    private readonly int[] beatBOptions = { 1, 2, 4, 8 };

    private void Awake()
    {
        // 初始化下拉选项（如已在编辑器设置可省略）
        if (dropdownBeatA != null)
        {
            dropdownBeatA.ClearOptions();
            dropdownBeatA.AddOptions(new System.Collections.Generic.List<string> { "1", "2", "3", "4", "5", "6" });
        }
        if (dropdownBeatB != null)
        {
            dropdownBeatB.ClearOptions();
            dropdownBeatB.AddOptions(new System.Collections.Generic.List<string> { "1", "2", "4", "8" });
        }
    }

    public void Init(AudioEditModel model)
    {
        mdl = model;

        // !!!先刷一次UI
        SyncModelToUI();

        // 再注册事件监听
        dropdownBeatA.onValueChanged.AddListener(i => mdl.BeatA = i + 1);
        dropdownBeatB.onValueChanged.AddListener(i => mdl.BeatB = beatBOptions[Mathf.Clamp(i, 0, beatBOptions.Length - 1)]);
        inputBPM.onEndEdit.AddListener(str =>
        {
            int bpm = 60;
            int.TryParse(str, out bpm);
            mdl.BPM = bpm;
        });
    }


    /// <summary>
    /// 将Model的值刷到UI控件
    /// </summary>
    public void SyncModelToUI()
    {
        if (mdl == null) return;

        // 彻底移除所有监听
        dropdownBeatA.onValueChanged.RemoveAllListeners();
        dropdownBeatB.onValueChanged.RemoveAllListeners();
        inputBPM.onEndEdit.RemoveAllListeners();

        // 赋值并强制刷新
        if (dropdownBeatA != null && dropdownBeatA.options.Count > 0)
        {
            dropdownBeatA.value = Mathf.Clamp(mdl.BeatA - 1, 0, dropdownBeatA.options.Count - 1);
            dropdownBeatA.RefreshShownValue();
        }
        if (dropdownBeatB != null && dropdownBeatB.options.Count > 0)
        {
            int bIdx = System.Array.IndexOf(beatBOptions, mdl.BeatB);
            dropdownBeatB.value = bIdx == -1 ? 0 : bIdx;
            dropdownBeatB.RefreshShownValue();
        }
        if (inputBPM != null)
            inputBPM.text = mdl.BPM.ToString();

        // 再重新注册监听（顺序不能错！）
        dropdownBeatA.onValueChanged.AddListener(i => mdl.BeatA = i + 1);
        dropdownBeatB.onValueChanged.AddListener(i => mdl.BeatB = beatBOptions[Mathf.Clamp(i, 0, beatBOptions.Length - 1)]);
        inputBPM.onEndEdit.AddListener(str =>
        {
            int bpm = 60;
            int.TryParse(str, out bpm);
            mdl.BPM = bpm;
        });
    }


    /// <summary>
    /// 将UI当前数据写回Model（用于Save前手动同步）
    /// </summary>
    public void WriteBackToModel()
    {
        if (mdl == null) return;
        mdl.BeatA = dropdownBeatA.value + 1;
        mdl.BeatB = beatBOptions[Mathf.Clamp(dropdownBeatB.value, 0, beatBOptions.Length - 1)];
        int bpm = 60;
        int.TryParse(inputBPM.text, out bpm);
        mdl.BPM = bpm;
    }
}
