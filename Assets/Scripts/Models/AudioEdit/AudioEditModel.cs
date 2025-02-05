using Qf.ClassDatas.AudioEdit;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Querys.AudioEdit;
using QFramework;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Qf.Models.AudioEdit
{
    /// <summary>
    /// 音频编辑数据
    /// </summary>
    public class AudioEditModel : AbstractModel
    {
        public SystemModeData Mode = SystemModeData.EditMode;//模式
        public AudioClip EditAudioClip;//编辑的音频
        public BindableProperty<float> EditAudioClipVolume =new(1);
        public BindableProperty<float> PreAdventVolume = new(1);
        public BindableProperty<float> SucceedAudioVolume = new(1);
        public BindableProperty<float> LoseAudioVolume = new(1);
        public AudioClip DownSucceedAudioClip;//交互成功的音频
        public AudioClip UpSucceedAudioClip;
        public AudioClip LeftSucceedAudioClip;
        public AudioClip RigthSucceedAudioClip;
        public AudioClip ClickSucceedAudioClip;
        public AudioClip LoseAudioClip;//交互失败的音频
        public AudioClip DownTipsAudioClip;
        public AudioClip UpTipsAudioClip;
        public AudioClip LeftTipsAudioClip;
        public AudioClip RightTipsAudioClip;
        public AudioClip ClickTipsAudioCLip;
        public BindableProperty<float> TipOffset = new(0.2f);//交互提示的偏移量(正数为在N秒前触发负数则相反)
        public float ThisTime;//当前时间
        public BindableProperty<float> TimeOfExistence = new(0.5f);//存在时间
        public Dictionary<float, List<AudioClip>> TipsAudio = new();//提示音频位置
        public Dictionary<float, List<float>> TipsVolume = new();//提示音频音量
        public Dictionary<float, List<DrumsLoadData>> TimeLineData = new();//时间线数据(存储的为时间轴对应鼓点数据)
        public int BPM = 60;
        public int BeatA = 4;//B/A节拍
        public int BeatB = 4;
        protected override void OnInit()
        {
            this.RegisterEvent<OnUpdateAudioEditDrumsUI>(v =>
            {
                UpdateTipsAudio();
            });
        }
        float ls;
        //用于更新鼓点相对的提示音位置
        public void UpdateTipsAudio()
        {
            TipsAudio.Clear();
            TipsVolume.Clear();
            foreach (var i in TimeLineData.Keys)
            {
                foreach (var j in TimeLineData[i])
                {
                    if (j.DrwmsData == null) continue;
                    if (i - j.DrwmsData.VPreAdventAudioClipOffsetTime >= 0)
                    {
                        ls = (float)Math.Round(i - j.DrwmsData.VPreAdventAudioClipOffsetTime, 2, MidpointRounding.ToEven);
                        if (!TipsAudio.ContainsKey(ls))
                        {
                            TipsAudio.Add(ls, new());
                            TipsVolume.Add(ls, new());
                        }
                        if (TipsAudio.ContainsKey(ls))
                        {
                            TipsAudio[ls].Add(this.GetModel<DataCachingModel>().GetAudioClip(j.DrwmsData.FPreAdventAudioClipPath));
                            TipsVolume[ls].Add(j.MusicData.SPreAdventVolume);
                        }

                    }
                    else
                    {
                        ls = (float)Math.Round(j.DrwmsData.VPreAdventAudioClipOffsetTime, 2, MidpointRounding.ToEven);
                        if (!TipsAudio.ContainsKey(ls)) 
                        { 
                            TipsAudio.Add(ls, new());
                            TipsVolume.Add(ls, new());
                        }

                        if (TipsAudio.ContainsKey(ls))
                        {
                            TipsAudio[ls].Add(this.GetModel<DataCachingModel>().GetAudioClip(j.DrwmsData.FPreAdventAudioClipPath));
                            TipsVolume[ls].Add(j.MusicData.SPreAdventVolume);
                        }

                    }
                }
            }
        }
        public void Load()
        {
            string Path = FileLoader.OpenLeveFile();
            if (Path.Equals("")) return;
            AudioSaveData audioSaveData = this.GetUtility<Storage>().Load<AudioSaveData>(Path, true);
            DataCachingModel s = this.GetModel<DataCachingModel>();
            EditAudioClipVolume.Value = audioSaveData.EditAudioClipVolume;
            this.SendCommand(new SetAudioEditAudioCommand(this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.EditAudioClip))));
            this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.SwipeDown,this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.DownSucceedAudioClip))));
            this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.SwipeUp, this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.UpSucceedAudioClip))));
            this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.SwipeLeft, this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.LeftSucceedAudioClip))));
            this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.SwipeRight, this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.RigthSucceedAudioClip))));
            this.SendCommand(new SetAudioEditSucceedAudioCommand(TheTypeOfOperation.Click, this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.ClickSucceedAudioClip))));
            this.SendCommand(new SetAudioEditAudioLoseAudioCommand(this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.LoseAudioClip))));
            UpTipsAudioClip = this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.UpTipsAudioClip));
            DownTipsAudioClip = this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.DownTipsAudioClip));
            LeftTipsAudioClip = this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.LeftTipsAudioClip));
            RightTipsAudioClip = this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.RightTipsAudioClip));
            ClickTipsAudioCLip = this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.ClickTipsAudioCLip));
            TipOffset.Value = audioSaveData.TipOffset;
            this.SendCommand(new SetAudioEditThisTimeCommand(audioSaveData.ThisTime));
            TimeLineData = audioSaveData.TimeLineData;
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
            this.SendEvent<AudioEditModelLoad>();
        }
        public void Save()
        {
            string Path = FileLoader.SaveLevelFile();
            if (Path.Equals("")) return;
            AudioSaveData audioSaveData = new AudioSaveData();
            audioSaveData.EditAudioClip = EditAudioClip.name;
            audioSaveData.EditAudioClipVolume = EditAudioClipVolume.Value;
            audioSaveData.DownSucceedAudioClip = DownSucceedAudioClip.name;
            audioSaveData.UpSucceedAudioClip = UpSucceedAudioClip.name;
            audioSaveData.LeftSucceedAudioClip = LeftSucceedAudioClip.name;
            audioSaveData.RigthSucceedAudioClip = RightTipsAudioClip.name;
            audioSaveData.ClickSucceedAudioClip = ClickSucceedAudioClip.name;
            audioSaveData.LoseAudioClip = LoseAudioClip.name;
            audioSaveData.DownTipsAudioClip = DownTipsAudioClip.name;
            audioSaveData.UpTipsAudioClip = UpTipsAudioClip.name;
            audioSaveData.LeftTipsAudioClip = LeftTipsAudioClip.name;
            audioSaveData.RightTipsAudioClip = RightTipsAudioClip.name;
            audioSaveData.ClickTipsAudioCLip = ClickTipsAudioCLip.name;
            audioSaveData.TipOffset = TipOffset.Value;
            audioSaveData.ThisTime = ThisTime;
            audioSaveData.TimeLineData = TimeLineData;
            audioSaveData.TimeOfExistence = TimeOfExistence.Value;
            this.GetUtility<Storage>().Save(audioSaveData, Path, true);
        }
    }
    public class AudioSaveData
    {
        public string EditAudioClip;
        public float EditAudioClipVolume;
        public string DownSucceedAudioClip;//交互成功的音频
        public string UpSucceedAudioClip;
        public string LeftSucceedAudioClip;
        public string RigthSucceedAudioClip;
        public string ClickSucceedAudioClip;
        public string LoseAudioClip;
        public string DownTipsAudioClip;
        public string UpTipsAudioClip;
        public string LeftTipsAudioClip;
        public string RightTipsAudioClip;
        public string ClickTipsAudioCLip;
        public float TipOffset;//交互提示的偏移量(正数为在N秒前触发负数则相反)
        public float ThisTime;//当前时间
        public float TimeOfExistence;//存在时间
        public Dictionary<float, List<DrumsLoadData>> TimeLineData = new();//时间线数据(存储的为时间轴对应鼓点数据)
    }
}
