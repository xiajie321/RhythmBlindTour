using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Qf.ClassDatas.AudioEdit;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models;
using Qf.Models.AudioEdit;
using QFramework;
using Qf.Managers;
using System.Globalization;

public class UIAttributeSetPanel : MonoBehaviour, IController
{
    AudioEditModel editModel;
    DataCachingModel cModel;

    [SerializeField] TMP_Text Name;
    [SerializeField] Button UpButton;
    [SerializeField] Button DownButton;

    [Header("鼓点类型（只改当前鼓点）")]
    [SerializeField] UIDorpAttribute DrwmType;

    [Header("仅展示：该鼓点创建时的预制类型")]
    [SerializeField] TMP_Text PrefabTypeLabel; // 只显示文本（同步过滤标签）

    [Header("音频与音量（对同类全部鼓点批量）")]
    [SerializeField] UIFileAttribute PreAdventAudio;
    [SerializeField] UIFileAttribute SucceedAudio;
    [SerializeField] UIFileAttribute LoseAudioClip;
    [SerializeField] UISliderAttribute PreAdventAudioVolum;
    [SerializeField] UISliderAttribute SucceedAudioVolum;
    [SerializeField] UISliderAttribute LoseAudioClipVolum;

    [Header("时间参数（对同类全部鼓点批量）")]
    [SerializeField] UIValueAttribute TimeOfExistence;
    [SerializeField] UIValueAttribute PreAdventAudioClipOffsetTime;
    [SerializeField] UIValueAttribute CenterTimeAttribute;
    [SerializeField] UIValueAttribute PreAdventAbsoluteTime;

    [Header("其他")]
    [SerializeField] mUIButtonAttribute IsDemoDrumButton;
    [SerializeField] Button MoveForwardButton;
    [SerializeField] Button MoveBackwardButton;
    [SerializeField] Button RemoveButton;

    public class OnSelectDrumByCode { public string DrumCode; }

    UIAudioEditTimeHand timeHand;
    mUIDrumsInspectorPanel listPanel;
    int index;
    float thisTime;
    DrumsLoadData ls;
    List<GameObject> gameObjects = new();

    private string currentDrumCode = null;
    private List<string> drumCodeList = new();

    public IArchitecture GetArchitecture() => GameBody.Interface;

    private void OnEnable()
    {
        if (editModel == null) editModel = this.GetModel<AudioEditModel>();
        if (cModel == null) cModel = this.GetModel<DataCachingModel>();

        EnsureRefs();
        GenerateDrumCodeList();

        this.RegisterEvent<UIAudioEditDrumsOrbit.OnSelectDrumByCode>(e =>
        {
            currentDrumCode = e.DrumCode;
            ShowDrumByCode(currentDrumCode);
        }).UnRegisterWhenDisabled(gameObject);

        this.RegisterEvent<OnUpdateAudioEditDrumsUI>(v =>
        {
            if (editModel.Mode.Equals(SystemModeData.PlayMode)) return;
            if (AudioEditManager.Instance != null && AudioEditManager.Instance.IsControlRunning) return;

            GenerateDrumCodeList();
            if (!string.IsNullOrEmpty(currentDrumCode))
            {
                ShowDrumByCode(currentDrumCode);
                UpdateSwitchButtonState();
            }
            else
            {
                Show(false);
                UpdateSwitchButtonState();
            }
        }).UnRegisterWhenDisabled(gameObject);

        GenerateDrumCodeList();
        if (drumCodeList.Count > 0)
        {
            currentDrumCode = drumCodeList[0];
            ShowDrumByCode(currentDrumCode);
            UpdateSwitchButtonState();
        }
        else
        {
            currentDrumCode = null;
            Show(false);
            UpdateSwitchButtonState();
        }
    }

