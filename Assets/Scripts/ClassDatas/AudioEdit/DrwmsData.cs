using System.Drawing;

namespace Qf.ClassDatas.AudioEdit
{
    public class DrwmsData
    {
        public TheTypeOfOperation DtheTypeOfOperation;
        public string FPreAdventAudioClipPath;
        public string FSucceedAudioClipPath;
        public string FLoseAudioClipPath;
        public float VPreAdventAudioClipOffsetTime;
        public float VTimeOfExistence;
        public float CenterTime;
        public string DrumCode;

        // 新增：预制类型和中心判定标记
        public int PrefabType = 4;  // 默认 DrumCenter
        public bool IsCenterDrum = true;

        public string FDefaultAudioClipPath; // 原来是 { get; internal set; }
        public float VTipPlayOffset;       // 原来是 { get; internal set; }
    }
}
