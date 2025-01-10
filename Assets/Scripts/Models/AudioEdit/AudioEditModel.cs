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
    /// 音频编辑数据
    /// </summary>
    public class AudioEditModel : AbstractModel
    {
        public SystemModeData Mode { get; set; } = SystemModeData.EditMode;//模式
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
        //编辑的音频
        private float thisTime;
        public float ThisTime  //当前时间
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
        public Dictionary<float, List<DrumsLoadData>> TimeLineData = new();//时间线数据(存储的为时间轴对应鼓点数据)
        protected override void OnInit()
        {

        }
    }

}