    void Start()
    {
        if (editModel == null) editModel = this.GetModel<AudioEditModel>();
        if (cModel == null) cModel = this.GetModel<DataCachingModel>();

        gameObjects.Add(DrwmType.transform.parent.gameObject);
        if (PrefabTypeLabel != null) gameObjects.Add(PrefabTypeLabel.transform.parent.gameObject);

        gameObjects.Add(PreAdventAudio.transform.parent.gameObject);
        gameObjects.Add(SucceedAudio.transform.parent.gameObject);
        gameObjects.Add(LoseAudioClip.transform.parent.gameObject);
        gameObjects.Add(PreAdventAudioVolum.transform.parent.gameObject);
        gameObjects.Add(SucceedAudioVolum.transform.parent.gameObject);
        gameObjects.Add(LoseAudioClipVolum.transform.parent.gameObject);
        gameObjects.Add(TimeOfExistence.transform.parent.gameObject);
        gameObjects.Add(PreAdventAudioClipOffsetTime.transform.parent.gameObject);
        gameObjects.Add(CenterTimeAttribute.transform.parent.gameObject);
        gameObjects.Add(PreAdventAbsoluteTime.transform.parent.gameObject);
        gameObjects.Add(MoveForwardButton.transform.parent.gameObject);
        gameObjects.Add(MoveBackwardButton.transform.parent.gameObject);

        MoveForwardButton.onClick.AddListener(() => OffsetCurrentDrumTime(0.01f));
        MoveBackwardButton.onClick.AddListener(() => OffsetCurrentDrumTime(-0.01f));

        EnsureRefs();
    }

    private void EnsureRefs()
    {
        if (timeHand == null) timeHand = FindObjectOfType<UIAudioEditTimeHand>();
        if (listPanel == null) listPanel = FindObjectOfType<mUIDrumsInspectorPanel>();
    }

    void GenerateDrumCodeList()
    {
        drumCodeList.Clear();
        var all = new List<string>();
        foreach (var pair in editModel.TimeLineData)
            for (int i = 0; i < pair.Value.Count; i++)
                all.Add(pair.Value[i].DrwmsData.DrumCode);
        drumCodeList.AddRange(all);
    }

    public static void DecodeDrumCode(string drumCode, out float thisTime, out int index)
    {
        if (drumCode.Length < 2) { thisTime = 0f; index = 0; return; }
        string timePart = drumCode.Substring(0, drumCode.Length - 1);
        string idxPart = drumCode.Substring(drumCode.Length - 1, 1);
        int timeInt = int.Parse(timePart);
        thisTime = timeInt * 0.01f;
        index = int.Parse(idxPart); // IndexType 统一为数字（此处用作操作类型索引）
    }

    public static DrumsLoadData FindDrumByCode(AudioEditModel model, string drumCode)
    {
        DecodeDrumCode(drumCode, out float thisTime, out int typeIndex);
        float roundedTarget = (float)Math.Round(thisTime, 2);
        foreach (var key in model.TimeLineData.Keys)
        {
            float roundedKey = (float)Math.Round(key, 2);
            if (Mathf.Approximately(roundedKey, roundedTarget))
            {
                var list = model.TimeLineData[key];
                foreach (var drum in list)
                {
                    int opIndex = (int)drum.DrwmsData.DtheTypeOfOperation;
                    if (opIndex == typeIndex) return drum;
                }
                return null;
            }
        }
        return null;
    }

