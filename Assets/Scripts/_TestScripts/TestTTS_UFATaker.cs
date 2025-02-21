using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;         // DOTween 命名空间
using QFramework;

/// <summary>
/// 暂时使用的测试脚本，会弃用。
/// </summary>
public class TestTTS_UFATaker : MonoBehaviour, ISelectHandler
{
    // 缓存本物体上的 UIFileAttribute 组件
    private UIFileAttribute _uiFileAttribute;

    private void Awake()
    {
        _uiFileAttribute = GetComponent<UIFileAttribute>();
        if (_uiFileAttribute == null)
        {
            ($"{gameObject.name} 上未找到 UIFileAttribute 组件！").TTSLog(Color.red);
        }
    }

    // 当该物体被选中时（通过键盘或鼠标获得焦点）调用
    public void OnSelect(BaseEventData eventData)
    {
        if (_uiFileAttribute != null)
        {
            // 调用 SelectManager.SetAttribute 并传入 UIFileAttribute 组件
            SelectManager.SetAttribute(_uiFileAttribute);
        }

        // 如果本物体上存在 UIEventsItem 组件，则执行缩放动画
        if (GetComponent<UIEventsItem>() != null)
        {
            transform.DOScale(new Vector3(1.1f, 1.1f, 1.1f), 0.1f)
                     .SetEase(Ease.Linear)
                     .OnComplete(() =>
                     {
                         transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.Linear);
                     });
        }
    }
}
