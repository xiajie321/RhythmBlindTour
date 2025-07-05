using QFramework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

namespace Qf.Systems
{
    /// <summary>
    /// 输入系统（通过统一的接口获取输入）
    /// </summary>
    public class InputSystems : AbstractSystem
    {
        public static bool Quit { get; protected set; }
        public static bool Sure { get; protected set; }
        public static bool SwipeUp { get; protected set; }
        public static bool SwipeDown { get; protected set; }
        public static bool SwipeLeft { get; protected set; }
        public static bool SwipeRight { get; protected set; }
        public static bool Click { get; protected set; }
        public static bool PlayClick { get; protected set; }

        public static float Horizontal { get; protected set; }
        public static float Vertical { get; protected set; }

        static Dictionary<string, List<KeyCode>> keyValuePairs = new();

        static Dictionary<string, bool> keyCacheThisFrame = new();
        static Dictionary<string, bool> keyCacheLastFrame = new();

        protected override void OnInit()
        {
            LoadInputMapFromJson(); // [从 JSON 文件加载键位映射] -- mixyao/25/07/02
            Managers.Managers.instance.AddUpdate(() => Pc());
        }

        // [替代 InputInit：从 JSON 文件初始化按键配置] -- mixyao/25/07/02
        void LoadInputMapFromJson()
        {
            string path = Path.Combine(Application.persistentDataPath, "KeyMap/inputMap.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning("未找到按键配置，使用默认配置");
                return;
            }

            var json = File.ReadAllText(path);
            var map = JsonUtility.FromJson<InputKeyMap>(json);

            foreach (var kv in map.KeyBindings)
            {
                var keyCodes = kv.Value.ConvertAll(i => (KeyCode)i);
                ClearKey(kv.Key);
                AddKey(kv.Key, keyCodes.ToArray());
            }
        }

        void Pc()
        {
            keyCacheLastFrame = new Dictionary<string, bool>(keyCacheThisFrame);
            keyCacheThisFrame.Clear();

            Quit = InputQuery("Quit");
            Sure = InputQuery("Sure");
            SwipeUp = InputQuery("SwipeUp");
            SwipeDown = InputQuery("SwipeDown");
            SwipeLeft = InputQuery("SwipeLeft");
            SwipeRight = InputQuery("SwipeRight");
            Click = InputQuery("Click");
            PlayClick = InputQuery("PlayClick");

            Horizontal = Input.GetAxis("Horizontal");
            Vertical = Input.GetAxis("Vertical");
        }

        public static bool InputQuery(string keyName, bool and = false)
        {
            if (!keyValuePairs.ContainsKey(keyName)) return false;

            if (keyCacheThisFrame.ContainsKey(keyName))
                return keyCacheThisFrame[keyName];

            bool result = false;

            var keys = keyValuePairs[keyName];
            if (keys == null || keys.Count == 0)
            {
                keyCacheThisFrame[keyName] = false;
                return false;
            }

            if (keys.Count == 1)
            {
                result = Input.GetKeyDown(keys[0]);
            }
            else
            {
                if (and)
                {
                    result = keys.All(Input.GetKey);
                }
                else
                {
                    foreach (var key in keys)
                    {
                        if (Input.GetKeyDown(key))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            keyCacheThisFrame[keyName] = result;
            return result;
        }

        public static bool WasPressedThisFrame(string keyName)
        {
            return keyCacheThisFrame.ContainsKey(keyName) && keyCacheThisFrame[keyName];
        }

        public static bool WasPressedLastFrame(string keyName)
        {
            return keyCacheLastFrame.ContainsKey(keyName) && keyCacheLastFrame[keyName];
        }

        // [新增：长按检测] -- mixyao/25/07/02
        public static bool IsKeyHeld(string keyName)
        {
            if (!keyValuePairs.ContainsKey(keyName)) return false;
            return keyValuePairs[keyName].Any(Input.GetKey);
        }

        // [新增：KeyUp 检测] -- mixyao/25/07/02
        public static bool WasReleasedThisFrame(string keyName)
        {
            return keyCacheLastFrame.ContainsKey(keyName) && keyCacheLastFrame[keyName]
                && (!keyCacheThisFrame.ContainsKey(keyName) || !keyCacheThisFrame[keyName]);
        }

        public static List<KeyCode> GetKeys(string keyName)
        {
            if (keyValuePairs.ContainsKey(keyName))
                return keyValuePairs[keyName];
            return null;
        }

        // [新增：返回所有注册的按键名] -- mixyao/25/07/02
        public static IEnumerable<string> GetAllKeyNames()
        {
            return keyValuePairs.Keys;
        }

        public static void AddKey(string keyName, params KeyCode[] keyCodes)
        {
            if (!keyValuePairs.ContainsKey(keyName))
            {
                keyValuePairs[keyName] = keyCodes.ToList();
            }
            else
            {
                foreach (var key in keyCodes)
                {
                    if (!keyValuePairs[keyName].Contains(key))
                        keyValuePairs[keyName].Add(key);
                    else
                        Debug.Log($"{key} 该按键已存在");
                }
            }
        }

        public static void RemoveKey(string keyName, KeyCode keyCode)
        {
            if (!keyValuePairs.ContainsKey(keyName))
            {
                Debug.LogError("操作不存在，请先使用 AddKey 创建");
                return;
            }
            keyValuePairs[keyName].Remove(keyCode);
        }

        public static void ClearKey(string keyName)
        {
            if (!keyValuePairs.ContainsKey(keyName))
            {
                return;
            }
            keyValuePairs[keyName].Clear();
        }
    }
}
