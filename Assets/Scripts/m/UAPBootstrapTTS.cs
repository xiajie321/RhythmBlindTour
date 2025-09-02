// UAPBootstrapTTS.cs
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class UAPBootstrapTTS : MonoBehaviour
{
    void Awake()
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
        // 如果你想“首启默认就能说话”，可以只在“无历史存档”时开启
        if (!PlayerPrefs.HasKey("UAP_Enabled_State"))
            UAP_AccessibilityManager.EnableAccessibility(true, false);
        // 若你希望每次启动都强制开，把上面两行合并成：
        // UAP_AccessibilityManager.EnableAccessibility(true, false);
#endif
    }
}
