using UnityEngine;
using UnityEngine.UI;
using Qf.Events;
using Qf.Managers;
using QFramework;
using System.Collections;

public class mModeButtonsController : MonoBehaviour, IController
{
    [Header("模式按钮图像（仅 Image，不使用 Button）")]
    public Image[] buttonImages;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    private int currentSelectedIndex = -1;

    void Start()
    {
        StartCoroutine(DelayedSetSelectedMode());
    }

    private IEnumerator DelayedSetSelectedMode()
    {
        yield return null; // 等待一帧
        SetSelectedMode(0); // 延迟一帧后执行
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            int nextIndex = (currentSelectedIndex + 1) % buttonImages.Length;
            SetSelectedMode(nextIndex);
        }
    }


    /// <summary>
    /// 设置当前选中索引，并更新按钮颜色，同时触发逻辑事件
    /// </summary>
    public void SetSelectedMode(int index)
    {
        currentSelectedIndex = index;
        UpdateButtonColors();

        var uiItem = GetUIEventsItem(index);
        if (uiItem != null)
        {
            uiItem.TriggerClick();
            Debug.Log($"[Mode] 触发 UIEventsItem.Click → {uiItem.name}");
        }
    }

    /// <summary>
    /// 仅触发当前选中按钮的 UIEventsItem 点击事件（不切换视觉状态）
    /// </summary>
    public void TriggerCurrentSelected()
    {
        var uiItem = GetUIEventsItem(currentSelectedIndex);
        if (uiItem != null)
        {
            uiItem.TriggerClick();
            Debug.Log($"[Mode] Tab 激活当前模式按钮 → {uiItem.name}");
        }
    }
    /// <summary>
    /// 根据 currentSelectedIndex 更新所有按钮颜色
    /// </summary>
    private void UpdateButtonColors()
    {
        for (int i = 0; i < buttonImages.Length; i++)
        {
            buttonImages[i].color = (i == currentSelectedIndex) ? selectedColor : normalColor;
            Debug.Log(buttonImages[i].color);

        }
    }

    public void UpdateButtonColors(int index)
    {
        for (int i = 0; i < buttonImages.Length; i++)
        {
            buttonImages[i].color = (i == index) ? selectedColor : normalColor;
        }
    }


    /// <summary>
    /// 获取指定索引下的 UIEventsItem 组件
    /// </summary>
    private UIEventsItem GetUIEventsItem(int index)
    {
        if (index >= 0 && index < buttonImages.Length)
            return buttonImages[index].GetComponent<UIEventsItem>();
        return null;
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
