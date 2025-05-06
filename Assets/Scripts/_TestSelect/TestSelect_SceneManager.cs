using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.Rendering.Universal;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TestRevolver
{
    /// <summary>
    /// TestSelect_Manager：系统管理模块，用于管理异步场景加载等其他逻辑，
    /// 并提供修改关卡选择数据的接口。该脚本在切换场景时不会被销毁。
    /// </summary>
    public class TestSelect_SceneManager : MonoBehaviour
    {
        #region 单例模式

        private static TestSelect_SceneManager s_instance;
        public static TestSelect_SceneManager Instance
        {
            get { return s_instance; }
        }

        #endregion

        #region 异步场景加载事件

        public UnityEvent m_evPreSceneLoading;
        public UnityEvent<float> m_evDuringSceneLoading;
        public UnityEvent m_evPostSceneLoading;

        #endregion

        #region 公共变量

        /// <summary>
        /// 最低等待时间（秒），在 Inspector 中统一设置。
        /// </summary>
        public float m_fMinWaitTime = 0f;

        #endregion

        #region 公共方法

        public void _TR_AsyncLoadScene(string sceneName)
        {
            StartCoroutine(TestSelect_SaticAction.AsyncLoadSceneByName(sceneName, m_fMinWaitTime, m_evPreSceneLoading, m_evDuringSceneLoading, m_evPostSceneLoading));
        }

        public void _TR_AsyncLoadScene(int sceneIndex)
        {
            StartCoroutine(TestSelect_SaticAction.AsyncLoadSceneByIndex(sceneIndex, m_fMinWaitTime, m_evPreSceneLoading, m_evDuringSceneLoading, m_evPostSceneLoading));
        }

        /// <summary>
        /// 测试方法：打印出 Layer、Number、LevelID 以及场景切换覆盖状态和覆盖的场景名称
        /// </summary>
        public void _TR_Test()
        {

        }


        #endregion
    }
}
