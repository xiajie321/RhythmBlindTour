using UnityEngine;
using UnityEngine.SceneManagement;

public class TestSceneSwitcher : MonoBehaviour
{
    private static TestSceneSwitcher s_instance;

    private void Awake()
    {
        // 保证唯一实例并跨场景保留
        if (s_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // R键：重新加载当前场景
        if (Input.GetKeyUp(KeyCode.R))
        {
            ReloadCurrentScene();
        }

        // 数字键 0 - 9：加载指定场景（按 Build Index）
        for (int i = 0; i <= 9; i++)
        {
            KeyCode key = KeyCode.Alpha0 + i;
            if (Input.GetKeyUp(key))
            {
                TryLoadSceneByIndex(i);
            }
        }
    }

    private void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"[TestSceneSwitcher] Reloading Scene: {currentScene.name}");
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    private void TryLoadSceneByIndex(int index)
    {
        if (index < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"[TestSceneSwitcher] Loading Scene Index: {index}");
            SceneManager.LoadScene(index);
        }
        else
        {
            Debug.LogWarning($"[TestSceneSwitcher] No scene found at index {index}. Check Build Settings.");
        }
    }
}
