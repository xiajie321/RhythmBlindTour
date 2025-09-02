using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;
using Qf.ClassDatas.AudioEdit;
using System;

public class Test2 : MonoBehaviour, IController
{
    [Header("UI 进度条/面板")]
    public Slider loadingSlider;
    public GameObject loadingPanel;

    [Header("唯一注入者（必须存在）")]
    [SerializeField] private mDefaultAudioInitializer defaultInitializer;

    // ===========================
    // 导出控制开关（可被 UIToggle.onValueChanged 绑定）
    // ===========================
    [Header("Export Toggles")]
    [Tooltip("开：只导出一个统一 Offset=0；关：导出五类独立 TipOffsetMs")]
    public bool toggleUnifiedOffset = true;

    [Tooltip("开：每条 Note 附带 \"Type\" 字段；关：不导出 Type")]
    public bool toggleExportType = false;

    [Tooltip("开：额外导出一份 settings.Level（中文键名）；关：只导出 unified.Level")]
    public bool toggleExportGlobalSettings = true;

    // —— 对外公开的三个 Set 方法（绑定 Toggle 的 OnValueChanged(bool)）
    public void SetUnifiedOffset(bool value)
    {
        toggleUnifiedOffset = value;
        Debug.Log("[ExportToggle] UnifiedOffset = " + value);
    }
    public void SetExportType(bool value)
    {
        toggleExportType = value;
        Debug.Log("[ExportToggle] ExportType = " + value);
    }
    public void SetExportGlobalSettings(bool value)
    {
        toggleExportGlobalSettings = value;
        Debug.Log("[ExportToggle] ExportGlobalSettings(settings.Level) = " + value);
    }

    // 与项目一致的操作类型映射
    const int TYPE_UP = 0;
    const int TYPE_DOWN = 1;
    const int TYPE_LEFT = 2;
    const int TYPE_RIGHT = 3;
    const int TYPE_TAP = 4;

    #region 手动外部调用（仅绑定 Model 的 Load/Save）
    public void Load()
    {
        var model = this.GetModel<AudioEditModel>();
        model.Load();
    }

    public void Save()
    {
        var model = this.GetModel<AudioEditModel>();
        model.Save();
        var imc = FindObjectOfType<mInputMappingConfigurator>();
        if (imc) imc.FlushModifiersNow();
    }
    #endregion

    // ─────────────────────────────────────────────────────────
    // 总导出：unified.Level（必导） + settings.Level（可选，中文键名）
    // ─────────────────────────────────────────────────────────
    /// <summary>统一导出：事件 Level（必导） + 中文键名的全局设置 Level（可选）。</summary>
    public void ExportUnified()
    {
        var model = this.GetModel<AudioEditModel>();
        if (model == null)
        {
            Debug.LogError("[Export] AudioEditModel 未找到。");
            return;
        }

        // 选择保存基名
        string basePath = FileLoader.SaveLevelFile();
        if (string.IsNullOrEmpty(basePath))
        {
            Debug.LogWarning("[Export] 用户取消保存对话框，导出终止。");
            return;
        }

        // 路径解析：与 Level 同目录、同名但不同前缀
        string dir = Path.GetDirectoryName(basePath);
        string stem = Path.GetFileNameWithoutExtension(basePath);
        if (string.IsNullOrEmpty(dir)) dir = ".";

        // 1) 收集事件数据
        var buckets = CollectBucketsByOperation(model);

        // 2) 构建 unified（事件）JSON
        JObject unified = BuildUnifiedJson(model, buckets);

        // 3) 写 unified.Level（事件数据）
        string unifiedPath = Path.Combine(dir, stem + ".unified.Level");
        WriteJson(unifiedPath, unified);

        // 4) 如需：构建并写 settings.Level（中文键名的简洁全局设置）
        string settingsPath = null;
        if (toggleExportGlobalSettings)
        {
            JObject settings = BuildSettingsJson(model);
            settingsPath = Path.Combine(dir, stem + ".settings.Level");
            WriteJson(settingsPath, settings);
        }

        Debug.Log($"[Export] 导出完成：\n{unifiedPath}" + (settingsPath != null ? $"\n{settingsPath}" : ""));
        var imc = FindObjectOfType<mInputMappingConfigurator>();
        if (imc) imc.FlushModifiersNow();

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    // ─────────────────────────────────────────────────────────
    // 事件 JSON（unified.Level）
    // ─────────────────────────────────────────────────────────
    private (List<int> tap, List<int> left, List<int> right, List<int> up, List<int> down)
        CollectBucketsByOperation(AudioEditModel model)
    {
        var tapSet = new HashSet<int>();
        var leftSet = new HashSet<int>();
        var rightSet = new HashSet<int>();
        var upSet = new HashSet<int>();
        var downSet = new HashSet<int>();

        var tld = model.TimeLineData ?? new Dictionary<float, List<DrumsLoadData>>();
        foreach (var kv in tld)
        {
            int timeMs = SecToMsInt(kv.Key);
            var list = kv.Value;
            if (list == null) continue;

            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i]?.DrwmsData;
                if (d == null) continue;

                int op = (int)d.DtheTypeOfOperation;
                switch (op)
                {
                    case TYPE_TAP: tapSet.Add(timeMs); break;
                    case TYPE_LEFT: leftSet.Add(timeMs); break;
                    case TYPE_RIGHT: rightSet.Add(timeMs); break;
                    case TYPE_UP: upSet.Add(timeMs); break;
                    case TYPE_DOWN: downSet.Add(timeMs); break;
                }
            }
        }

