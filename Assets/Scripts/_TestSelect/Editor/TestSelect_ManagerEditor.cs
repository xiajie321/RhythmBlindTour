using UnityEngine;
using UnityEditor;

namespace TestRevolver
{
    [CustomEditor(typeof(TestSelect_SceneManager))]
    public class TestSelect_ManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 先绘制默认的 Inspector
            DrawDefaultInspector();

            EditorGUILayout.HelpBox("注意：场景列表已弃用通过 SO 自动获取方式，请通过 TestSelect_Base 组件的 'm_manualSceneNameEnum' 手动设置场景枚举标签。", MessageType.Info);
        }
    }
}
