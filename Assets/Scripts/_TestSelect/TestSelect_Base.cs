using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TestRevolver
{
    /// <summary>
    /// 选择系统基础类
    /// 存储弹匣数据（Bullet 数组）并定义 Bullet 结构，
    /// 供选择系统的实现类继承使用。
    /// </summary>
    public class TestSelect_Base : MonoBehaviour
    {
        /// <summary>
        /// 手动设置场景名称的枚举标签列表。
        /// 请在 Inspector 中填入需要的场景名称，而不是通过 SO 自动获取。
        /// </summary>
        [Tooltip("请手动设置场景名称的枚举标签列表，弃用通过SO自动获取。")]
        public string[] m_manualSceneNameEnum = new string[0];

        /// <summary>
        /// 返回手动设置的场景名称枚举列表。
        /// </summary>
        public string[] SceneNameEnum
        {
            get { return m_manualSceneNameEnum; }
        }

        /// <summary>
        /// Bullet 类：用于存储每个选项的数据和回调事件。
        /// </summary>
        [System.Serializable]
        public class Bullet
        {
            public string m_strGroupName;
            public UnityEvent m_evOnEnter;
            public UnityEvent m_evOnExit;
            public UnityEvent m_evOnConfirm;
            public UnityEvent m_evOnCancel;
            public string m_levelLayer;
            public string m_levelNumber;
            public Button m_Button;
            public LevelSetMode m_LevelSetMode = LevelSetMode.None;

            // 用于自动订阅 Button-OnSelected 事件
            public bool m_bAutoSubscribeOnSelected = false;
            public UnityEvent m_evOnSelected;
            [System.NonSerialized]
            public bool m_bOnSelectedSubscribed = false;

            // 以下场景加载回调设置（由 Common 管理）仍保留
            public float m_minWaitTime = 0f;
            public UnityEvent m_preSceneLoading;
            public UnityEvent<float> m_duringSceneLoading;
            public UnityEvent m_postSceneLoading;
            public bool m_bApplySceneSwitchSettings = false;

            /// <summary>
            /// 【新增】Bullet 层级中独有的场景切换“应用开关”。
            /// 当为 true 时，在进入该 Bullet 时将优先采用 Bullet 中选中的场景。
            /// 当为 false 时，则采用 Common 的场景设置。
            /// </summary>
            public bool m_bApplySceneSwitch = false;

            /// <summary>
            /// 场景切换使用的场景列表索引，供 Bullet 局部使用（保留）。
            /// </summary>
            public int m_sceneListIndex = 0;
        }

        /// <summary>
        /// 弹匣：存储所有 Bullet 选项的数组。
        /// </summary>
        public Bullet[] m_Magazine;

        public enum LevelSetMode { None, Layer, Number }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                // 将手动设置的场景枚举列表同步到静态数据中，以便其它模块获取
                TestSelect_SaticAction.StaticData.SetSceneNameEnum(m_manualSceneNameEnum);
            }
        }
#endif
    }
}
