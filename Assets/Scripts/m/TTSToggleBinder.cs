using UnityEngine;
using UnityEngine.UI;

public class TTSToggleBinder : MonoBehaviour
{
    public Toggle ttsToggle;

    void Start()
    {
        // 初始同步：读当前是否启用
        ttsToggle.isOn = UAP_AccessibilityManager.IsEnabled(); // 公开API
        // 绑定事件
        ttsToggle.onValueChanged.AddListener(OnTTSOptionChanged);
    }

    void OnDestroy()
    {
        ttsToggle.onValueChanged.RemoveListener(OnTTSOptionChanged);
    }

    private void OnTTSOptionChanged(bool on)
    {
        // 第二个参数 readNotification=true，会播“已启用/已关闭”提示音
        UAP_AccessibilityManager.EnableAccessibility(on, true);
        // 若想在“编辑器+Override 开着”时也强制保存当前状态，可手动写一份：
        PlayerPrefs.SetInt("UAP_Enabled_State", on ? 1 : 0);
        PlayerPrefs.Save();
    }
}
