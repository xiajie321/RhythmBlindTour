using System;
using System.Collections;
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
using Unity.Burst.Intrinsics;
using static UIAudioEditDrumsOrbit;
using Qf.Managers;

public class UIAttributeSetPanel : MonoBehaviour, IController
{
    AudioEditModel editModel;
    DataCachingModel cModel;

    [SerializeField] TMP_Text Name;
    [SerializeField] Button UpButton;
    [SerializeField] Button DownButton;
    [SerializeField] UIDorpAttribute DrwmType;
    [SerializeField] UIFileAttribute PreAdventAudio;
    [SerializeField] UIFileAttribute SucceedAudio;
    [SerializeField] UIFileAttribute LoseAudioClip;
    [SerializeField] UISliderAttribute PreAdventAudioVolum;
    [SerializeField] UISliderAttribute SucceedAudioVolum;
    [SerializeField] UISliderAttribute LoseAudioClipVolum;
    [SerializeField] UIValueAttribute TimeOfExistence;
    [SerializeField] UIValueAttribute PreAdventAudioClipOffsetTime;
    [SerializeField] UIValueAttribute CenterTimeAttribute;
    [SerializeField] UIValueAttribute PreAdventAbsoluteTime;
    [SerializeField] mUIButtonAttribute IsDemoDrumButton;
    [SerializeField] Button MoveForwardButton;
    [SerializeField] Button MoveBackwardButton;
    [SerializeField] Button RemoveButton;

    // [事件定义] -- mixyao/07/08
    public class OnSelectDrumByCode
    {
        public string DrumCode;
    }

    UIAudioEditTimeHand timeHand;
    mUIDrumsInspectorPanel listPanel;
    int index;
    float thisTime;
    DrumsLoadData ls;
    List<GameObject> gameObjects = new();

    // [当前鼓点唯一编号] -- mixyao/07/08
    private string currentDrumCode = null;
    // [排序后的唯一编号列表] -- mixyao/07/08
    private List<string> drumCodeList = new();

    public IArchitecture GetArchitecture() => GameBody.Interface;

    private void OnEnable()
    {
        if (editModel == null) editModel = this.GetModel<AudioEditModel>();
        if (cModel == null) cModel = this.GetModel<DataCachingModel>();

        GenerateDrumCodeList();

        // 只监听一次唯一事件即可
        this.RegisterEvent<UIAudioEditDrumsOrbit.OnSelectDrumByCode>(e =>
        {
            Debug.Log("[DrumPanel] Select: " + e.DrumCode);
            currentDrumCode = e.DrumCode;
            ShowDrumByCode(currentDrumCode);
        }).UnRegisterWhenDisabled(gameObject);

        // 鼓点信息有变（新增/删除/修改）时刷新
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

        // 初始化时自动展示第一个鼓点
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
        timeHand = FindObjectOfType<UIAudioEditTimeHand>();
        listPanel = FindObjectOfType<mUIDrumsInspectorPanel>();
    }

    // [生成当前排序下唯一编号列表] -- mixyao/07/08
    void GenerateDrumCodeList()
    {
        drumCodeList.Clear();
        var all = new List<string>();
        foreach (var pair in editModel.TimeLineData)
        {
            for (int i = 0; i < pair.Value.Count; i++)
                all.Add(pair.Value[i].DrwmsData.DrumCode);
        }
        // 排序，如有特殊要求可以排序；不排序就用现有顺序
        drumCodeList.AddRange(all);
    }


    // [通过 DrumCode 解码 thisTime/index] -- mixyao/07/09
    public static void DecodeDrumCode(string drumCode, out float thisTime, out int index)
    {
        // 例：000582 -> timeInt=58，index=2，thisTime=0.58
        if (drumCode.Length < 2)
        {
            thisTime = 0f; index = 0; return;
        }
        string timePart = drumCode.Substring(0, drumCode.Length - 1);
        string idxPart = drumCode.Substring(drumCode.Length - 1, 1);
        int timeInt = int.Parse(timePart);
        thisTime = timeInt * 0.01f;
        index = int.Parse(idxPart);
    }

