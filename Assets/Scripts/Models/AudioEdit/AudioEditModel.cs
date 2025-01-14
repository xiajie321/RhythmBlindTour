using Qf.ClassDatas;
using Qf.ClassDatas.AudioEdit;
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
        public float ThisTime;//��ǰʱ��
        public Dictionary<float,List<DrumsLoadData>> TimeLineData = new();//ʱ��������(�洢��Ϊʱ�����Ӧ�ĵ�����)
        public int BPM = 60;
        protected override void OnInit()
        {
            
        }
    }

}
