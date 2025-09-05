using System.Globalization;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Views.UIManager.UIPanels
{
    public class GameSettingsPanel : BasePanel
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
            // 加载保存的音量设置
            LoadVolumeSettings();
            
            musicVolume.onValueChanged.AddListener((v) =>
            {
                v *= 100;
                musicVolumeText.text = Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
                PlayerPrefs.SetFloat("MusicVolume", v);
            });
            tipSoundVolume.onValueChanged.AddListener((v) =>
            {
                v *= 100;
                tipSoundVolumeText.text = Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
                PlayerPrefs.SetFloat("TipSoundVolume", v);
            });
            intervalSoundVolume.onValueChanged.AddListener((v) =>
            {
                v *= 100;
                intervalSoundVolumeText.text = Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
                PlayerPrefs.SetFloat("IntervalSoundVolume", v);
            });
            readerVolume.onValueChanged.AddListener((v) =>
            {
                v *= 100;
                readerVolumeText.text = Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
                PlayerPrefs.SetFloat("ReaderVolume", v);
            });
            
            backBtn.OnClick+=(() =>
            {
                UIManager.Instance.HidePanel<GameSettingsPanel>();
                UIManager.Instance.ShowPanel<MainPanel>();
            });
        }

        private void LoadVolumeSettings()
        {
            // 加载音量设置，默认值为40
            float musicVol = PlayerPrefs.GetFloat("MusicVolume", 40f);
            float tipVol = PlayerPrefs.GetFloat("TipSoundVolume", 40f);
            float intervalVol = PlayerPrefs.GetFloat("IntervalSoundVolume", 40f);
            float readerVol = PlayerPrefs.GetFloat("ReaderVolume", 40f);
            
            // 设置滑块值（0-1范围）
            musicVolume.value = musicVol / 100f;
            tipSoundVolume.value = tipVol / 100f;
            intervalSoundVolume.value = intervalVol / 100f;
            readerVolume.value = readerVol / 100f;
            
            // 设置文本显示
            musicVolumeText.text = Mathf.RoundToInt(musicVol).ToString(CultureInfo.InvariantCulture);
            tipSoundVolumeText.text = Mathf.RoundToInt(tipVol).ToString(CultureInfo.InvariantCulture);
            intervalSoundVolumeText.text = Mathf.RoundToInt(intervalVol).ToString(CultureInfo.InvariantCulture);
            readerVolumeText.text = Mathf.RoundToInt(readerVol).ToString(CultureInfo.InvariantCulture);
        }

    }
}