    // [通过 DrumCode 直接查鼓点] -- mixyao/07/09
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
                    Debug.Log($"[真实ThisTime] 检查 ThisTime={model.ThisTime}");
                    Debug.Log($"[FindDrumByCode] 检查 ThisTime={key:F8}, typeIndex={typeIndex}, opIndex={opIndex}, DrumCode={drum.DrwmsData.DrumCode}");
                    if (opIndex == typeIndex)
                    {
                        Debug.Log($"[FindDrumByCode] 命中鼓点 DrumCode={drum.DrwmsData.DrumCode}");
                        return drum;
                    }
                }
                Debug.LogWarning($"[FindDrumByCode] 未在 ThisTime={key:F8} 下找到 typeIndex={typeIndex} 的鼓点");
                return null;
            }
        }
        Debug.LogWarning($"[FindDrumByCode] 没有找到 roundedKey={roundedTarget:F2} 的鼓点时间点");
        return null;
    }



    // [鼓点详情面板直接通过 DrumCode 唤起] -- mixyao/07/09
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

            // 仅针对当前鼓点移动
            CenterTimeAttribute.SetAction(v =>
            {
                if (!float.TryParse(v.ToString(), out float newCenterTime)) return;
                float oldCenterTime = ls.DrwmsData.CenterTime;
                if (Mathf.Approximately(oldCenterTime, newCenterTime)) return;
                MoveDrumToNewTime(oldCenterTime, newCenterTime);
            });

            // 仅针对当前鼓点移动
            PreAdventAbsoluteTime.SetAction(v =>
            {
                if (!float.TryParse(v.ToString(), out float preAdventTime)) return;
                float offset = ls.DrwmsData.VPreAdventAudioClipOffsetTime;
                float newCenterTime = (float)Math.Round(preAdventTime + offset, 2);
                float oldCenterTime = ls.DrwmsData.CenterTime;
                if (Mathf.Approximately(newCenterTime, oldCenterTime)) return;
                MoveDrumToNewTime(oldCenterTime, newCenterTime);
            });

            // 修改类型只影响当前鼓点
            DrwmType.SetAction(v =>
            {
                ls.DrwmsData.DtheTypeOfOperation = (TheTypeOfOperation)v;
                DrwmType.SetDropdownVlaue(ls.DrwmsData.DtheTypeOfOperation);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            // ——以下全部为批量应用到同类——
            PreAdventAudio.SetAction(v =>
            {
                string clipName = ((AudioClip)v).name;
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, drum => drum.DrwmsData.FPreAdventAudioClipPath = clipName);
                PreAdventAudio.SetShowFileName(clipName);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            SucceedAudio.SetAction(v =>
            {
                string clipName = ((AudioClip)v).name;
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, drum => drum.DrwmsData.FSucceedAudioClipPath = clipName);
                SucceedAudio.SetShowFileName(clipName);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            LoseAudioClip.SetAction(v =>
            {
                string clipName = ((AudioClip)v).name;
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, drum => drum.DrwmsData.FLoseAudioClipPath = clipName);
                LoseAudioClip.SetShowFileName(clipName);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            PreAdventAudioVolum.SetAction(v =>
            {
                float vol = (float)v;
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, drum => drum.MusicData.SPreAdventVolume = vol);
                PreAdventAudioVolum.SetValueShow(vol);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            SucceedAudioVolum.SetAction(v =>
            {
                float vol = (float)v;
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, drum => drum.MusicData.SSucceedVolume = vol);
                SucceedAudioVolum.SetValueShow(vol);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            LoseAudioClipVolum.SetAction(v =>
            {
                float vol = (float)v;
                var curType = ls.DrwmsData.DtheTypeOfOperation;
                ApplyToAllDrumsOfType(curType, drum => drum.MusicData.SLoseVolume = vol);
                LoseAudioClipVolum.SetValueShow(vol);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            TimeOfExistence.SetAction(v =>
            {
                if (float.TryParse(v.ToString(), out float result))
                {
                    var curType = ls.DrwmsData.DtheTypeOfOperation;
                    ApplyToAllDrumsOfType(curType, drum => drum.DrwmsData.VTimeOfExistence = result);
                    TimeOfExistence.SetValueShow(result.ToString());
                    this.SendEvent<OnUpdateAudioEditDrumsUI>();
                }
            });

            PreAdventAudioClipOffsetTime.SetAction(v =>
            {
                if (float.TryParse(v.ToString(), out float result))
                {
                    var curType = ls.DrwmsData.DtheTypeOfOperation;
                    ApplyToAllDrumsOfType(curType, drum => drum.DrwmsData.VPreAdventAudioClipOffsetTime = result);
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

    // [切换到上一个鼓点] -- mixyao/07/09
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

    // [切换到下一个鼓点] -- mixyao/07/09
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

    #region 添加鼓点的微动（连个按钮分别控制 前进/后退 0.01s）  -mixyao/25/06/19 
    void OffsetCurrentDrumTime(float offset)
    {
        if (ls == null) return;

        float oldTime = ls.DrwmsData.CenterTime;
        float newTime = (float)Math.Round(oldTime + offset, 2, MidpointRounding.ToEven);
        if (Mathf.Approximately(oldTime, newTime)) return;

        MoveDrumToNewTime(oldTime, newTime);
    }

    void MoveDrumToNewTime(float oldTime, float newTime)
    {
        // 1. 从旧时间点移除
        if (editModel.TimeLineData.ContainsKey(oldTime))
        {
            var list = editModel.TimeLineData[oldTime];
            list.Remove(ls);
            if (list.Count == 0)
                editModel.TimeLineData.Remove(oldTime);
        }

        // 2. 更新数据模型中的 CenterTime
        ls.DrwmsData.CenterTime = newTime;
        // —— 新增：根据新的 time + typeIndex 重新生成 DrumCode —— 
        ls.DrwmsData.DrumCode = GenerateDrumCode(newTime, index);
        currentDrumCode = ls.DrwmsData.DrumCode;

        // 3. 插入到新时间点
        if (!editModel.TimeLineData.ContainsKey(newTime))
            editModel.TimeLineData[newTime] = new List<DrumsLoadData>();
        editModel.TimeLineData[newTime].Add(ls);

        // 4. 同步当前播放/编辑指针
        editModel.ThisTime = newTime;

        // 5. 更新界面上所有的 UIAudioEditDrums 实例
        UIAudioEditDrums[] allDrums = FindObjectsOfType<UIAudioEditDrums>();
        foreach (var drum in allDrums)
        {
            if (Mathf.Approximately(drum.ThisTime, oldTime) && drum.Index == index)
            {
                drum.ThisTime = newTime;
                // —— 新增：同步更新界面组件上的 DrumCode 字段 —— 
                drum.DrumCode = ls.DrwmsData.DrumCode;
                break;
            }
        }

        // 6. 重新生成编号列表并广播更新
        GenerateDrumCodeList();
        this.SendEvent<OnUpdateThisTime>();
        this.SendEvent<OnUpdateAudioEditDrumsUI>();

        // 7. 将时间指针移动到新位置
        timeHand.SetTime(newTime);
        listPanel.RefreshList();

        // 8. 新增：广播唯一 DrumCode 事件，联动所有面板高亮
        GameBody.Interface.SendEvent(new UIAudioEditDrumsOrbit.OnSelectDrumByCode() { DrumCode = currentDrumCode });

    }
    #endregion
    public void Show(bool isbool)
    {
        foreach (var i in gameObjects)
            i.SetActive(isbool);
        RemoveButton.gameObject.SetActive(isbool);
    }

    public void RemoveDrwm()
    {
        listPanel.RemoveCurrentSelectedDrumByDrumCode(currentDrumCode);
    }

    public void UpdateName(string str = "")
    {
        if (!string.IsNullOrEmpty(str))
        {
            Name.text = str;
            return;
        }
        if (ls != null && ls.DrwmsData != null)
        {
            Name.text = $"鼓点编号: {ls.DrwmsData.DrumCode}";
        }
        else
        {
            Name.text = "当前未选中鼓点";
        }
    }

    // [切换按钮状态] -- mixyao/07/08
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


    void Update() { }
}
