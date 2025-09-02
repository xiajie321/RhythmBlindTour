using UnityEngine;
using TMPro;
using System.Globalization;
using System;

[DisallowMultipleComponent]
public class mTimeInputStepper : MonoBehaviour
{
    [Header("目标输入框（TMP InputField）")]
    [SerializeField] private TMP_InputField target;

    [Header("步长（秒）")]
    [Min(0.0001f)]
    [SerializeField] private float stepSeconds = 0.01f;

    [Header("小数位数（固定两位 0.01 精度）")]
    [SerializeField] private int decimals = 2;

    private NumberFormatInfo inv => CultureInfo.InvariantCulture.NumberFormat;

    private void Reset()
    {
        if (target == null) target = GetComponent<TMP_InputField>();
    }

    /// <summary>减少：按步长 -stepSeconds（仅在目标启用且有数据时生效）</summary>
    public void StepDown()
    {
        if (!CanOperate(out decimal cur)) return;
        decimal step = (decimal)stepSeconds;
        decimal next = Math.Round(cur - step, decimals, MidpointRounding.AwayFromZero);
        Apply(next);
    }

    /// <summary>增加：按步长 +stepSeconds（仅在目标启用且有数据时生效）</summary>
    public void StepUp()
    {
        if (!CanOperate(out decimal cur)) return;
        decimal step = (decimal)stepSeconds;
        decimal next = Math.Round(cur + step, decimals, MidpointRounding.AwayFromZero);
        Apply(next);
    }

    /// <summary>运行时修改步长</summary>
    public void SetStep(float seconds)
    {
        stepSeconds = Mathf.Max(0.0001f, seconds);
    }

    // —— 内部工具 —— //

    private bool CanOperate(out decimal current)
    {
        current = 0m;
        if (target == null) return false;

        // 仅当组件启用 + 处于激活 + 可交互
        if (!target.enabled || !target.gameObject.activeInHierarchy || !target.interactable) return false;

        // 必须有可解析的内容
        string txt = target.text;
        if (string.IsNullOrWhiteSpace(txt)) return false;

        // 先按 InvariantCulture 解析（支持 10.00 形式）；失败再按本地文化兜底
        if (decimal.TryParse(txt, NumberStyles.Float, inv, out current)) return true;
        return decimal.TryParse(txt, NumberStyles.Float, CultureInfo.CurrentCulture, out current);
    }

    private void Apply(decimal value)
    {
        // 固定两位小数输出
        string s = value.ToString("0." + new string('0', decimals), inv);

        // 直接赋值会触发 onValueChanged（如果你不想触发，可改用 SetTextWithoutNotify）
        target.text = s;
        // target.SetTextWithoutNotify(s); // 如需静默更新，改用这行
    }
}