    public void ShowDrumByCode(string drumCode)
    {
        var editModel = GameBody.Interface.GetModel<AudioEditModel>();
        var drum = UIAttributeSetPanel.FindDrumByCode(editModel, drumCode);
        if (drum != null)
        {
            ls = drum;
            DecodeDrumCode(drumCode, out thisTime, out index);
            UpdateName();
            UpdateDataShow(ls);
            Show(true);
            UpdateSwitchButtonState();

            // 仅当前鼓点：类型切换
            DrwmType.SetAction(v =>
            {
                ls.DrwmsData.DtheTypeOfOperation = (TheTypeOfOperation)v;
                DrwmType.SetDropdownVlaue(ls.DrwmsData.DtheTypeOfOperation);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            // 仅当前鼓点：中心时间
            CenterTimeAttribute.SetAction(v =>
            {
                if (!float.TryParse(v.ToString(), out float newCenterTime)) return;
                float oldCenterTime = ls.DrwmsData.CenterTime;
                if (Mathf.Approximately(oldCenterTime, newCenterTime)) return;
                MoveDrumToNewTime(oldCenterTime, newCenterTime);
            });

            // 仅当前鼓点：提示绝对时间 = Center - Offset
            PreAdventAbsoluteTime.SetAction(v =>
            {
                if (!float.TryParse(v.ToString(), out float preAdventTime)) return;
                float offset = ls.DrwmsData.VPreAdventAudioClipOffsetTime;
                float newCenterTime = (float)Math.Round(preAdventTime + offset, 2);
                float oldCenterTime = ls.DrwmsData.CenterTime;
                if (Mathf.Approximately(newCenterTime, oldCenterTime)) return;
                MoveDrumToNewTime(oldCenterTime, newCenterTime);
            });

            // ——以下“批量应用到同类”，根据 PrefabType 做过滤——

            PreAdventAudio.SetAction(v =>
            {
                if (v == null) return;
                var clip = v as AudioClip;
                if (clip == null) return;

                string clipName = clip.name;
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, d => d.DrwmsData.FPreAdventAudioClipPath = clipName);
                PreAdventAudio.SetShowFileName(clipName);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            SucceedAudio.SetAction(v =>
            {
                if (v == null) return;
                var clip = v as AudioClip;
                if (clip == null) return;

                string clipName = clip.name;
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, d => d.DrwmsData.FSucceedAudioClipPath = clipName);
                SucceedAudio.SetShowFileName(clipName);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            LoseAudioClip.SetAction(v =>
            {
                if (v == null) return;
                var clip = v as AudioClip;
                if (clip == null) return;

                string clipName = clip.name;
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, d => d.DrwmsData.FLoseAudioClipPath = clipName);
                LoseAudioClip.SetShowFileName(clipName);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            // 提示音量（b/d 不应用；c/g 允许应用）
            PreAdventAudioVolum.SetAction(v =>
            {
                float vol = Mathf.Clamp01((float)v);
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, d =>
                {
                    if (!IsSkipTipVolume(d.DrwmsData.PrefabType))
                        d.MusicData.SPreAdventVolume = vol;
                });
                PreAdventAudioVolum.SetValueShow(vol);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            // 成功音量（a 不应用；其他都应用，包含 c/g）
            SucceedAudioVolum.SetAction(v =>
            {
                float vol = Mathf.Clamp01((float)v);
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, d =>
                {
                    if (!IsSkipSuccessVolume(d.DrwmsData.PrefabType))
                        d.MusicData.SSucceedVolume = vol;
                });
                SucceedAudioVolum.SetValueShow(vol);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            // 失败音量（全部可应用；注意 c/g 创建时被设为 0，可被改动）
            LoseAudioClipVolum.SetAction(v =>
            {
                float vol = Mathf.Clamp01((float)v);
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, d => d.MusicData.SLoseVolume = vol);
                LoseAudioClipVolum.SetValueShow(vol);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            // 存在时间（a/b 不应用；c/g 可应用，但创建时为 0）
            TimeOfExistence.SetAction(v =>
            {
                if (float.TryParse(v.ToString(), out float result))
                {
                    var curType = ls.DrwmsData.DtheTypeOfOperation;
                    ApplyToAllDrumsOfType(curType, d =>
                    {
                        if (!IsSkipExistence(d.DrwmsData.PrefabType))
                            d.DrwmsData.VTimeOfExistence = result;
                    });
                    TimeOfExistence.SetValueShow(result.ToString());
                    this.SendEvent<OnUpdateAudioEditDrumsUI>();
                }
            });

            // 提示偏移（b/d 不应用；其余应用）
            PreAdventAudioClipOffsetTime.SetAction(v =>
            {
                if (float.TryParse(v.ToString(), out float result))
                {
                    var curType = ls.DrwmsData.DtheTypeOfOperation;
                    ApplyToAllDrumsOfType(curType, d =>
                    {
                        if (!IsSkipTipOffset(d.DrwmsData.PrefabType))
                            d.DrwmsData.VPreAdventAudioClipOffsetTime = result;
                    });
                    PreAdventAudioClipOffsetTime.SetValueShow(result.ToString());
                    this.SendEvent<OnUpdateAudioEditDrumsUI>();
                }
            });
        }
        else
        {
            Show(false);
        }
    }

    private static string PrefabTypeToText(int prefabType)
    {
        var t = (UIAudioEditDrumsOrbit.PrefabDrumType)prefabType;
        switch (t)
        {
            case UIAudioEditDrumsOrbit.PrefabDrumType.TipOnly: return "仅提示音";
            case UIAudioEditDrumsOrbit.PrefabDrumType.AnswerOnly: return "仅回答音";
            case UIAudioEditDrumsOrbit.PrefabDrumType.JudgeOnly: return "仅判定区";
            case UIAudioEditDrumsOrbit.PrefabDrumType.PlaceAtCenter: return "判定区位置";
            case UIAudioEditDrumsOrbit.PrefabDrumType.PlaceAtTip: return "提示音位置";
            case UIAudioEditDrumsOrbit.PrefabDrumType.Audition: return "试听";
            default: return $"未知({prefabType})";
        }
    }

    public void UpQh()
    {
        int pos = drumCodeList.IndexOf(currentDrumCode);
        if (pos > 0)
        {
            currentDrumCode = drumCodeList[pos - 1];
            ShowDrumByCode(currentDrumCode);
            UpdateSwitchButtonState();
        }
    }

    public void DownQh()
    {
        int pos = drumCodeList.IndexOf(currentDrumCode);
        if (pos >= 0 && pos < drumCodeList.Count - 1)
        {
            currentDrumCode = drumCodeList[pos + 1];
            ShowDrumByCode(currentDrumCode);
            UpdateSwitchButtonState();
        }
    }

    void UpdateDataShow(DrumsLoadData ls)
    {
        DrwmType.SetDropdownVlaue(ls.DrwmsData.DtheTypeOfOperation);

        if (PrefabTypeLabel != null)
            PrefabTypeLabel.text = "预制类型：" + PrefabTypeToText(ls.DrwmsData.PrefabType);

        PreAdventAudio.SetShowFileName(ls.DrwmsData.FPreAdventAudioClipPath);
        SucceedAudio.SetShowFileName(ls.DrwmsData.FSucceedAudioClipPath);
        LoseAudioClip.SetShowFileName(ls.DrwmsData.FLoseAudioClipPath);
        PreAdventAudioVolum.SetValueShow(ls.MusicData.SPreAdventVolume);
        SucceedAudioVolum.SetValueShow(ls.MusicData.SSucceedVolume);
        LoseAudioClipVolum.SetValueShow(ls.MusicData.SLoseVolume);
        TimeOfExistence.SetValueShow(ls.DrwmsData.VTimeOfExistence.ToString());
        PreAdventAudioClipOffsetTime.SetValueShow(ls.DrwmsData.VPreAdventAudioClipOffsetTime.ToString());
        CenterTimeAttribute.SetValueShow(ls.DrwmsData.CenterTime.ToString("0.00"));

        float preAdventAbsTime = ls.DrwmsData.CenterTime - ls.DrwmsData.VPreAdventAudioClipOffsetTime;
        PreAdventAbsoluteTime.SetValueShow(preAdventAbsTime.ToString("0.00"));
    }

    #region 位移微调
    void OffsetCurrentDrumTime(float offset)
    {
        if (ls == null) return;

        float oldTime = ls.DrwmsData.CenterTime;
        float newTime = (float)Math.Round(oldTime + offset, 2, MidpointRounding.ToEven);
        if (Mathf.Approximately(oldTime, newTime)) return;

        // —— TTS：鼓点加/减 {offset} 秒，当前时间，{newTime} 秒 ——
        string verb = offset >= 0 ? "加" : "减";
        float absOffset = (float)Math.Round(Mathf.Abs(offset), 2, MidpointRounding.ToEven);
        string offsetTxt = absOffset.ToString("0.00", CultureInfo.InvariantCulture) + "秒";
        string newTimeTxt = mTTS.FormatSecondsForTTS(newTime, mTTS.TimeReadStyle.NumericTokens) + "秒";
        mTTS.Speak($"鼓点{verb}{offsetTxt}，当前时间，{newTimeTxt}");

        MoveDrumToNewTime(oldTime, newTime);
    }

    void MoveDrumToNewTime(float oldTime, float newTime)
    {
        EnsureRefs();

        if (editModel.TimeLineData.ContainsKey(oldTime))
        {
            var list = editModel.TimeLineData[oldTime];
            list.Remove(ls);
            if (list.Count == 0)
                editModel.TimeLineData.Remove(oldTime);
        }

        ls.DrwmsData.CenterTime = newTime;
        ls.DrwmsData.DrumCode = UIAudioEditDrumsOrbit.GenerateDrumCode(newTime, index);
        currentDrumCode = ls.DrwmsData.DrumCode;

        if (!editModel.TimeLineData.ContainsKey(newTime))
            editModel.TimeLineData[newTime] = new List<DrumsLoadData>();
        editModel.TimeLineData[newTime].Add(ls);

        editModel.ThisTime = newTime;

        UIAudioEditDrums[] allDrums = FindObjectsOfType<UIAudioEditDrums>();
        foreach (var drum in allDrums)
        {
            if (Mathf.Approximately(drum.ThisTime, oldTime) && drum.Index == index)
            {
                drum.ThisTime = newTime;
                drum.DrumCode = ls.DrwmsData.DrumCode;
                break;
            }
        }

        GenerateDrumCodeList();
        this.SendEvent<OnUpdateThisTime>();
        this.SendEvent<OnUpdateAudioEditDrumsUI>();

        if (timeHand != null) timeHand.SetTime(newTime); // 仅保留这一次广播
        if (listPanel != null) listPanel.RefreshList();
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode() { DrumCode = currentDrumCode });

    }
    #endregion

    public void Show(bool isbool)
    {
        foreach (var i in gameObjects) i.SetActive(isbool);
        RemoveButton.gameObject.SetActive(isbool);
    }

    public void RemoveDrwm()
    {
        EnsureRefs();
        if (listPanel != null)
            listPanel.RemoveCurrentSelectedDrumByDrumCode(currentDrumCode);
    }

    public void UpdateName(string str = "")
    {
        if (!string.IsNullOrEmpty(str)) { Name.text = str; return; }
        if (ls != null && ls.DrwmsData != null)
            Name.text = $"鼓点编号: {ls.DrwmsData.DrumCode}";
        else
            Name.text = "当前未选中鼓点";
    }

    private void UpdateSwitchButtonState()
    {
        int pos = drumCodeList.IndexOf(currentDrumCode);
        UpButton.gameObject.SetActive(pos > 0);
        DownButton.gameObject.SetActive(pos >= 0 && pos < drumCodeList.Count - 1);
    }

    void ApplyToAllDrumsOfType(TheTypeOfOperation type, Action<DrumsLoadData> action)
    {
        foreach (var pair in editModel.TimeLineData)
            foreach (var drum in pair.Value)
                if (drum.DrwmsData.DtheTypeOfOperation == type)
                    action(drum);
    }

    // ——批量同步过滤规则（基于 PrefabType）——
    // a: 提示音独立 → 不同步 成功音量、存在时间
    // b: 回答音独立 → 不同步 提示音量/偏移、存在时间
    // d: 判定区间独立 → 不同步 提示音量/偏移
    private static bool IsSkipSuccessVolume(int prefabType)
        => prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.TipOnly;

    private static bool IsSkipTipVolume(int prefabType)
        => prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.AnswerOnly
        || prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.JudgeOnly;

    private static bool IsSkipTipOffset(int prefabType)
        => prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.AnswerOnly
        || prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.JudgeOnly;

    private static bool IsSkipExistence(int prefabType)
        => prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.TipOnly
        || prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.AnswerOnly;

    void Update() { }
}
