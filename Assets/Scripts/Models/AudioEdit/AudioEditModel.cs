using Qf.ClassDatas;
using Qf.ClassDatas.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Qf.Models.AudioEdit
{
    public class AudioEditModel : AbstractModel
    {
        public SystemModeData Mode = SystemModeData.EditMode;//ģʽ
        public AudioClip EditAudioClip;//�༭����Ƶ
        public float ThisTime;//��ǰʱ��
        public Dictionary<float, DrumsLoadData> TimeLineData = new();//ʱ��������
        protected override void OnInit()
        {

        }

    }

}
