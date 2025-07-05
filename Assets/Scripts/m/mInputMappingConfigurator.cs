using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Qf.Systems;

public class mInputMappingConfigurator : MonoBehaviour
{
    public enum InputModeType { None, Edit, Play, UI }

    [Header("当前输入响应模式")]
    public InputModeType inputModeType = InputModeType.Edit;

    [Header("Edit 模式键位设置（每个操作一个键）")]
    public KeyCode mKey_Quit = KeyCode.Escape;
    public KeyCode mKey_Sure = KeyCode.Return;
    public KeyCode mKey_SwipeUp = KeyCode.UpArrow;
    public KeyCode mKey_SwipeDown = KeyCode.DownArrow;
    public KeyCode mKey_SwipeLeft = KeyCode.LeftArrow;
    public KeyCode mKey_SwipeRight = KeyCode.RightArrow;
    public KeyCode mKey_Click = KeyCode.Space;

    [Header("UI 输入映射列表")]
    public List<UIInputMapping> mUIInputMappings = new();

    [Header("Play 模式键位设置（每个操作多个键）")]
    public KeyCode[] pKeys_Quit = new KeyCode[] { KeyCode.Escape };
    public KeyCode[] pKeys_Sure = new KeyCode[] { KeyCode.Return, KeyCode.Space };
    public KeyCode[] pKeys_SwipeUp = new KeyCode[] { KeyCode.W, KeyCode.UpArrow };
    public KeyCode[] pKeys_SwipeDown = new KeyCode[] { KeyCode.S, KeyCode.DownArrow };
    public KeyCode[] pKeys_SwipeLeft = new KeyCode[] { KeyCode.A, KeyCode.LeftArrow };
    public KeyCode[] pKeys_SwipeRight = new KeyCode[] { KeyCode.D, KeyCode.RightArrow };
    public KeyCode[] pKeys_Click = new KeyCode[] { KeyCode.Space, KeyCode.Return };
    [Header("模式选择")]
    private InputModeType mLastAppliedMode = InputModeType.None;

