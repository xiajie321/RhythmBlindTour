#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(Text))]
[AddComponentMenu("mTools/mTextNameSync")]
public class mTextNameSync : MonoBehaviour
{
    [Tooltip("前缀文本")]
    public string prefix = "说明文本 - ";

    private Text _text;
    private string _lastText;

    private void OnEnable()
    {
        _text ??= GetComponent<Text>();
        EditorApplication.update -= EditorTick;
        EditorApplication.update += EditorTick;
        ApplyName(true);
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorTick;
    }

    // Inspector 修改等会触发
    private void OnValidate()
    {
        _text ??= GetComponent<Text>();
        ApplyName(true);
    }

    private void EditorTick()
    {
        // 只在编辑器非运行状态下处理
        if (Application.isPlaying) return;
        if (_text == null) { _text = GetComponent<Text>(); return; }

        if (_text.text != _lastText)
            ApplyName();
    }

    private void ApplyName(bool force = false)
    {
        if (_text == null) return;

        string t = _text.text ?? string.Empty;
        string newName = (prefix ?? string.Empty) + t;

        if (force || name != newName)
        {
            // 记录撤销 & 改名 & 标记脏
            Undo.RecordObject(gameObject, "Rename by mTextNameSync_EditorOnly");
            name = newName;
            _lastText = t;
            EditorUtility.SetDirty(gameObject);
        }
    }
}
#endif
