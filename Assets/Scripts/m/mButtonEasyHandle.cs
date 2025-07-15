using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 简单的Button封装：可从Inspector或代码调用InvokeClick（等效于手动点击），并支持AddListener添加响应
/// </summary>
[RequireComponent(typeof(Button))]
public class mButtonEasyHandle : MonoBehaviour
{
    private Button mButton;

    void Awake()
    {
        mButton = GetComponent<Button>();
        if (mButton == null)
        {
            Debug.LogError("[mButtonEasyHandle] 未找到Button组件！");
        }
    }

    /// <summary>
    /// 代码调用此方法，相当于点击了按钮，会触发所有OnClick/监听器
    /// </summary>
    public void InvokeClick()
    {
        if (mButton != null)
        {
            mButton.onClick.Invoke();
        }
    }

    /// <summary>
    /// 在运行时为Button添加额外的监听器
    /// </summary>
    public void AddListener(UnityAction action)
    {
        if (mButton != null && action != null)
        {
            mButton.onClick.AddListener(action);
        }
    }
}
