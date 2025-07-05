using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制排序按钮行为，每个按钮可设置为 Index / Time / TypeToggle 三种类型。
/// 按下后调用 mUIDrumsInspectorPanel 的公开方法执行排序并刷新颜色。
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class mDrumsSortButtonController : MonoBehaviour
{
    public enum SortType { Index, Time, TypeToggle }
    public SortType sortType;

    public mUIDrumsInspectorPanel inspectorPanel;

    private Button btn;
    private Image image;

    void Start()
    {
        btn = GetComponent<Button>();
        image = GetComponent<Image>();

        if (btn != null)
        {
            btn.onClick.AddListener(OnSortClicked);
        }

        RefreshColor(); // 初始颜色
    }

    void OnSortClicked()
    {
        if (inspectorPanel == null) return;

        switch (sortType)
        {
            case SortType.Index:
                inspectorPanel.SortByIndex();
                break;
            case SortType.Time:
                inspectorPanel.SortByTime();
                break;
            case SortType.TypeToggle:
                inspectorPanel.ToggleTypeSort();
                break;
        }

        // 同步刷新所有按钮颜色
        foreach (var other in FindObjectsOfType<mDrumsSortButtonController>())
        {
            other.RefreshColor();
        }
    }

    public void RefreshColor()
    {
        if (image == null || inspectorPanel == null) return;

        bool isActive = sortType switch
        {
            SortType.Index => inspectorPanel.currentPrimaryMode == mUIDrumsInspectorPanel.SortMode.ByIndex,
            SortType.Time => inspectorPanel.currentPrimaryMode == mUIDrumsInspectorPanel.SortMode.ByTime,
            SortType.TypeToggle => inspectorPanel.typeSortEnabled,
            _ => false
        };

        image.color = isActive ? Color.green : Color.white;
    }
}
