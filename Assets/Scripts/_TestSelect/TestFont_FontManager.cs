using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace TestRevolver
{
    public enum LanguageType
    {
        ChineseS,
        ChineseT,
        English,
        Japanese
    }

    public enum LanguageTypeWithDefault
    {
        Default,
        ChineseS,
        ChineseT,
        English,
        Japanese
    }

    [System.Serializable]
    public class LocalizedFontSetting
    {
        public LanguageType language = LanguageType.English;
        public Font fontAsset;
        public Vector3 localScale = Vector3.one;
        public float characterSpacing = 0f;
        public Vector4 margins = Vector4.zero;
        public float fontSize = 36f;

        public string GetLanguageCode()
        {
            return language switch
            {
                LanguageType.ChineseS => "zh-CN",
                LanguageType.ChineseT => "zh-TW",
                LanguageType.English => "en",
                LanguageType.Japanese => "ja",
                _ => "en"
            };
        }
    }

    [System.Serializable]
    public class FontTargetText
    {
        public string groupName;
        public GameObject textGameObject;
        public bool applyCustomSetting = true;

        [Tooltip("用于从 CSV 中获取翻译的 Key")]
        public string localizedKey;

        public void TryUpdateGroupName()
        {
            if (textGameObject == null)
            {
                groupName = "";
                return;
            }

            var tmp = textGameObject.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                groupName = tmp.text;
                return;
            }

            var uiText = textGameObject.GetComponent<Text>();
            if (uiText != null)
            {
                groupName = uiText.text;
                return;
            }

            groupName = "(无文本组件)";
        }
    }

    public class TestFont_FontManager : MonoBehaviour
    {
        [Header("当前语言设置（Default 表示跟随系统）")]
        public LanguageTypeWithDefault currentLanguage = LanguageTypeWithDefault.Default;

        [Header("CSV 路径（相对于 Assets）")]
        public string csvPath = "Assets/Fonts/Localization/StringTestTable.csv";

        [Header("所有需要管理的文本 GameObject（Text 或 TMP）")]
        public List<FontTargetText> m_managedTextList = new();

        [Header("各语言字体与样式设置")]
        public List<LocalizedFontSetting> m_languageSettings = new();

        // ✅ 静态变量：跨场景共享语言设置
        public static LanguageTypeWithDefault s_currentLanguage = LanguageTypeWithDefault.Default;

        private Dictionary<string, Dictionary<string, string>> m_localizedData = new();

        private void Start()
        {
            s_currentLanguage = currentLanguage;
            LoadLocalizationCsv();
            ApplyCurrentLanguageSettings();
        }

        private string GetCurrentLanguageCode()
        {
            LanguageTypeWithDefault lang = s_currentLanguage;

            if (lang == LanguageTypeWithDefault.Default)
            {
                return Application.systemLanguage switch
                {
                    SystemLanguage.ChineseSimplified => "zh-CN",
                    SystemLanguage.ChineseTraditional => "zh-TW",
                    SystemLanguage.Japanese => "ja",
                    SystemLanguage.English => "en",
                    _ => "en"
                };
            }

            LanguageType mappedLang = (LanguageType)((int)lang - 1);
            return new LocalizedFontSetting { language = mappedLang }.GetLanguageCode();
        }

        public void LoadLocalizationCsv()
        {
            m_localizedData.Clear();

            string fullPath = Path.Combine(Application.dataPath, csvPath.Replace("Assets/", ""));
            if (!File.Exists(fullPath))
            {
                // Debug.LogWarning($"找不到 CSV 文件: {fullPath}");
                return;
            }

            string[] lines;
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                var list = new List<string>();
                while (!reader.EndOfStream)
                {
                    list.Add(reader.ReadLine());
                }
                lines = list.ToArray();
            }

            if (lines.Length < 2)
            {
                // Debug.LogWarning("CSV 文件内容不足");
                return;
            }

            string[] headers = lines[0].Split(',');
            Dictionary<string, int> langColIndex = new();

            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim();
                if (header.Contains("zh-CN")) langColIndex["zh-CN"] = i;
                else if (header.Contains("zh-TW")) langColIndex["zh-TW"] = i;
                else if (header.Contains("en")) langColIndex["en"] = i;
                else if (header.Contains("ja")) langColIndex["ja"] = i;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string[] cols = lines[i].Split(',');
                if (cols.Length < 2) continue;

                string key = cols[0].Trim().Trim('"');
                var entry = new Dictionary<string, string>();

                foreach (var lang in langColIndex)
                {
                    if (lang.Value < cols.Length)
                        entry[lang.Key] = cols[lang.Value].Trim().Trim('"');
                }

                m_localizedData[key] = entry;
            }
        }

        public void ApplyCurrentLanguageSettings()
        {
            string langCode = GetCurrentLanguageCode();

            LocalizedFontSetting currentSetting = m_languageSettings.Find(s => s.GetLanguageCode() == langCode);
            if (currentSetting == null)
            {
                // Debug.LogWarning($"未找到语言 {langCode} 的字体配置");
                return;
            }

            foreach (var target in m_managedTextList)
            {
                if (target.textGameObject == null || !target.applyCustomSetting || string.IsNullOrEmpty(target.localizedKey))
                    continue;

                string localizedText = null;

                if (m_localizedData.TryGetValue(target.localizedKey, out var langMap))
                {
                    if (langMap.TryGetValue(langCode, out var value))
                        localizedText = value;
                }

                var tmp = target.textGameObject.GetComponent<TMP_Text>();
                var uiText = target.textGameObject.GetComponent<Text>();

                if (tmp != null)
                {
                    tmp.text = localizedText ?? tmp.text;
                    //tmp.font = currentSetting.fontAsset;
                    tmp.characterSpacing = currentSetting.characterSpacing;
                    tmp.margin = currentSetting.margins;
                    tmp.fontSize = currentSetting.fontSize;
                    tmp.rectTransform.localScale = currentSetting.localScale;
                    target.groupName = tmp.text;
                }
                else if (uiText != null)
                {
                    uiText.text = localizedText ?? uiText.text;
                    uiText.fontSize = Mathf.RoundToInt(currentSetting.fontSize);
                    uiText.font = currentSetting.fontAsset;
                    target.textGameObject.transform.localScale = currentSetting.localScale;

                    var spacingComp = target.textGameObject.GetComponent<TestFont_TextLetterSpacing>();
                    if (spacingComp != null)
                    {
                        spacingComp.spacing = currentSetting.characterSpacing;
                        uiText.SetVerticesDirty();
                    }

                    target.groupName = uiText.text;
                }
            }
        }

        public void RefreshFontSettings()
        {
            LoadLocalizationCsv();
            ApplyCurrentLanguageSettings();
        }

        public void SwitchLanguage(int index)
        {
            if (index < 0 || index > 4)
            {
                // Debug.LogWarning("语言索引越界，只允许 0 ~ 4");
                return;
            }

            currentLanguage = (LanguageTypeWithDefault)index;
            s_currentLanguage = currentLanguage; // ✅ 同步静态字段
            ApplyCurrentLanguageSettings();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var item in m_managedTextList)
            {
                item?.TryUpdateGroupName();
            }

            // ✅ 编辑器模式预览语言并同步静态字段
            if (!Application.isPlaying)
            {
                s_currentLanguage = currentLanguage;
                LoadLocalizationCsv();
                ApplyCurrentLanguageSettings();
            }
        }
#endif

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.F1)) SwitchLanguage(1);
            if (Input.GetKeyUp(KeyCode.F2)) SwitchLanguage(2);
            if (Input.GetKeyUp(KeyCode.F3)) SwitchLanguage(3);
            if (Input.GetKeyUp(KeyCode.F4)) SwitchLanguage(4);
        }
    }
}
