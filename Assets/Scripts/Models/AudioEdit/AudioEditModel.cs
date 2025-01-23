using Qf.ClassDatas;
using Qf.ClassDatas.AudioEdit;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Querys.AudioEdit;
using QFramework;
using System.Collections;
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
        public BindableProperty<float> TipOffset = new();//������ʾ��ƫ����(����Ϊ��N��ǰ�����������෴)
        public float ThisTime;//��ǰʱ��
        public Dictionary<float, List<DrumsLoadData>> TimeLineData = new();//ʱ��������(�洢��Ϊʱ�����Ӧ�ĵ�����)
        public int BPM = 60;
        public int BeatA = 1;//B/A����
        public int BeatB = 1;
        protected override void OnInit()
        {

        }
        public void Load()
        {
            AudioSaveData audioSaveData = this.GetUtility<Storage>().Load<AudioSaveData>(".Save");
            DataCachingModel s = this.GetModel<DataCachingModel>();
            this.SendCommand(new SetAudioEditAudioCommand(this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.EditAudioClip))));
            this.SendCommand(new SetAudioEditSucceedAudioCommand(this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.SucceedAudioClip))));
            this.SendCommand(new SetAudioEditAudioLoseAudioCommand(this.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.LoseAudioClip))));
            TipOffset.Value = audioSaveData.TipOffset;
            this.SendCommand(new SetAudioEditThisTimeCommand(audioSaveData.ThisTime));
            TimeLineData = audioSaveData.TimeLineData;
            this.SendEvent<OnUpdateAudioEditDrumsUI>();

        }
        public void Save()
        {
            AudioSaveData audioSaveData = new AudioSaveData();
            audioSaveData.EditAudioClip = EditAudioClip.name;
            audioSaveData.SucceedAudioClip = SucceedAudioClip.name;
            audioSaveData.LoseAudioClip = LoseAudioClip.name;
            audioSaveData.TipOffset = TipOffset.Value;
            audioSaveData.ThisTime = ThisTime;
            audioSaveData.TimeLineData = TimeLineData;
            this.GetUtility<Storage>().Save(audioSaveData,".Save");
        }
    }
    public class AudioSaveData
    {
        public string EditAudioClip;
        public string SucceedAudioClip;
        public string LoseAudioClip;
        public float TipOffset;//������ʾ��ƫ����(����Ϊ��N��ǰ�����������෴)
        public float ThisTime;//��ǰʱ��
        public Dictionary<float, List<DrumsLoadData>> TimeLineData = new();//ʱ��������(�洢��Ϊʱ�����Ӧ�ĵ�����)
    }
}