    void Awake()
    {
        ApplyModeChange(inputModeType);
    }
    void Start()
    {
        SaveAllMapsToJson();
    }
    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            SaveAllMapsToJson();
        }

        if (inputModeType != mLastAppliedMode)
        {
            ApplyModeChange(inputModeType);
            mLastAppliedMode = inputModeType;
        }

        switch (inputModeType)
        {
            case InputModeType.UI:
                foreach (var map in mUIInputMappings)
                {
                    bool triggered = map.triggerType switch
                    {
                        InputTriggerType.Click => InputSystems.InputQuery(map.groupName),
                        InputTriggerType.Down => InputSystems.InputQuery(map.groupName),
                        InputTriggerType.Up => Input.GetKeyUp(map.keyCode),
                        _ => false
                    };

                    if (triggered)
                    {
                        map.targetItem?.TriggerClick();
                    }
                }
                break;

            case InputModeType.Edit:
            case InputModeType.Play:
                // 统一处理 Edit / Play 输入
                CheckStandardInputs();
                break;
        }
    }
    private void CheckStandardInputs()
    {
        if (InputSystems.InputQuery("Click"))
            Debug.Log("[Input] Click 被触发");

        if (InputSystems.InputQuery("Sure"))
            Debug.Log("[Input] Sure 被触发");

        if (InputSystems.InputQuery("Quit"))
            Debug.Log("[Input] Quit 被触发");

        if (InputSystems.InputQuery("SwipeUp"))
            Debug.Log("[Input] SwipeUp 被触发");

        if (InputSystems.InputQuery("SwipeDown"))
            Debug.Log("[Input] SwipeDown 被触发");

        if (InputSystems.InputQuery("SwipeLeft"))
            Debug.Log("[Input] SwipeLeft 被触发");

        if (InputSystems.InputQuery("SwipeRight"))
            Debug.Log("[Input] SwipeRight 被触发");
    }


    void ApplyModeChange(InputModeType mode)
    {
        InputSystems.ClearKey("PlayClick");

        if (mode == InputModeType.None)
        {
            foreach (var key in InputSystems.GetAllKeyNames().ToList())
                InputSystems.ClearKey(key);
        }
        else if (mode == InputModeType.Edit)
        {
            LoadEditMap();
        }
        else if (mode == InputModeType.Play)
        {
            LoadPlayMap();
        }
        else if (mode == InputModeType.UI)
        {
            RegisterAllUIInputMappings();
        }
    }

    void RegisterAllUIInputMappings()
    {
        foreach (var map in mUIInputMappings)
        {
            InputSystems.ClearKey(map.groupName);
            InputSystems.AddKey(map.groupName, map.keyCode);
        }
    }

    public void SwitchToNone()
    {
        inputModeType = InputModeType.None;
        Debug.Log("[Switch] 切换至 None 模式");
        PrintCurrentInputMap(InputModeType.None);
    }

    public void SwitchToEdit()
    {
        inputModeType = InputModeType.Edit;
        Debug.Log("[Switch] 切换至 Edit 模式");
        PrintCurrentInputMap(InputModeType.Edit);
    }

    public void SwitchToPlay()
    {
        inputModeType = InputModeType.Play;
        Debug.Log("[Switch] 切换至 Play 模式");
        PrintCurrentInputMap(InputModeType.Play);
    }

    public void SwitchToUI()
    {
        inputModeType = InputModeType.UI;
        Debug.Log("[Switch] 切换至 UI 模式");
        PrintCurrentInputMap(InputModeType.UI);
    }
    private void PrintCurrentInputMap(InputModeType mode)
    {
        string path = mode switch
        {
            InputModeType.Edit => RuntimeJsonPath_Edit,
            InputModeType.Play => RuntimeJsonPath_Play,
            InputModeType.UI => RuntimeJsonPath_UI,
            _ => "(无映射)"
        };

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[InputMap] 当前模式 {mode} 映射文件不存在: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        Debug.Log($"[InputMap] 当前模式: {mode}");
        Debug.Log($"[InputMap] 加载路径: {path}");

        if (mode == InputModeType.UI)
        {
            var wrapper = JsonUtility.FromJson<UIMapWrapper>(json);
            foreach (var mapping in wrapper.uiMappings)
            {
                Debug.Log($"[UIMap] group: {mapping.groupName}, key: {mapping.keyCode}, trigger: {mapping.triggerType}, target: {mapping.targetItem?.name}");
            }
        }
        else
        {
            var map = JsonUtility.FromJson<InputKeyMap>(json);
            foreach (var kv in map.KeyBindings)
            {
                string keys = string.Join(", ", kv.Value.ConvertAll(i => ((KeyCode)i).ToString()));
                Debug.Log($"[InputMap] 操作名: {kv.Key} → 按键: {keys}");
            }
        }
    }

    public void SaveEditMapToJson()
    {
        var dict = new Dictionary<string, List<int>>
    {
        { "Quit", new List<int> { (int)mKey_Quit } },
        { "Sure", new List<int> { (int)mKey_Sure } },
        { "SwipeUp", new List<int> { (int)mKey_SwipeUp } },
        { "SwipeDown", new List<int> { (int)mKey_SwipeDown } },
        { "SwipeLeft", new List<int> { (int)mKey_SwipeLeft } },
        { "SwipeRight", new List<int> { (int)mKey_SwipeRight } },
        { "Click", new List<int> { (int)mKey_Click } }
    };

        var map = new InputKeyMap { KeyBindings = dict }; // ✅ 通过 setter 设置 entries
        var json = JsonUtility.ToJson(map, true);
        string path = RuntimeJsonPath_Edit;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);

        Debug.Log($"[EditMap] 已保存至: {path}");
        Debug.Log($"[EditMap] 内容如下:\n{json}");
    }


    public void LoadEditMap()
    {
        if (!File.Exists(RuntimeJsonPath_Edit)) return;

        string json = File.ReadAllText(RuntimeJsonPath_Edit);
        var map = JsonUtility.FromJson<InputKeyMap>(json);
        foreach (var kv in map.KeyBindings)
        {
            var keys = kv.Value.ConvertAll(i => (KeyCode)i);
            InputSystems.ClearKey(kv.Key);
            InputSystems.AddKey(kv.Key, keys.ToArray());
        }
    }
    public void SavePlayMapToJson()
    {
        var dict = new Dictionary<string, List<int>>
    {
        { "Quit", pKeys_Quit.Select(k => (int)k).ToList() },
        { "Sure", pKeys_Sure.Select(k => (int)k).ToList() },
        { "SwipeUp", pKeys_SwipeUp.Select(k => (int)k).ToList() },
        { "SwipeDown", pKeys_SwipeDown.Select(k => (int)k).ToList() },
        { "SwipeLeft", pKeys_SwipeLeft.Select(k => (int)k).ToList() },
        { "SwipeRight", pKeys_SwipeRight.Select(k => (int)k).ToList() },
        { "Click", pKeys_Click.Select(k => (int)k).ToList() }
    };

        var map = new InputKeyMap { KeyBindings = dict };
        var json = JsonUtility.ToJson(map, true);
        string path = RuntimeJsonPath_Play;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);

        Debug.Log($"[PlayMap] 已保存至: {path}");
        Debug.Log($"[PlayMap] 内容如下:\n{json}");
    }



    public void LoadPlayMap()
    {
        if (!File.Exists(RuntimeJsonPath_Play)) return;

        string json = File.ReadAllText(RuntimeJsonPath_Play);
        var map = JsonUtility.FromJson<InputKeyMap>(json);
        foreach (var kv in map.KeyBindings)
        {
            var keys = kv.Value.ConvertAll(i => (KeyCode)i);
            InputSystems.ClearKey(kv.Key);
            InputSystems.AddKey(kv.Key, keys.ToArray());
        }
    }

    public void SaveAllMapsToJson()
    {
        SaveEditMapToJson();
        SavePlayMapToJson();


        // === 3. 保存 UIMap.json（UIInputMapping List）===
        var uiList = new UIMapWrapper();
        uiList.uiMappings = mUIInputMappings;
        File.WriteAllText(RuntimeJsonPath_UI, JsonUtility.ToJson(uiList, true), System.Text.Encoding.UTF8);

        Debug.Log("[SaveAllMapsToJson] 所有输入配置已保存至 JSON 文件。");
    }


    // === 文件路径 ===
    public static string RuntimeJsonPath_Edit =>
        Path.Combine(Application.persistentDataPath, "KeyMap/EditMap.json");

    public static string RuntimeJsonPath_Play =>
        Path.Combine(Application.persistentDataPath, "KeyMap/PlayMap.json");

    public static string RuntimeJsonPath_UI =>
        Path.Combine(Application.persistentDataPath, "KeyMap/UIMap.json");
}


[System.Serializable]
public struct UIInputMapping
{
    public string groupName;
    public KeyCode keyCode;
    public InputTriggerType triggerType;
    public UIEventsItem targetItem;
}

[System.Serializable]
public struct UIInputMappingSerializable
{
    public string groupName;
    public KeyCode keyCode;
    public InputTriggerType triggerType;
}

[System.Serializable]
public class UIInputMapWrapper
{
    public List<UIInputMappingSerializable> entries;
}

public enum InputTriggerType
{
    Click,
    Down,
    Up
}
[System.Serializable]
public class UIMapWrapper
{
    public List<UIInputMapping> uiMappings;
}
