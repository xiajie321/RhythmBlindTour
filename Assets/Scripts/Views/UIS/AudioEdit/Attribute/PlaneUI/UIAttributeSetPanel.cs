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

    UIAudioEditTimeHand timeHand;
    int index;
    float thisTime;
    DrumsLoadData ls;
    List<GameObject> gameObjects = new();

    public IArchitecture GetArchitecture() => GameBody.Interface;

    private void OnEnable()
    {
        if (editModel == null) editModel = this.GetModel<AudioEditModel>();
        if (cModel == null) cModel = this.GetModel<DataCachingModel>();

        this.RegisterEvent<OnUpdateThisTime>(v =>
        {
            if (editModel.Mode.Equals(SystemModeData.PlayMode)) return;
            if (AudioEditManager.Instance != null && AudioEditManager.Instance.IsControlRunning) return;

            if (editModel.Mode.Equals(SystemModeData.PlayMode)) return;
            index = 0;
            if (editModel.TimeLineData.ContainsKey(v.ThisTime))
            {
                UpButton.gameObject.SetActive(false);
                DownButton.gameObject.SetActive(true);
            }
            else
            {
                UpButton.gameObject.SetActive(false);
                DownButton.gameObject.SetActive(false);
                thisTime = (float)Math.Round(v.ThisTime, 2);
                UpdateName();
                Show(false);
                return;
            }
            UpdateData(index);
        }).UnRegisterWhenDisabled(gameObject);

        this.RegisterEvent<OnUpdateAudioEditDrumsUI>(v =>
        {
            if (editModel.Mode.Equals(SystemModeData.PlayMode)) return;


            if (AudioEditManager.Instance != null && AudioEditManager.Instance.IsControlRunning) return;


            if (editModel.Mode.Equals(SystemModeData.PlayMode)) return;

            if (editModel.TimeLineData.ContainsKey(editModel.ThisTime))
            {
                if (editModel.TimeLineData[editModel.ThisTime].Count <= index)
                    index = editModel.TimeLineData[editModel.ThisTime].Count - 1;

                if (editModel.TimeLineData[editModel.ThisTime].Count != 0)
                {
                    UpButton.gameObject.SetActive(index != 0);
                    DownButton.gameObject.SetActive(index != editModel.TimeLineData[editModel.ThisTime].Count - 1);
                    UpdateData(index);
                }
                else
                {
                    UpdateName("当前时间节点无鼓点");
                    UpButton.gameObject.SetActive(false);
                    DownButton.gameObject.SetActive(false);
                    Show(false);
                }
            }
            else
            {
                UpButton.gameObject.SetActive(false);
                DownButton.gameObject.SetActive(false);
                thisTime = (float)Math.Round(editModel.ThisTime, 2);
                UpdateName();
                Show(false);
            }
        }).UnRegisterWhenDisabled(gameObject);

        index = 0;
        if (editModel.TimeLineData.ContainsKey(editModel.ThisTime))
        {
            UpButton.gameObject.SetActive(false);
            DownButton.gameObject.SetActive(true);
            Show(true);
        }
        else
        {
            UpButton.gameObject.SetActive(false);
            DownButton.gameObject.SetActive(false);
            Show(false);
            return;
        }

        UpdateData(index);
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
    }

    void UpdateData(int index = 0)
    {
        thisTime = (float)Math.Round(editModel.ThisTime, 2);
        UpdateName();

        if (!editModel.TimeLineData.ContainsKey(thisTime))
        {
            Show(false);
            return;
        }

        ls = editModel.TimeLineData[thisTime][index];
        UpdateDataShow(ls);

        // IsDemoDrumButton.SetAction(v =>
        // {

        // });
        CenterTimeAttribute.SetAction(v =>
        {
            if (!float.TryParse(v.ToString(), out float newCenterTime)) return;
            float oldCenterTime = ls.DrwmsData.CenterTime;
            if (Mathf.Approximately(oldCenterTime, newCenterTime)) return;

            MoveDrumToNewTime(oldCenterTime, newCenterTime);
        });

        PreAdventAbsoluteTime.SetAction(v =>
        {
            if (!float.TryParse(v.ToString(), out float preAdventTime)) return;

            float offset = ls.DrwmsData.VPreAdventAudioClipOffsetTime;
            float newCenterTime = (float)Math.Round(preAdventTime + offset, 2);
            float oldCenterTime = ls.DrwmsData.CenterTime;
            if (Mathf.Approximately(newCenterTime, oldCenterTime)) return;

            MoveDrumToNewTime(oldCenterTime, newCenterTime);
        });

        DrwmType.SetAction(v =>
        {
            ls.DrwmsData.DtheTypeOfOperation = (TheTypeOfOperation)v;
            DrwmType.SetDropdownVlaue(ls.DrwmsData.DtheTypeOfOperation);
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
        });

        PreAdventAudio.SetAction(v =>
        {
            ls.DrwmsData.FPreAdventAudioClipPath = ((AudioClip)v).name;
            PreAdventAudio.SetShowFileName(ls.DrwmsData.FPreAdventAudioClipPath);
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
        });

        SucceedAudio.SetAction(v =>
        {
            ls.DrwmsData.FSucceedAudioClipPath = ((AudioClip)v).name;
            SucceedAudio.SetShowFileName(ls.DrwmsData.FSucceedAudioClipPath);
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
        });

        LoseAudioClip.SetAction(v =>
        {
            ls.DrwmsData.FLoseAudioClipPath = ((AudioClip)v).name;
            LoseAudioClip.SetShowFileName(ls.DrwmsData.FLoseAudioClipPath);
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
        });

        PreAdventAudioVolum.SetAction(v =>
        {
            ls.MusicData.SPreAdventVolume = (float)v;
            PreAdventAudioVolum.SetValueShow(ls.MusicData.SPreAdventVolume);
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
        });

        SucceedAudioVolum.SetAction(v =>
        {
            ls.MusicData.SSucceedVolume = (float)v;
            SucceedAudioVolum.SetValueShow(ls.MusicData.SSucceedVolume);
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
        });

        LoseAudioClipVolum.SetAction(v =>
        {
            ls.MusicData.SLoseVolume = (float)v;
            LoseAudioClipVolum.SetValueShow(ls.MusicData.SLoseVolume);
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
        });

        TimeOfExistence.SetAction(v =>
        {
            if (float.TryParse(v.ToString(), out float result))
            {
                ls.DrwmsData.VTimeOfExistence = result;
                TimeOfExistence.SetValueShow(result.ToString());
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            }
        });

        PreAdventAudioClipOffsetTime.SetAction(v =>
        {
            if (float.TryParse(v.ToString(), out float result))
            {
                ls.DrwmsData.VPreAdventAudioClipOffsetTime = result;
                PreAdventAudioClipOffsetTime.SetValueShow(result.ToString());
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            }
        });

        Show(true);
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
        if (editModel.TimeLineData.ContainsKey(oldTime))
        {
            var list = editModel.TimeLineData[oldTime];
            list.Remove(ls);
            if (list.Count == 0)
                editModel.TimeLineData.Remove(oldTime);
        }

        ls.DrwmsData.CenterTime = newTime;
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
                break;
            }
        }

        this.SendEvent<OnUpdateThisTime>();
        this.SendEvent<OnUpdateAudioEditDrumsUI>();

        timeHand.SetTime(newTime);

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
        thisTime = (float)Math.Round(editModel.ThisTime, 2);
        this.SendCommand(new RemoveAudioEditTimeLineDataCommand(thisTime, index));
    }

    public void UpdateName(string str = "")
    {
        if (!string.IsNullOrEmpty(str))
        {
            Name.text = str;
            return;
        }
        if (editModel.TimeLineData.ContainsKey(thisTime))
        {
            if (editModel.TimeLineData[thisTime].Count <= index) return;
            Name.text = editModel.TimeLineData[thisTime][index].Name;
        }
        else
        {
            Name.text = "当前时间节点无鼓点";
        }
    }

    public void UpQh()
    {
        index--;
        if (index <= 0)
        {
            index = 0;
            UpButton.gameObject.SetActive(false);
        }
        else
        {
            UpButton.gameObject.SetActive(true);
        }
        DownButton.gameObject.SetActive(true);
        UpdateData(index);
    }

    public void DownQh()
    {
        index++;
        if (editModel.TimeLineData.ContainsKey(thisTime))
        {
            if (index >= editModel.TimeLineData[thisTime].Count - 1)
            {
                index = editModel.TimeLineData[thisTime].Count - 1;
                DownButton.gameObject.SetActive(false);
            }
            else
            {
                DownButton.gameObject.SetActive(true);
            }
            UpButton.gameObject.SetActive(true);
        }
        UpdateData(index);
    }

    void Update() { }
}
