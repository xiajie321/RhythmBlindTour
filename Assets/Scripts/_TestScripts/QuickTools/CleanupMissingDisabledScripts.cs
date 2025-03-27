using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 使脚本在编辑器和运行时均生效（编辑器下每帧都会调用 Update）
[ExecuteAlways]
public class CleanupMissingDisabledScripts : MonoBehaviour
{
    [Tooltip("选中后，将自动遍历当前场景中的所有游戏物体，移除失效或未启用的脚本组件（包括缺失脚本）。")]
    public bool CleanUp = false;

    void Update()
    {
        // 仅在编辑器非播放状态下运行
        if (!Application.isPlaying && CleanUp)
        {
            int removedCount = CleanUpScripts();
#if UNITY_EDITOR
            Debug.Log($"清理完成，共移除 {removedCount} 个失效或禁用的脚本。");
#endif
            CleanUp = false;
        }
    }

    /// <summary>
    /// 遍历当前场景所有游戏物体，清理失效或禁用的脚本组件
    /// </summary>
    int CleanUpScripts()
    {
        int totalRemoved = 0;
        // 获取当前场景所有根物体
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            totalRemoved += CleanUpRecursively(root);
        }
        return totalRemoved;
    }

    /// <summary>
    /// 递归清理指定游戏物体及其子物体上失效或禁用的脚本组件
    /// </summary>
    int CleanUpRecursively(GameObject go)
    {
        int count = 0;
#if UNITY_EDITOR
        // 移除丢失脚本（缺失脚本组件的引用）；
        count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
#endif
        // 移除禁用的脚本（已挂载但未启用的 MonoBehaviour）
        MonoBehaviour[] monos = go.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour m in monos)
        {
            if (m != null && !m.enabled)
            {
                DestroyImmediate(m, true);
                count++;
            }
        }
        // 递归处理子物体
        foreach (Transform child in go.transform)
        {
            count += CleanUpRecursively(child.gameObject);
        }
        return count;
    }
}
