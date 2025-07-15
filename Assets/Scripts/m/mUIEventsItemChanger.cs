using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class mUIEventsItemChanger : MonoBehaviour
{
    [Header("UI控件")]
    public UIEventsItem leftItem;
    public UIEventsItem rightItem;

    [Header("事件组列表")]
    public List<UIEventPair> eventPairs = new();

    void Start()
    {
        SetEventGroup(0);
    }

    /// <summary>
    /// 设置第 idx 组事件为当前UI的事件
    /// </summary>
    public void SetEventGroup(int idx)
    {
        if (eventPairs == null || eventPairs.Count <= idx)
        {
            Debug.LogWarning($"eventPairs[{idx}] 不存在！");
            return;
        }

        var pair = eventPairs[idx];

        // 清空旧绑定
        leftItem.clickevent.RemoveAllListeners();
        rightItem.clickevent.RemoveAllListeners();

        // 将pair的事件"搬运"到对应的UIEventsItem
        if (pair.leftEvent != null)
            leftItem.clickevent = CloneUnityEvent(pair.leftEvent);
        if (pair.rightEvent != null)
            rightItem.clickevent = CloneUnityEvent(pair.rightEvent);
    }

    // UnityEvent不能直接赋值，否则inspector引用丢失，可以这样做：
    UnityEvent CloneUnityEvent(UnityEvent src)
    {
        UnityEvent newEvt = new UnityEvent();
        var count = src.GetPersistentEventCount();
        for (int i = 0; i < count; ++i)
        {
            var target = src.GetPersistentTarget(i);
            var method = src.GetPersistentMethodName(i);
            if (!string.IsNullOrEmpty(method))
                newEvt.AddListener(UnityEngine.Events.UnityAction.CreateDelegate(typeof(UnityAction), target, method) as UnityAction);
        }
        return newEvt;
    }
}
[System.Serializable]
public class UIEventPair
{
    public UnityEvent leftEvent = new UnityEvent();
    public UnityEvent rightEvent = new UnityEvent();
}
