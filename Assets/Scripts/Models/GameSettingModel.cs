using QFramework;

namespace Qf.Models
{
    public class GameSettingModel : AbstractModel
    {
        public float musicVolume;
        public float tipSoundVolume;
        public float intervalSoundVolume;
        public float readerVolume;
        protected override void OnInit()
        {
            musicVolume = 40f;
            tipSoundVolume = 40f;
            intervalSoundVolume = 40f;
            readerVolume = 40f;
        }
    }
}