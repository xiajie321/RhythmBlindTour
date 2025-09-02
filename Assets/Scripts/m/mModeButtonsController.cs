using UnityEngine;
using UnityEngine.UI;
using Qf.Events;
using Qf.Managers;
using QFramework;
using System.Collections;
// 新增：拿模型需要
using Qf.Models.AudioEdit;

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

    /// <summary>
    /// 初始化时：等待所有依赖组件/模型就绪后再触发首个模式按钮点击
    /// </summary>
    private IEnumerator DelayedSetSelectedMode()
    {
        // 1) 等 QFramework 架构就绪
        yield return new WaitUntil(() => GameBody.Interface != null);

        // 2) 等 AudioEditModel 可用（可能需要多帧）
        AudioEditModel editModel = null;
        while (editModel == null)
        {
            try { editModel = this.GetModel<AudioEditModel>(); }
            catch { /* 下一帧重试 */ }
            yield return null;
        }

        // 3) 等检查面板出现
        mUIDrumsInspectorPanel inspector = null;
        while (inspector == null)
        {
            inspector = FindObjectOfType<mUIDrumsInspectorPanel>();
            yield return null;
        }

        // 4) 等检查面板必要引用就绪（避免 Item/Content 未赋值）
        yield return new WaitUntil(() => inspector.ItemPrefab != null && inspector.ContentRoot != null);

        // 5) 让面板自检并准备（可选，但更稳妥）
        inspector.EnsureReadyAndRefresh();

        // 6) 一切就绪后，再触发默认模式
        if (buttonImages != null && buttonImages.Length > 0)
        {
            SetSelectedMode(0);
        }
        else
        {
            Debug.LogWarning("[mModeButtonsController] 未配置任何按钮图像，跳过默认模式设置。");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && buttonImages != null && buttonImages.Length > 0)
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
        if (buttonImages == null || buttonImages.Length == 0) return;
        if (index < 0 || index >= buttonImages.Length) return;

        currentSelectedIndex = index;
        UpdateButtonColors();

        var uiItem = GetUIEventsItem(index);
        if (uiItem != null)
        {
            uiItem.TriggerClick();
            Debug.Log($"[Mode] 触发 UIEventsItem.Click → {uiItem.name}");
        }
        else
        {
            Debug.LogWarning($"[Mode] 索引 {index} 未找到 UIEventsItem 组件。");
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
        if (buttonImages == null) return;
        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (buttonImages[i] == null) continue;
            buttonImages[i].color = (i == currentSelectedIndex) ? selectedColor : normalColor;
        }
    }

    public void UpdateButtonColors(int index)
    {
        if (buttonImages == null) return;
        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (buttonImages[i] == null) continue;
            buttonImages[i].color = (i == index) ? selectedColor : normalColor;
        }
    }

    /// <summary>
    /// 获取指定索引下的 UIEventsItem 组件
    /// </summary>
    private UIEventsItem GetUIEventsItem(int index)
    {
        if (buttonImages == null) return null;
        if (index >= 0 && index < buttonImages.Length && buttonImages[index] != null)
            return buttonImages[index].GetComponent<UIEventsItem>();
        return null;
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
}
