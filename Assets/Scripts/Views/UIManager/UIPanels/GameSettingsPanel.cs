using System.Globalization;
using Qf.Models;
using QFramework;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Views.UIManager.UIPanels
{
    public class GameSettingsPanel : BasePanel, ICanGetModel
    {
        public Slider musicVolume;
        public Slider tipSoundVolume;
        public Slider intervalSoundVolume;
        public Slider readerVolume;
        
        public TMP_Text musicVolumeText;
        public TMP_Text tipSoundVolumeText;
        public TMP_Text intervalSoundVolumeText;
        public TMP_Text readerVolumeText;
        
        public Block backBtn;
        protected override void Init()
        {
            var model = this.GetModel<GameSettingModel>();
            musicVolume.onValueChanged.AddListener((v) =>
            {
                v *= 100;
                musicVolumeText.text = Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
                model.musicVolume = v;
            });
            tipSoundVolume.onValueChanged.AddListener((v) =>
            {
                v *= 100;
                tipSoundVolumeText.text = Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
                model.tipSoundVolume = v;
            });
            intervalSoundVolume.onValueChanged.AddListener((v) =>
            {
                v *= 100;
                intervalSoundVolumeText.text = Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
                model.intervalSoundVolume = v;
            });
            readerVolume.onValueChanged.AddListener((v) =>
            {
                v *= 100;
                readerVolumeText.text = Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
                model.readerVolume = v;
            });
            
            backBtn.OnClick+=(() =>
            {
                UIManager.Instance.HidePanel<GameSettingsPanel>();
                UIManager.Instance.ShowPanel<MainPanel>();
            });
        }

        public IArchitecture GetArchitecture()
        {
            return UIArchitecture.Interface;
        }
    }
}