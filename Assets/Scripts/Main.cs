using UnityEngine;
using Views.UIManager.UIPanels;

public class Main : MonoBehaviour
{
    //入口
    void Start()
    {
        Views.UIManager.UIManager.Instance.ShowPanel<MainPanel>();
    }
}
