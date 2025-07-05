
using Assets.Scripts.Querys.AudioEdit;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;

using UnityEngine;

public class UIMainAttributeSetPanel : MonoBehaviour, IController
{
    [SerializeField]
    UIFileAttribute MainAudio;//主音频
    [SerializeField]
    UIFileAttribute LoseAudio;//失败音频
    [SerializeField]
    UIValueAttribute TipOffset;//偏移量
    [SerializeField]
    UIValueAttribute TimeOfExistence;//鼓点存在时间
    [SerializeField]
    UISliderAttribute MainAudioVolume;//主音频音量大小
    [SerializeField]
    UISliderAttribute SucceedVolume;//成功音量大小
    [SerializeField]
    UISliderAttribute LoseVolume;//失败音量大小
    [SerializeField]
    UISliderAttribute PreAdventVolume;//提示音量大小
    [SerializeField]
    UIFileAttribute[] SucceedAudios;//成功音频
    [SerializeField]
    UIFileAttribute[] InteractionEvents;
    AudioEditModel editModel;
    private void OnEnable()
    {
        if (editModel == null)
            editModel = this.GetModel<AudioEditModel>();
        UpdateAll();
    }
    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
    /// <summary>
    /// 基础事件
    /// </summary>
    void Start()
    {
        MainAudioVolume.SetAction(v =>
        {
            float ls = (float)v;
            if (ls >= 1)
                editModel.EditAudioClipVolume.Value = 1;
            else if (ls <= 0)
                editModel.EditAudioClipVolume.Value = 0;
            else
                editModel.EditAudioClipVolume.Value = ls;
        });
        SucceedVolume.SetAction(v =>
        {
            float ls = (float)v;
            if (ls >= 1)
                editModel.SucceedAudioVolume.Value = 1;
            else if (ls <= 0)
                editModel.SucceedAudioVolume.Value = 0;
            else
                editModel.SucceedAudioVolume.Value = ls;
        });
        LoseVolume.SetAction(v =>
        {
            float ls = (float)v;
            if (ls >= 1)
                editModel.LoseAudioVolume.Value = 1;
            else if (ls <= 0)
                editModel.LoseAudioVolume.Value = 0;
            else
                editModel.LoseAudioVolume.Value = ls;
        });
        PreAdventVolume.SetAction(v =>
        {
            float ls = (float)v;
            if (ls >= 1)
                editModel.PreAdventVolume.Value = 1;
            else if (ls <= 0)
                editModel.PreAdventVolume.Value = 0;
            else
                editModel.PreAdventVolume.Value = ls;
        });
        MainAudio.SetAction(v =>
        {
            this.SendCommand(new SetAudioEditAudioCommand((AudioClip)v));
            MainAudio.SetShowFileName(((AudioClip)v).name);
        });
        LoseAudio.SetAction(v =>
        {
            this.SendCommand(new SetAudioEditAudioLoseAudioCommand((AudioClip)v));
            LoseAudio.SetShowFileName(((AudioClip)v).name);
        });
        TipOffset.SetAction(v =>
        {
            if (!v.Equals(""))
                editModel.TipOffset.Value = float.Parse((string)v);
            else
                editModel.TipOffset.Value = 0;
        });
        TimeOfExistence.SetAction(v =>
        {
            if (!v.Equals(""))
                editModel.TimeOfExistence.Value = float.Parse((string)v);
            else
                editModel.TipOffset.Value = 0;
        });
        editModel.TipOffset.Register(v =>
        {
            TipOffset.SetValueShow(v.ToString());
        }).UnRegisterWhenDisabled(gameObject);
        editModel.TimeOfExistence.Register(v =>
        {
            TimeOfExistence.SetValueShow(v.ToString());
        }).UnRegisterWhenDisabled(gameObject);
        editModel.EditAudioClipVolume.Register(v =>
        {
            MainAudioVolume.SetValueShow(v);
        }).UnRegisterWhenDisabled(gameObject);
        editModel.SucceedAudioVolume.Register(v =>
        {
            SucceedVolume.SetValueShow(v);
        }).UnRegisterWhenDisabled(gameObject);
        editModel.LoseAudioVolume.Register(v =>
        {
            LoseVolume.SetValueShow(v);
        }).UnRegisterWhenDisabled(gameObject);
        editModel.PreAdventVolume.Register(v =>
        {
            PreAdventVolume.SetValueShow(v);
        }).UnRegisterWhenDisabled(gameObject);
        int index = 0;
        foreach (var i in InteractionEvents)//上下左右单击
        {
            int ls = index;
            i.SetAction(v =>
            {

                this.SendCommand(new SetAudioEditAudioComeTipsCommand((TheTypeOfOperation)ls, (AudioClip)v));
                i.SetShowFileName(((AudioClip)v).name);
            });
            index++;
        }
        index = 0;
        foreach (var i in SucceedAudios)
        {
            int ls = index;
            i.SetAction(v =>
            {
                this.SendCommand(new SetAudioEditSucceedAudioCommand((TheTypeOfOperation)ls, (AudioClip)v));
                i.SetShowFileName(((AudioClip)v).name);
            });
            index++;
        }
        this.RegisterEvent<AudioEditModelLoad>(v =>
        {
            UpdateAll();
        }).UnRegisterWhenDisabled(gameObject);

    }
    /// <summary>
    /// 刷新显示名称
    /// </summary>
    void UpdateAll()
    {
        MainAudioVolume.SetValueShow(editModel.EditAudioClipVolume.Value);
        LoseVolume.SetValueShow(editModel.LoseAudioVolume.Value);
        SucceedVolume.SetValueShow(editModel.SucceedAudioVolume.Value);
        PreAdventVolume.SetValueShow(editModel.PreAdventVolume.Value);
        MainAudio.SetShowFileName(editModel?.EditAudioClip?.name);
        LoseAudio.SetShowFileName(editModel?.LoseAudioClip?.name);
        int index = 0;
        foreach (var i in InteractionEvents)
        {
            i.SetShowFileName(this.SendQuery(new QueryAudioEditComeTipAudio((TheTypeOfOperation)index))?.name);
            index++;
        }
        index = 0;
        foreach (var i in SucceedAudios)
        {
            i.SetShowFileName(this.SendQuery(new QueryAudioEditSucceedsAudio((TheTypeOfOperation)index))?.name);
            index++;
        }
        TipOffset.SetValueShow(editModel.TipOffset.ToString());
        TimeOfExistence.SetValueShow(editModel.TimeOfExistence.ToString());
    }
    /// <summary>
    /// 刷新对应鼓点数据
    /// </summary>
    public void UpdateAllData()
    {
        foreach (var i in editModel.TimeLineData.Keys)
        {
            foreach (var j in editModel.TimeLineData[i])
            {
                j.DrwmsData.FSucceedAudioClipPath = this.SendQuery(new QueryAudioEditSucceedsAudio(j.DrwmsData.DtheTypeOfOperation)).name;
                j.DrwmsData.FLoseAudioClipPath = editModel.LoseAudioClip.name;
                j.DrwmsData.FPreAdventAudioClipPath = this.SendQuery(new QueryAudioEditComeTipAudio(j.DrwmsData.DtheTypeOfOperation)).name;
                j.DrwmsData.VPreAdventAudioClipOffsetTime = editModel.TipOffset.Value;
                j.DrwmsData.VTimeOfExistence = editModel.TimeOfExistence.Value;
                j.MusicData.SPreAdventVolume = editModel.PreAdventVolume.Value;
                j.MusicData.SLoseVolume = editModel.LoseAudioVolume.Value;
                j.MusicData.SSucceedVolume = editModel.SucceedAudioVolume.Value; // 修复 2025/06/06 - mixyao
            }
        }
        this.SendEvent<OnUpdateAudioEditDrumsUI>();
    }
}
