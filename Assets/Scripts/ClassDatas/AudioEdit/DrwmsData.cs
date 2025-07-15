using System.Drawing;

namespace Qf.ClassDatas.AudioEdit
{
    public class DrwmsData
    {
        public Color Color;//颜色
        public TheTypeOfOperation DtheTypeOfOperation;//鼓点类型
        public float VTimeOfExistence = 0.5f;//存在时间
        public float VPreAdventAudioClipOffsetTime = 0.5f;//偏移
        public float CenterTime = 0f;//真实位置 
        public string FPreAdventAudioClipPath;//来临前的音频数据路径
        public string FSucceedAudioClipPath;//成功时的音频数据路径
        public string FLoseAudioClipPath;//失败时的音频路径

        // [鼓点唯一编号] -- mixyao/07/08
        public string DrumCode;
    }

}
