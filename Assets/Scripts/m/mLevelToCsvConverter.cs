using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class mLevelToCsvConverter : MonoBehaviour
{
    [Header("要转换的 .Level 文件路径（相对/绝对）")]
    public string levelPath = "Assets/StreamingAssets/Levels/Level00/Level.Level";

    [Header("谱面导出时按时间升序")]
    public bool sortChartByTime = true;

    [Header("写入编码（UTF-8 无 BOM）")]
    public bool utf8NoBom = true;

    #region 导出 CSV（global/chart/byType）
    public void Convert()
    {
        try
        {
            if (levelPath.Contains("Streamingassets"))
                levelPath = levelPath.Replace("Streamingassets", "StreamingAssets");

            string absLevelPath = ToAbsolutePath(levelPath);
            if (string.IsNullOrEmpty(absLevelPath))
            {
                Debug.LogError("[Level→CSV] 路径为空。");
                return;
            }
            if (!File.Exists(absLevelPath))
            {
                Debug.LogError($"[Level→CSV] 文件不存在：{absLevelPath}");
                return;
            }

            var json = File.ReadAllText(absLevelPath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[Level→CSV] 文件内容为空。");
                return;
            }

            var root = JObject.Parse(json);

            // 1) 整体设置（纵向：name,value）
            string globalCsv = Path.ChangeExtension(absLevelPath, ".global.csv");
            WriteGlobalCsv(globalCsv, root);

            // 2) 明细表（每个鼓点一行）
            string chartCsv = Path.ChangeExtension(absLevelPath, ".chart.csv");
            var tld = root["TimeLineData"] as JObject;
            var rows = (tld == null) ? new List<Row>() : ExtractChartRows(tld);
            if (sortChartByTime) rows.Sort((a, b) => a.time.CompareTo(b.time));
            WriteChartCsv(chartCsv, rows);

            // 3) 按类型整合（宽表）
            string byTypeCsv = Path.ChangeExtension(absLevelPath, ".byType.csv");
            WriteChartCsvByType(byTypeCsv, rows);

            Debug.Log($"[Level→CSV] 导出完成：\n{globalCsv}\n{chartCsv}\n{byTypeCsv}");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Level→CSV] 发生异常：\n{ex}");
        }
    }

    private void WriteGlobalCsv(string csvPath, JObject root)
    {
        var keys = new List<string>
        {
            "BPM","BeatA","BeatB",
            "EditAudioClip","EditAudioClipVolume",
            "DefaultAudioClip","DefaultAudioVolume",
            "LoseAudioClip",
            "DownSucceedAudioClip","UpSucceedAudioClip","LeftSucceedAudioClip","RightSucceedAudioClip","ClickSucceedAudioClip",
            "DownTipsAudioClip","UpTipsAudioClip","LeftTipsAudioClip","RightTipsAudioClip","ClickTipsAudioClip",
            "TipOffset","TimeOfExistence","ThisTime",
            "MainAudioOffset","TipOffsetSwipeUp","TipOffsetSwipeDown","TipOffsetSwipeLeft","TipOffsetSwipeRight","TipOffsetClick"
        };

        var lines = new List<string>(keys.Count + 1) { "name,value" };
        foreach (var k in keys)
        {
            string v = ReadScalar(root, k);
            lines.Add($"{Csv(k)},{Csv(v)}");
        }

        WriteAll(csvPath, lines);
    }

    private static string ReadScalar(JObject root, string key)
    {
        var tok = root[key];
        if (tok == null) return "";

        if (tok.Type == JTokenType.String)
            return tok.Value<string>();

        if (tok.Type == JTokenType.Float || tok.Type == JTokenType.Integer)
            // 关键修复：使用 System.Convert，避免被本类的 Convert() 方法遮蔽
            return System.Convert.ToString(tok.Value<double>(), CultureInfo.InvariantCulture);

        if (tok.Type == JTokenType.Boolean)
            return tok.Value<bool>() ? "true" : "false";

        return "";
    }

    // ---------- 明细表 ----------
    private struct Row
    {
        public float time;
        public string drumCode;
        public int laneIndex;     // 对应 TheTypeOfOperation
        public string laneName;
        public float existence;
        public float tipOffset;
        public string preTipClip;
        public string succeedClip;
        public string loseClip;
        public float preTipVol;
        public float succeedVol;
        public float loseVol;
    }

    private static List<Row> ExtractChartRows(JObject timeLineData)
    {
        var rows = new List<Row>(1024);

        if (timeLineData.ContainsKey("keys") && timeLineData.ContainsKey("values"))
        {
            var keysArr = timeLineData["keys"] as JArray;
            var valsArr = timeLineData["values"] as JArray;
            if (keysArr == null || valsArr == null) return rows;

            int count = Math.Min(keysArr.Count, valsArr.Count);
            for (int i = 0; i < count; i++)
            {
                float time = SafeFloat(keysArr[i]);
                var list = valsArr[i] as JArray;
                if (list == null) continue;
                foreach (var item in list) TryAddRow(rows, time, item as JObject);
            }
        }
        else
        {
            foreach (var kv in timeLineData)
            {
                float time = SafeFloat(kv.Key);
                var list = kv.Value as JArray;
                if (list == null) continue;
                foreach (var item in list) TryAddRow(rows, time, item as JObject);
            }
        }
        return rows;
    }

    private static void TryAddRow(List<Row> rows, float time, JObject drumsLoadData)
    {
        if (drumsLoadData == null) return;
        var d = drumsLoadData["DrwmsData"] as JObject;
        var m = drumsLoadData["MusicData"] as JObject;
        if (d == null) return;

        int laneIndex = (int)SafeFloat(d["DtheTypeOfOperation"]);
        string laneName = S(d["DtheTypeOfOperation"]) switch
        {
            string s when !string.IsNullOrEmpty(s) => s,
            _ => laneIndex.ToString()
        };

        rows.Add(new Row
        {
            time = Round2(time),
            drumCode = S(d["DrumCode"]),
            laneIndex = laneIndex,
            laneName = laneName,
            existence = Round2(SafeFloat(d["VTimeOfExistence"])),
            tipOffset = Round2(SafeFloat(d["VPreAdventAudioClipOffsetTime"])),
            preTipClip = S(d["FPreAdventAudioClipPath"]),
            succeedClip = S(d["FSucceedAudioClipPath"]),
            loseClip = S(d["FLoseAudioClipPath"]),
            preTipVol = m != null ? SafeFloat(m["SPreAdventVolume"]) : 1f,
            succeedVol = m != null ? SafeFloat(m["SSucceedVolume"]) : 1f,
            loseVol = m != null ? SafeFloat(m["SLoseVolume"]) : 1f
        });
    }

    private void WriteChartCsv(string csvPath, List<Row> rows)
    {
        var lines = new List<string>(Mathf.Max(1, rows.Count + 1));
        lines.Add(string.Join(",",
            "drumCode", "time", "laneIndex", "laneName", "type",
            "existence", "tipOffset", "preTipClip", "succeedClip", "loseClip",
            "preTipVol", "succeedVol", "loseVol"));

        foreach (var r in rows)
        {
            string line = string.Join(",",
                Csv(r.drumCode),
                Csv(r.time.ToString("0.00", CultureInfo.InvariantCulture)),
                Csv(r.laneIndex.ToString()),
                Csv(r.laneName),
                Csv(r.laneName),
                Csv(r.existence.ToString("0.00", CultureInfo.InvariantCulture)),
                Csv(r.tipOffset.ToString("0.00", CultureInfo.InvariantCulture)),
                Csv(r.preTipClip),
                Csv(r.succeedClip),
                Csv(r.loseClip),
                Csv(r.preTipVol.ToString("0.###", CultureInfo.InvariantCulture)),
                Csv(r.succeedVol.ToString("0.###", CultureInfo.InvariantCulture)),
                Csv(r.loseVol.ToString("0.###", CultureInfo.InvariantCulture))
            );
            lines.Add(line);
        }

        WriteAll(csvPath, lines);
    }
    #endregion

    // =====================================================================
    // 按类型整合（宽表）——与 TheTypeOfOperation 一致
    // TheTypeOfOperation: SwipeUp=0, SwipeDown=1, SwipeLeft=2, SwipeRight=3, Click=4
    // 列顺序：TapNotes,SlideLeftNotes,SlideRightNotes,SlideUpNotes,SlideDownNotes
    // =====================================================================
    private void WriteChartCsvByType(string csvPath, List<Row> rows)
    {
        const int TYPE_UP = 0;
        const int TYPE_DOWN = 1;
        const int TYPE_LEFT = 2;
        const int TYPE_RIGHT = 3;
        const int TYPE_TAP = 4;

        var tap = new List<string>();
        var left = new List<string>();
        var right = new List<string>();
        var up = new List<string>();
        var down = new List<string>();

        foreach (var r in rows)
        {
            string t = r.time.ToString("0.00", CultureInfo.InvariantCulture);
            switch (r.laneIndex)
            {
                case TYPE_TAP: tap.Add(t); break;
                case TYPE_LEFT: left.Add(t); break;
                case TYPE_RIGHT: right.Add(t); break;
                case TYPE_UP: up.Add(t); break;
                case TYPE_DOWN: down.Add(t); break;
            }
        }

        if (sortChartByTime)
        {
            Comparison<string> cmp = (a, b) =>
            {
                float fa = SafeFloat(a);
                float fb = SafeFloat(b);
                return fa.CompareTo(fb);
            };
            tap.Sort(cmp); left.Sort(cmp); right.Sort(cmp); up.Sort(cmp); down.Sort(cmp);
        }

        int maxLen = Mathf.Max(tap.Count, left.Count, right.Count, up.Count, down.Count);
        var lines = new List<string>(maxLen + 1)
        {
            "TapNotes,SlideLeftNotes,SlideRightNotes,SlideUpNotes,SlideDownNotes"
        };

        for (int i = 0; i < maxLen; i++)
        {
            string a = i < tap.Count ? Csv(tap[i]) : "";
            string b = i < left.Count ? Csv(left[i]) : "";
            string c = i < right.Count ? Csv(right[i]) : "";
            string d = i < up.Count ? Csv(up[i]) : "";
            string e = i < down.Count ? Csv(down[i]) : "";
            lines.Add(string.Join(",", a, b, c, d, e));
        }

        WriteAll(csvPath, lines);
    }

    // =====================================================================
    // 旧 .Level → 简化 .Level（只保留各类鼓点中心，单位：毫秒，整型）
    // {
    //   "AudioOffset": 0,
    //   "TapNotes":[int...], "SlideLeftNotes":[int...], "SlideRightNotes":[int...], "SlideUpNotes":[int...], "SlideDownNotes":[int...]
    // }
    // =====================================================================
    public void ConvertOldLevelToSimpleNotes()
    {
        try
        {
            if (levelPath.Contains("Streamingassets"))
                levelPath = levelPath.Replace("Streamingassets", "StreamingAssets");

            string absLevelPath = ToAbsolutePath(levelPath);
            if (string.IsNullOrEmpty(absLevelPath))
            {
                Debug.LogError("[Level→SimpleLevel] 路径为空。");
                return;
            }
            if (!File.Exists(absLevelPath))
            {
                Debug.LogError($"[Level→SimpleLevel] 文件不存在：{absLevelPath}");
                return;
            }

            var json = File.ReadAllText(absLevelPath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[Level→SimpleLevel] 文件内容为空。");
                return;
            }

            var root = JObject.Parse(json);
            var tld = root["TimeLineData"] as JObject;
            if (tld == null)
            {
                Debug.LogError("[Level→SimpleLevel] 未找到 TimeLineData。");
                return;
            }

            const int TYPE_UP = 0;
            const int TYPE_DOWN = 1;
            const int TYPE_LEFT = 2;
            const int TYPE_RIGHT = 3;
            const int TYPE_TAP = 4;

            var tap = new List<int>();
            var left = new List<int>();
            var right = new List<int>();
            var up = new List<int>();
            var down = new List<int>();

            if (tld.ContainsKey("keys") && tld.ContainsKey("values"))
            {
                var keysArr = tld["keys"] as JArray;
                var valsArr = tld["values"] as JArray;
                if (keysArr != null && valsArr != null)
                {
                    int count = Math.Min(keysArr.Count, valsArr.Count);
                    for (int i = 0; i < count; i++)
                    {
                        int timeMs = TimeTokenToMsInt(keysArr[i]);
                        var list = valsArr[i] as JArray;
                        if (list == null) continue;
                        foreach (var item in list)
                            ClassifyOneMs(item as JObject, timeMs, tap, left, right, up, down,
                                TYPE_TAP, TYPE_LEFT, TYPE_RIGHT, TYPE_UP, TYPE_DOWN);
                    }
                }
            }
            else
            {
                foreach (var kv in tld)
                {
                    int timeMs = SecStringToMsInt(kv.Key);
                    var list = kv.Value as JArray;
                    if (list == null) continue;
                    foreach (var item in list)
                        ClassifyOneMs(item as JObject, timeMs, tap, left, right, up, down,
                            TYPE_TAP, TYPE_LEFT, TYPE_RIGHT, TYPE_UP, TYPE_DOWN);
                }
            }

            if (sortChartByTime)
            {
                tap.Sort(); left.Sort(); right.Sort(); up.Sort(); down.Sort();
            }

            var outRoot = new JObject
            {
                ["AudioOffset"] = 0,
                ["TapNotes"] = ToNoteArray(tap),
                ["SlideLeftNotes"] = ToNoteArray(left),
                ["SlideRightNotes"] = ToNoteArray(right),
                ["SlideUpNotes"] = ToNoteArray(up),
                ["SlideDownNotes"] = ToNoteArray(down),
            };


            string outPath = absLevelPath + ".simple.Level";
            WriteJson(outPath, outRoot);

            Debug.Log($"[Level→SimpleLevel] 生成完成：{outPath}\n" +
                      $"Tap:{tap.Count}  Left:{left.Count}  Right:{right.Count}  Up:{up.Count}  Down:{down.Count}");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Level→SimpleLevel] 发生异常：\n{ex}");
        }
    }

    // =====================================================================
    // 第三种：旧 .Level → 简化 .Level（附带“5种操作的全局配置”，不写 AudioOffset）
    // {
    //   "TapNotes":[int...], "SlideLeftNotes":[int...], "SlideRightNotes":[int...], "SlideUpNotes":[int...], "SlideDownNotes":[int...],
    //   "OperationGlobals": { Click:{...}, SwipeLeft:{...}, SwipeRight:{...}, SwipeUp:{...}, SwipeDown:{...} }
    // }
    // =====================================================================
    public void ConvertOldLevelToNotesWithOperationGlobals()
    {
        try
        {
            if (levelPath.Contains("Streamingassets"))
                levelPath = levelPath.Replace("Streamingassets", "StreamingAssets");

            string absLevelPath = ToAbsolutePath(levelPath);
            if (string.IsNullOrEmpty(absLevelPath))
            {
                Debug.LogError("[Level→SimpleWithOps] 路径为空。");
                return;
            }
            if (!File.Exists(absLevelPath))
            {
                Debug.LogError($"[Level→SimpleWithOps] 文件不存在：{absLevelPath}");
                return;
            }

            var json = File.ReadAllText(absLevelPath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[Level→SimpleWithOps] 文件内容为空。");
                return;
            }

            var root = JObject.Parse(json);
            var tld = root["TimeLineData"] as JObject;
            if (tld == null)
            {
                Debug.LogError("[Level→SimpleWithOps] 未找到 TimeLineData。");
                return;
            }

            const int TYPE_UP = 0;
            const int TYPE_DOWN = 1;
            const int TYPE_LEFT = 2;
            const int TYPE_RIGHT = 3;
            const int TYPE_TAP = 4;

            var tap = new List<int>();
            var left = new List<int>();
            var right = new List<int>();
            var up = new List<int>();
            var down = new List<int>();

            // 收集鼓点中心（毫秒）
            if (tld.ContainsKey("keys") && tld.ContainsKey("values"))
            {
                var keysArr = tld["keys"] as JArray;
                var valsArr = tld["values"] as JArray;
                if (keysArr != null && valsArr != null)
                {
                    int count = Math.Min(keysArr.Count, valsArr.Count);
                    for (int i = 0; i < count; i++)
                    {
                        int timeMs = TimeTokenToMsInt(keysArr[i]);
                        var list = valsArr[i] as JArray;
                        if (list == null) continue;
                        foreach (var item in list)
                            ClassifyOneMs(item as JObject, timeMs, tap, left, right, up, down,
                                TYPE_TAP, TYPE_LEFT, TYPE_RIGHT, TYPE_UP, TYPE_DOWN);
                    }
                }
            }
            else
            {
                foreach (var kv in tld)
                {
                    int timeMs = SecStringToMsInt(kv.Key);
                    var list = kv.Value as JArray;
                    if (list == null) continue;
                    foreach (var item in list)
                        ClassifyOneMs(item as JObject, timeMs, tap, left, right, up, down,
                            TYPE_TAP, TYPE_LEFT, TYPE_RIGHT, TYPE_UP, TYPE_DOWN);
                }
            }

            if (sortChartByTime)
            {
                tap.Sort(); left.Sort(); right.Sort(); up.Sort(); down.Sort();
            }

            // 组装“5种操作的全局配置”
            var opGlobals = new JObject
            {
                ["Click"] = new JObject
                {
                    ["SucceedAudioClip"] = ReadString(root, "ClickSucceedAudioClip"),
                    ["TipsAudioClip"] = ReadString(root, "ClickTipsAudioClip"),
                    ["TipOffsetMs"] = ReadMsFromToken(root["TipOffsetClick"])
                },
                ["SwipeLeft"] = new JObject
                {
                    ["SucceedAudioClip"] = ReadString(root, "LeftSucceedAudioClip"),
                    ["TipsAudioClip"] = ReadString(root, "LeftTipsAudioClip"),
                    ["TipOffsetMs"] = ReadMsFromToken(root["TipOffsetSwipeLeft"])
                },
                ["SwipeRight"] = new JObject
                {
                    ["SucceedAudioClip"] = ReadString(root, "RightSucceedAudioClip"),
                    ["TipsAudioClip"] = ReadString(root, "RightTipsAudioClip"),
                    ["TipOffsetMs"] = ReadMsFromToken(root["TipOffsetSwipeRight"])
                },
                ["SwipeUp"] = new JObject
                {
                    ["SucceedAudioClip"] = ReadString(root, "UpSucceedAudioClip"),
                    ["TipsAudioClip"] = ReadString(root, "UpTipsAudioClip"),
                    ["TipOffsetMs"] = ReadMsFromToken(root["TipOffsetSwipeUp"])
                },
                ["SwipeDown"] = new JObject
                {
                    ["SucceedAudioClip"] = ReadString(root, "DownSucceedAudioClip"),
                    ["TipsAudioClip"] = ReadString(root, "DownTipsAudioClip"),
                    ["TipOffsetMs"] = ReadMsFromToken(root["TipOffsetSwipeDown"])
                }
            };

            var outRoot = new JObject
            {
                ["TapNotes"] = ToNoteArray(tap),
                ["SlideLeftNotes"] = ToNoteArray(left),
                ["SlideRightNotes"] = ToNoteArray(right),
                ["SlideUpNotes"] = ToNoteArray(up),
                ["SlideDownNotes"] = ToNoteArray(down),
                ["OperationGlobals"] = opGlobals
            };

            string outPath = absLevelPath + ".simple.withOps.Level";
            WriteJson(outPath, outRoot);

            Debug.Log($"[Level→SimpleWithOps] 生成完成：{outPath}\n" +
                      $"Tap:{tap.Count}  Left:{left.Count}  Right:{right.Count}  Up:{up.Count}  Down:{down.Count}");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Level→SimpleWithOps] 发生异常：\n{ex}");
        }
    }

    // 根据旧鼓点项分类到各类毫秒列表（timeMs 已经是整数毫秒）
    private static void ClassifyOneMs(
        JObject item, int timeMs,
        List<int> tap, List<int> left, List<int> right, List<int> up, List<int> down,
        int TYPE_TAP, int TYPE_LEFT, int TYPE_RIGHT, int TYPE_UP, int TYPE_DOWN)
    {
        if (item == null) return;

        int t = 0;
        if (item["DrwmsData"] is JObject d && d["DtheTypeOfOperation"] != null)
            t = (int)SafeFloat(d["DtheTypeOfOperation"]);
        else if (item["type"] != null)
            t = (int)SafeFloat(item["type"]);

        switch (t)
        {
            case var _ when t == TYPE_TAP: tap.Add(timeMs); break;
            case var _ when t == TYPE_LEFT: left.Add(timeMs); break;
            case var _ when t == TYPE_RIGHT: right.Add(timeMs); break;
            case var _ when t == TYPE_UP: up.Add(timeMs); break;
            case var _ when t == TYPE_DOWN: down.Add(timeMs); break;
            default: break;
        }
    }

    #region 写文件与通用工具
    // 你希望对象里用什么字段名存时间：如果你的 Note 类里是 Center/TimeMs，改这里即可
    private const string NOTE_TIME_KEY = "Timing";

    private static JArray ToNoteArray(IEnumerable<int> times)
    {
        var arr = new JArray();
        foreach (var t in times)
            arr.Add(new JObject { [NOTE_TIME_KEY] = t });  // 封一层：对象里存时间
        return arr;
    }

    private void WriteJson(string path, JObject jo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var enc = utf8NoBom ? new UTF8Encoding(false) : Encoding.UTF8;
        File.WriteAllText(path, jo.ToString(Newtonsoft.Json.Formatting.Indented), enc);
    }

    private void WriteAll(string path, List<string> lines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var content = string.Join("\n", lines);
        var enc = utf8NoBom ? new UTF8Encoding(false) : Encoding.UTF8;
        File.WriteAllText(path, content, enc);
    }

    private static float SafeFloat(JToken t)
    {
        if (t == null) return 0f;
        if (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) return t.Value<float>();
        if (t.Type == JTokenType.String &&
            float.TryParse(t.Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            return f;
        return 0f;
    }

    private static float SafeFloat(string s)
    {
        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return f;
        return 0f;
    }

    private static string S(JToken t) => t == null ? "" : (t.Type == JTokenType.String ? t.Value<string>() : t.ToString());

    private static float Round2(float f) => (float)Math.Round(f, 2, MidpointRounding.ToEven);

    private static string Csv(string raw)
    {
        if (raw == null) return "";
        bool needQuote = raw.Contains(',') || raw.Contains('"') || raw.Contains('\n') || raw.Contains('\r');
        if (!needQuote) return raw;
        return "\"" + raw.Replace("\"", "\"\"") + "\"";
    }

    private static string ToAbsolutePath(string maybeRelativePath)
    {
        if (string.IsNullOrEmpty(maybeRelativePath)) return "";
        string p = maybeRelativePath.Replace('\\', '/');
        if (Path.IsPathRooted(p)) return Path.GetFullPath(p);
#if UNITY_EDITOR
        string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
#else
        string projectRoot = Directory.GetParent(Application.dataPath) != null
            ? Directory.GetParent(Application.dataPath)!.FullName
            : Application.dataPath;
#endif
        return Path.GetFullPath(Path.Combine(projectRoot, p));
    }

    // —— 精确“秒字符串/Token”→int 毫秒（decimal 避免浮点误差） ——
    private static int TimeTokenToMsInt(JToken t)
    {
        if (t == null) return 0;
        string s = (t.Type == JTokenType.String) ? t.Value<string>() : t.ToString();
        return SecStringToMsInt(s);
    }

    private static int ReadMsFromToken(JToken t)
    {
        if (t == null) return 0;
        string s = (t.Type == JTokenType.String) ? t.Value<string>() : t.ToString();
        return SecStringToMsInt(s);
    }

    private static string ReadString(JObject root, string key)
    {
        var tok = root[key];
        if (tok == null) return "";
        if (tok.Type == JTokenType.String) return tok.Value<string>();
        return tok.ToString();
    }

    private static int SecStringToMsInt(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;

        if (decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
        {
            decimal ms = sec * 1000m;             // 用 decimal，避免浮点误差
            decimal trunc = decimal.Truncate(ms);  // 期望是整数毫秒（0.01s→10ms 步进）
            if (ms == trunc) return (int)trunc;

            // 极少数非常规：做一次最近整数收敛
            return (int)decimal.Round(ms, 0, MidpointRounding.AwayFromZero);
        }
        return 0;
    }
    #endregion
}
