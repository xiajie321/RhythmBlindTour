using UnityEngine;
using UnityEngine.SceneManagement;
using Views.UIManager.UIPanels;

public class Main : MonoBehaviour
{
    //入口
    void Start()
    {
        Views.UIManager.UIManager.Instance.ShowPanel<MainPanel>();
        SceneManager.LoadSceneAsync("Level One",LoadSceneMode.Additive);
    }
    
}
