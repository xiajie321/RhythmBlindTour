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
    /// ��Ƶ�༭����
    /// </summary>
    public class AudioEditModel : AbstractModel
    {
        public SystemModeData Mode = SystemModeData.EditMode;//ģʽ
        public AudioClip EditAudioClip;//�༭����Ƶ
        public AudioClip SucceedAudioClip;//�����ɹ�����Ƶ
        public AudioClip LoseAudioClip;//����ʧ�ܵ���Ƶ
        public AudioClip DownTipsAudioClip;
        public AudioClip UpTipsAudioClip;
        public AudioClip LeftTipsAudioClip;
        public AudioClip RightTipsAudioClip;
        public AudioClip ClickTipsAudioCLip;
        public BindableProperty<float> TipOffset = new(0.2f);//������ʾ��ƫ����(����Ϊ��N��ǰ�����������෴)
        public float ThisTime;//��ǰʱ��
        public BindableProperty<float> TimeOfExistence = new(0.5f);//����ʱ��
        public Dictionary<float, List<AudioClip>> TipsAudio = new();//��ʾ��Ƶλ��
        public Dictionary<float, List<float>> TipsVolume = new();//��ʾ��Ƶ����
        public Dictionary<float, List<DrumsLoadData>> TimeLineData = new();//ʱ��������(�洢��Ϊʱ�����Ӧ�ĵ�����)
        public int BPM = 60;
        public int BeatA = 4;//B/A����
        public int BeatB = 4;
        protected override void OnInit()
        {
            this.RegisterEvent<OnUpdateAudioEditDrumsUI>(v =>
            {
                UpdateTipsAudio();
            });
        }
        float ls;
        //���ڸ��¹ĵ���Ե���ʾ��λ��
        public void UpdateTipsAudio()
        {
            TipsAudio.Clear();
            TipsVolume.Clear();
            foreach (var i in TimeLineData.Keys)
            {
                foreach (var j in TimeLineData[i])
                {
                    if (i - j.DrwmsData.PreAdventAudioClipOffsetTime >= 0)
                    {
                        ls = (float)Math.Round(i - j.DrwmsData.PreAdventAudioClipOffsetTime, 2, MidpointRounding.ToEven);
                        if (!TipsAudio.ContainsKey(ls))
                        {
                            TipsAudio.Add(ls, new());
                            TipsVolume.Add(ls, new());
                        }
                        if (TipsAudio.ContainsKey(ls))
                        {
                            TipsAudio[ls].Add(this.GetModel<DataCachingModel>().GetAudioClip(j.DrwmsData.PreAdventAudioClipPath));
                            TipsVolume[ls].Add(j.MusicData.PreAdventVolume);
                        }

                    }
                    else
                    {
                        ls = (float)Math.Round(j.DrwmsData.PreAdventAudioClipOffsetTime, 2, MidpointRounding.ToEven);
                        if (!TipsAudio.ContainsKey(ls)) 
                        { 
                            TipsAudio.Add(ls, new());
                            TipsVolume.Add(ls, new());
                        }

                        if (TipsAudio.ContainsKey(ls))
                        {
                            TipsAudio[ls].Add(this.GetModel<DataCachingModel>().GetAudioClip(j.DrwmsData.PreAdventAudioClipPath));
                            TipsVolume[ls].Add(j.MusicData.PreAdventVolume);
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
            this.SendCommand(new SetAudioEditAudioCommand(this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.EditAudioClip))));
            this.SendCommand(new SetAudioEditSucceedAudioCommand(this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.SucceedAudioClip))));
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

        }
        public void Save()
        {
            string Path = FileLoader.SaveLevelFile();
            if (Path.Equals("")) return;
            AudioSaveData audioSaveData = new AudioSaveData();
            audioSaveData.EditAudioClip = EditAudioClip.name;
            audioSaveData.SucceedAudioClip = SucceedAudioClip.name;
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
        public string SucceedAudioClip;
        public string LoseAudioClip;
        public string DownTipsAudioClip;
        public string UpTipsAudioClip;
        public string LeftTipsAudioClip;
        public string RightTipsAudioClip;
        public string ClickTipsAudioCLip;
        public float TipOffset;//������ʾ��ƫ����(����Ϊ��N��ǰ�����������෴)
        public float ThisTime;//��ǰʱ��
        public float TimeOfExistence;//����ʱ��
        public Dictionary<float, List<DrumsLoadData>> TimeLineData = new();//ʱ��������(�洢��Ϊʱ�����Ӧ�ĵ�����)
    }
}
