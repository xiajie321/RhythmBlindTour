using Qf.ClassDatas;
using Qf.ClassDatas.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Qf.Events;

namespace Qf.Models.AudioEdit
{
    /// <summary>
    /// ��Ƶ�༭����
    /// </summary>
    public class AudioEditModel : AbstractModel
    {
        public SystemModeData Mode { get; set; } = SystemModeData.EditMode;//ģʽ
        private AudioClip editAudioClip;
        public AudioClip EditAudioClip
        {
            get => editAudioClip;
            set
            {
                if (value != editAudioClip)
                {
                    editAudioClip = value;
                    this.SendEvent(new AudioEditChangeEvent() { clip = editAudioClip });
                }
            }
        }
        //�༭����Ƶ
        private float thisTime;
        public float ThisTime  //��ǰʱ��
        {
            get => thisTime;
            set
            {
                if (value != thisTime)
                {
                    thisTime = value;
                    this.SendEvent<AudioEditTimeChangeEvent>();
                }
            }
        }
        public Dictionary<float, List<DrumsLoadData>> TimeLineData = new();//ʱ��������(�洢��Ϊʱ�����Ӧ�ĵ�����)
        protected override void OnInit()
        {

        }
    }

}
