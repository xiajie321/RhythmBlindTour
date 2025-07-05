// using UnityEditor.SceneManagement; // ✅ 这是关键
// using UnityEngine.SceneManagement;

// using UnityEngine;
// using UnityEditor;
// using UnityEngine.UI;
///
/// 功能脚本 打包不可携带
/// 
// public class SetAllNavigationToNone
// {
//     [MenuItem("Tools/输入/关闭所有Selectable导航")]
//     static void DisableAllSelectableNavigation()
//     {
//         int count = 0;
//         foreach (Selectable selectable in Object.FindObjectsOfType<Selectable>())
//         {
//             Navigation nav = selectable.navigation;
//             nav.mode = Navigation.Mode.None;
//             selectable.navigation = nav;
//             count++;
//         }

//         Debug.Log($"已设置 {count} 个 Selectable 的 Navigation 为 None");
//         EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); // 标记场景已修改
//     }
// }