        var tap = tapSet.ToList(); tap.Sort();
        var left = leftSet.ToList(); left.Sort();
        var right = rightSet.ToList(); right.Sort();
        var up = upSet.ToList(); up.Sort();
        var down = downSet.ToList(); down.Sort();

        return (tap, left, right, up, down);
    }

    private JObject BuildUnifiedJson(
        AudioEditModel model,
        (List<int> tap, List<int> left, List<int> right, List<int> up, List<int> down) buckets)
    {
        // Notes（Timing 固定；Type 可选）
        var tapArr = ToNoteArray(buckets.tap, () => toggleExportType ? "PlaceAtCenter" : null);
        var leftArr = ToNoteArray(buckets.left, () => toggleExportType ? "PlaceAtCenter" : null);
        var rightArr = ToNoteArray(buckets.right, () => toggleExportType ? "PlaceAtCenter" : null);
        var upArr = ToNoteArray(buckets.up, () => toggleExportType ? "PlaceAtCenter" : null);
        var downArr = ToNoteArray(buckets.down, () => toggleExportType ? "PlaceAtCenter" : null);

        // Offset：统一0 或 分类型（单位毫秒）
        JToken offsetNode = toggleUnifiedOffset
            ? new JValue(0)
            : new JObject
            {
                ["Click"] = SecToMsInt(model.TipOffsetClick?.Value ?? 0f),
                ["SwipeUp"] = SecToMsInt(model.TipOffsetSwipeUp?.Value ?? 0f),
                ["SwipeDown"] = SecToMsInt(model.TipOffsetSwipeDown?.Value ?? 0f),
                ["SwipeLeft"] = SecToMsInt(model.TipOffsetSwipeLeft?.Value ?? 0f),
                ["SwipeRight"] = SecToMsInt(model.TipOffsetSwipeRight?.Value ?? 0f),
            };

        // 事件根对象（保持英文键，方便逻辑消费）
        return new JObject
        {
            ["AudioOffset"] = SecToMsInt(model.MainAudioOffset?.Value ?? 0f),
            ["TapNotes"] = tapArr,
            ["SlideLeftNotes"] = leftArr,
            ["SlideRightNotes"] = rightArr,
            ["SlideUpNotes"] = upArr,
            ["SlideDownNotes"] = downArr,
            ["TipOffsetMs"] = offsetNode
        };
    }

    private static JArray ToNoteArray(IEnumerable<int> times, Func<string> typeGetter)
    {
        var arr = new JArray();
        foreach (var t in times)
        {
            var note = new JObject { ["Timing"] = t }; // Timing 必导
            var typeStr = typeGetter?.Invoke();
            if (!string.IsNullOrEmpty(typeStr))
                note["Type"] = typeStr;
            arr.Add(note);
        }
        return arr;
    }

    // ─────────────────────────────────────────────────────────
    // 设置 JSON（settings.Level）— 中文键名
    // ─────────────────────────────────────────────────────────
    private JObject BuildSettingsJson(AudioEditModel model)
    {
        // LevelMusic关卡音乐
        var levelMusic = new JObject
        {
            ["AudioName音乐文件"] = SafeName(model.EditAudioClip),
            ["Volume音量"] = model.EditAudioClipVolume?.Value ?? 1f,
            ["AudioOffsetMs偏移"] = SecToMsInt(model.MainAudioOffset?.Value ?? 0f)
        };

        // TipOffsetMs提示音偏移（统一 0 或 分类型）
        JToken tipOffset = toggleUnifiedOffset
            ? new JValue(0)
            : new JObject
            {
                ["Click点击鼓点"] = SecToMsInt(model.TipOffsetClick?.Value ?? 0f),
                ["SwipeUp上滑鼓点"] = SecToMsInt(model.TipOffsetSwipeUp?.Value ?? 0f),
                ["SwipeDown下滑鼓点"] = SecToMsInt(model.TipOffsetSwipeDown?.Value ?? 0f),
                ["SwipeLeft左滑鼓点"] = SecToMsInt(model.TipOffsetSwipeLeft?.Value ?? 0f),
                ["SwipeRight右滑鼓点"] = SecToMsInt(model.TipOffsetSwipeRight?.Value ?? 0f),
            };

        // Types鼓点（五类）
        var types = new JObject
        {
            ["Click点击鼓点"] = BuildOneTypeBlock_CN(model, TheTypeOfOperation.Click),
            ["SwipeUp上滑鼓点"] = BuildOneTypeBlock_CN(model, TheTypeOfOperation.SwipeUp),
            ["SwipeDown下滑鼓点"] = BuildOneTypeBlock_CN(model, TheTypeOfOperation.SwipeDown),
            ["SwipeLeft左滑鼓点"] = BuildOneTypeBlock_CN(model, TheTypeOfOperation.SwipeLeft),
            ["SwipeRight右滑鼓点"] = BuildOneTypeBlock_CN(model, TheTypeOfOperation.SwipeRight)
        };

        // 简洁 settings 根（中文键名）
        return new JObject
        {
            ["LevelMusic关卡音乐"] = levelMusic,
            ["TipOffsetMs提示音偏移"] = tipOffset,
            ["Types鼓点"] = types
        };
    }

    private JObject BuildOneTypeBlock_CN(AudioEditModel model, TheTypeOfOperation t)
    {
        model.TypeSettings.TryGetValue(t, out var ts);

        // 音频名（优先类型默认；否则读全局；Lose/Default 走全局）
        string tipName = ts?.TipAudio ?? GetTypeTipName(model, t);
        string succName = ts?.SucceedAudio ?? GetTypeSucceedName(model, t);
        string loseName = ts?.LoseAudio ?? SafeName(model.LoseAudioClip);
        string defName = ts?.DefaultAudio ?? SafeName(model.DefaultAudioClip);

        // 时间参数（若类型未设置则回落到全局默认）
        float exist = !float.IsNaN(ts?.TimeOfExistence ?? float.NaN) ? ts!.TimeOfExistence : model.TimeOfExistence.Value;
        float tipAdv = !float.IsNaN(ts?.TipAdvance ?? float.NaN) ? ts!.TipAdvance : model.TipOffset.Value;
        float tipPlay = !float.IsNaN(ts?.TipPlayOffset ?? float.NaN) ? ts!.TipPlayOffset : 0f;

        // 音量（若类型未设置则回落到全局默认）
        float tipVol = !float.IsNaN(ts?.TipVolume ?? float.NaN) ? ts!.TipVolume : model.PreAdventVolume.Value;
        float sucVol = !float.IsNaN(ts?.SucceedVolume ?? float.NaN) ? ts!.SucceedVolume : model.SucceedAudioVolume.Value;
        float loseVol = !float.IsNaN(ts?.LoseVolume ?? float.NaN) ? ts!.LoseVolume : model.LoseAudioVolume.Value;
        float defVol = !float.IsNaN(ts?.DefaultVolume ?? float.NaN) ? ts!.DefaultVolume : model.DefaultAudioVolume.Value;

        return new JObject
        {
            ["Audios音频文件"] = new JObject
            {
                ["Tip提示音"] = tipName,
                ["Succeed回答音"] = succName,
                ["Lose错误音"] = loseName,
                ["Default默认音"] = defName
            },
            ["Volumes音频音量"] = new JObject
            {
                ["Tip提示音"] = tipVol,
                ["Succeed回答音"] = sucVol,
                ["Lose错误音"] = loseVol,
                ["Default默认音"] = defVol
            },
            ["Timing鼓点设置"] = new JObject
            {
                ["Existence判定时长"] = exist,
                ["TipAdvance提示音提前"] = tipAdv,
                ["TipPlayOffset提示音偏移"] = tipPlay
            }
        };
    }

    // ─────────────────────────────────────────────────────────
    // 工具
    // ─────────────────────────────────────────────────────────
    private static int SecToMsInt(float sec)
    {
        decimal d = (decimal)sec;
        decimal ms = d * 1000m;
        return (int)decimal.Round(ms, 0, MidpointRounding.AwayFromZero);
    }

    private static void WriteJson(string path, JObject jo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); // UTF-8 无 BOM
        File.WriteAllText(path, jo.ToString(Newtonsoft.Json.Formatting.Indented), enc);
        Debug.Log($"[Export][JSON] 写入成功：{path}");
    }

    private static string SafeName(AudioClip clip) => clip ? clip.name : "";

    private static string GetTypeTipName(AudioEditModel m, TheTypeOfOperation t)
    {
        return t switch
        {
            TheTypeOfOperation.SwipeUp => SafeName(m.UpTipsAudioClip),
            TheTypeOfOperation.SwipeDown => SafeName(m.DownTipsAudioClip),
            TheTypeOfOperation.SwipeLeft => SafeName(m.LeftTipsAudioClip),
            TheTypeOfOperation.SwipeRight => SafeName(m.RightTipsAudioClip),
            _ => SafeName(m.ClickTipsAudioClip),
        };
    }

    private static string GetTypeSucceedName(AudioEditModel m, TheTypeOfOperation t)
    {
        return t switch
        {
            TheTypeOfOperation.SwipeUp => SafeName(m.UpSucceedAudioClip),
            TheTypeOfOperation.SwipeDown => SafeName(m.DownSucceedAudioClip),
            TheTypeOfOperation.SwipeLeft => SafeName(m.LeftSucceedAudioClip),
            TheTypeOfOperation.SwipeRight => SafeName(m.RightSucceedAudioClip),
            _ => SafeName(m.ClickSucceedAudioClip),
        };
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
}
