using UnityEngine;
using UnityEngine.UI;

public class UAPSystemTTSToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Reset()
    {
        // 方便拖上 Toggle 后自动赋值
        toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        // 初始化并同步 UI 状态（IsEnabled() 内部会确保 UAP 初始化）
        bool isOn = UAP_AccessibilityManager.IsEnabled();
        if (toggle != null)
        {
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        // 我们只要“朗读”，不要任何无障碍手势
        UAP_AccessibilityManager.EnableMagicGestures(false);

        // 如果你的 UAP 预制里 m_HandleUI 已经是 false，这里不需要再做别的。
        // 如果你改不了预制、又担心 UI 被接管，可在需要时临时阻断输入但保留语音：
        // UAP_AccessibilityManager.BlockInput(true, stopSpeakingOnBlock: false);
    }

    private void OnDisable()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    // 直接把 Toggle 的 OnValueChanged(bool) 绑到这个方法即可
    public void OnToggleChanged(bool isOn)
    {
        // 关掉时先停当前说话，避免残留
        if (!isOn && UAP_AccessibilityManager.IsSpeaking())
            UAP_AccessibilityManager.StopSpeaking();

        // 开/关系统 TTS（关闭后 Say 会被忽略）
        UAP_AccessibilityManager.EnableAccessibility(isOn, readNotification: false);
    }
}
