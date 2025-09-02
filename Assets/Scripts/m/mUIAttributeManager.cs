using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class mUIAttributeManager : MonoBehaviour
{
    public static mUIAttributeManager Instance { get; private set; }
    /// <summary>当为 true 时，允许“鼠标直改模式”（不再拦截原生输入）。</summary>
    public static bool DisableAttributeGating { get; private set; } = false;

    [Header("当 Toggle = On 时，启用“鼠标直改模式”")]
    [SerializeField] private Toggle directMouseToggle;

    [Tooltip("载入场景时是否立即根据 Toggle 应用一次")]
    [SerializeField] private bool applyOnStart = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (directMouseToggle)
        {
            directMouseToggle.onValueChanged.AddListener(OnToggleChanged);
            if (applyOnStart) OnToggleChanged(directMouseToggle.isOn);
        }
        else if (applyOnStart)
        {
            ApplyModeToAll();
        }
    }

    void OnDisable()
    {
        if (directMouseToggle)
            directMouseToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool on)
    {
        DisableAttributeGating = on;
        ApplyModeToAll();
    }

    public void ApplyModeToAll()
    {
        // 统一把模式应用到场景已存在的 Attribute 上
        foreach (var v in FindObjectsOfType<UIValueAttribute>(true)) v.ApplyGlobalMode();
        foreach (var s in FindObjectsOfType<UISliderAttribute>(true)) s.ApplyGlobalMode();
        foreach (var f in FindObjectsOfType<UIFileAttribute>(true)) f.ApplyGlobalMode();
        // 如有别的 Attribute 类型，也可在此追加
    }
}
