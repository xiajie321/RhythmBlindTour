using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using QFramework;
using Qf.Models.AudioEdit;
using Qf.Querys.AudioEdit;
using Qf.Events;

public class mUIBeatTeleport : MonoBehaviour, IController
{
    [Header("在这里指定用于显示输入/提示的 TMP_Text")]
    [SerializeField] private TMP_Text displayText;

    [Header("启动/取消时同步显示或隐藏的容器对象")]
    [SerializeField] private GameObject container;   // 可选：传送状态下一起显示/隐藏的面板或背景

    [Header("（可选）页面切换器：若为空会自动查找")]
    [SerializeField] private mInputMappingConfigurator pageSwitcher; // ★ 新增：用于切到 Page4

    [Header("键盘：是否同时允许全角分号/数字")]
    [SerializeField] private bool allowFullwidthChars = true;

    private bool _active = false;
    private string _buffer = "";

    /// <summary>启动传送状态</summary>
    public void StartTeleport()
    {
        _active = true;
        _buffer = string.Empty;
        UpdateDisplay();

        if (displayText) displayText.gameObject.SetActive(true);
        if (container) container.SetActive(true);

        mTTS.Speak("传送");
    }

    /// <summary>确认传送：解析“数字;数字”，合法则跳转并切回 Page4；非法则清空等待重输。</summary>
    public void ConfirmTeleport()
    {
        if (!_active) return;

        if (TryParseInput(_buffer, out int measure1, out int beat1))
        {
            var model = this.GetModel<AudioEditModel>();
            if (model != null && model.BPM > 0 && model.BeatA > 0 && model.BeatB > 0)
            {
                if (measure1 >= 1 && beat1 >= 1 && beat1 <= model.BeatA)
                {
                    // 一拍时长与刻度一致
                    float beatDuration = 60f / model.BPM * (4f / model.BeatB);

                    // 目标秒：第 measure1 小节第 beat1 拍（1基）
                    long totalBeatIndex = (long)(measure1 - 1) * model.BeatA + (beat1 - 1);
                    float targetSec = totalBeatIndex * beatDuration;

                    // 可选：裁剪到音频长度范围
                    float clipLen = this.SendQuery(new QueryAudioEditAudioClipLength());
                    if (clipLen > 0) targetSec = Mathf.Clamp(targetSec, 0f, Math.Max(0f, clipLen - 0.01f));

                    // 移动时间针
                    var timeHand = GameObject.FindObjectOfType<UIAudioEditTimeHand>();
                    if (timeHand != null)
                    {
                        timeHand.SetTime(targetSec, true);
                        TypeEventSystem.Global.Send(new OnUpdateThisTime { ThisTime = targetSec });
                    }
                    else
                    {
                        TypeEventSystem.Global.Send(new OnUpdateThisTime { ThisTime = targetSec });
                    }

                    // 显示 & 朗读 “10节3拍”
                    _buffer = $"{measure1};{beat1}";
                    UpdateDisplay();
                    mTTS.Speak($"{measure1}节{beat1}拍");

                    // ★ 成功后退出并切回 Page4
                    ExitAndSwitchToPage4();
                    return;
                }
            }
        }

        // 非法：清空并等待
        _buffer = string.Empty;
        UpdateDisplay();
    }

    /// <summary>取消传送：清空输入、隐藏 UI，朗读“取消”，并切回 Page4。</summary>
    public void CancelTeleport()
    {
        if (displayText)
        {
            displayText.text = string.Empty;
            displayText.gameObject.SetActive(false);
        }
        if (container) container.SetActive(false);

        _buffer = string.Empty;
        _active = false;

        mTTS.Speak("取消");

        // ★ 取消时也切回 Page1
        SwitchToPage1();
    }

    private void Update()
    {
        if (!_active) return;

        // Enter 确认 / Esc 取消
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmTeleport();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTeleport();
            return;
        }

        // 逐字符读取
        string s = Input.inputString;
        if (string.IsNullOrEmpty(s)) return;

        bool changed = false;
        foreach (char raw in s)
        {
            char c = NormalizeChar(raw);

            if (c == '\b') // 退格
            {
                if (_buffer.Length > 0) { _buffer = _buffer.Substring(0, _buffer.Length - 1); changed = true; }
            }
            else if (char.IsDigit(c))
            {
                _buffer += c;
                changed = true;
            }
            else if (c == ';') // 分号分隔
            {
                if (!_buffer.Contains(";")) { _buffer += ';'; changed = true; }
            }
            // 其他字符忽略
        }

        if (changed) UpdateDisplay();
    }

    // ====== 小工具 ======

    // 退出状态 + 隐藏 UI + 切 Page4
    private void ExitAndSwitchToPage4()
    {
        _active = false;
        if (displayText)
        {
            displayText.gameObject.SetActive(false);
        }
        if (container) container.SetActive(false);

        SwitchToPage1();
    }

    // 切换到 Page4（优先用 Inspector 指定的对象，缺省时自动查找）
    private void SwitchToPage1()
    {
        var sw = pageSwitcher;
        if (sw == null)
        {
            // 自动查找（若场景内只有一个配置器，通常能找到）
            sw = FindObjectOfType<mInputMappingConfigurator>();
        }

        if (sw != null)
        {
            try { sw.SwitchUIToPage1(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[mUIBeatTeleport] 切换 Page4 失败：{e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[mUIBeatTeleport] 未找到 mInputMappingConfigurator，无法切换至 Page1。");
        }
    }

    // 把全角数字/分号转半角；其余原样返回
    private char NormalizeChar(char c)
    {
        if (!allowFullwidthChars) return c;
        if (c >= '０' && c <= '９') return (char)('0' + (c - '０'));
        if (c == '；') return ';';
        if (c == '，' || c == '、') return ';';
        return c;
    }

    // 解析 “number;number” → (measure, beat)
    private bool TryParseInput(string buffer, out int measure, out int beat)
    {
        measure = beat = 0;
        if (string.IsNullOrEmpty(buffer)) return false;

        string norm = buffer.Replace('；', ';').Replace('，', ';').Replace('、', ';');
        var parts = norm.Split(';');
        if (parts.Length != 2) return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out measure)) return false;
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out beat)) return false;

        return true;
    }

    // 把 _buffer 渲染到 UI：结构合法时显示 “10节3拍”，否则显示原始输入
    private void UpdateDisplay()
    {
        if (!displayText) return;

        if (TryParseInput(_buffer, out int m, out int b) && m >= 1 && b >= 1)
            displayText.text = $"{m}节{b}拍";
        else
            displayText.text = _buffer;
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
}
