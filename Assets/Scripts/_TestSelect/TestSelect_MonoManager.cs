using UnityEngine;
using UnityEngine.Events;

namespace TestRevolver
{
    /// <summary>
    /// TestSelect_MonoManager 用于管理列表选择和层级切换功能。
    /// </summary>
    public class TestSelect_MonoManager : MonoBehaviour
    {
        /// <summary>
        /// 定义当前处于哪个层级。
        /// </summary>
        public enum ESelectLayer
        {
            UILayer,    // UI层级：例如主界面、场景选择、关卡选择面板
            FuncLayer   // 功能层级：例如面板内的具体按钮选项（如“第一关”、“第二关”）
        }

        /// <summary>
        /// TestSelectItem 用于保存每个选项的数据和回调事件。
        /// </summary>
        [System.Serializable]
        public class TestSelectItem
        {
            /// <summary>
            /// 选项所属组名称。
            /// </summary>
            public string m_strGroupName;

            /// <summary>
            /// 当进入该选项时调用的 UnityEvent 回调。
            /// </summary>
            public UnityEvent m_evOnEnter;

            /// <summary>
            /// 当离开该选项时调用的 UnityEvent 回调。
            /// </summary>
            public UnityEvent m_evOnExit;

            /// <summary>
            /// 当确认该选项时调用的 UnityEvent 回调。
            /// </summary>
            public UnityEvent m_evOnConfirm;
        }

        #region Variables

        /// <summary>
        /// UI层级的选项数组（例如主界面、场景选择面板）。
        /// </summary>
        public TestSelectItem[] m_arrUILayerItems;

        /// <summary>
        /// 功能层级的选项数组（例如面板内的各个按钮）。
        /// </summary>
        public TestSelectItem[] m_arrFuncLayerItems;

        /// <summary>
        /// 当前活动的层级（默认从 UI 层级开始）。
        /// </summary>
        private ESelectLayer m_eCurrentLayer = ESelectLayer.UILayer;

        /// <summary>
        /// UI层级当前选中的索引。
        /// </summary>
        private int m_iUILayerIndex = 0;

        /// <summary>
        /// 功能层级当前选中的索引。
        /// </summary>
        private int m_iFuncLayerIndex = 0;

        #endregion

        #region Unity Methods

        /// <summary>
        /// Unity 的 Start 方法，用于初始化默认选中项。
        /// </summary>
        private void Start()
        {
            if (m_arrUILayerItems != null && m_arrUILayerItems.Length > 0)
            {
                m_iUILayerIndex = 0;
                // 调用初始选项的进入回调
                m_arrUILayerItems[m_iUILayerIndex].m_evOnEnter?.Invoke();
            }
        }

        /// <summary>
        /// Unity 的 Update 方法，监听按键输入控制选项切换和确认。
        /// </summary>
        private void Update()
        {
            // 检测左右切换，A 键向左，D 键向右
            if (Input.GetKeyDown(KeyCode.A))
            {
                _TR_MoveSelection(false);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                _TR_MoveSelection(true);
            }

            // 检测确认选项，空格键触发确认事件
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _TR_ConfirmSelection();
            }

            // 检测返回操作（从功能层返回 UI 层），例如按下 Escape 键
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _TR_BackAction();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 公共方法：通过左右移动来切换选项。
        /// isRight 为 true 表示向右移动，false 表示向左移动。
        /// </summary>
        /// <param name="isRight">方向参数</param>
        public void _TR_MoveSelection(bool isRight)
        {
            _MoveSelection(isRight);
        }

        /// <summary>
        /// 公共方法：确认当前选项（调用确认 UnityEvent）。
        /// </summary>
        public void _TR_ConfirmSelection()
        {
            _ConfirmSelection();
        }

        /// <summary>
        /// 公共方法：返回上一级（例如从功能层返回 UI 层）。
        /// </summary>
        public void _TR_BackAction()
        {
            _BackAction();
        }

        /// <summary>
        /// 公共测试方法，前缀 _TR_ ，用于调用内部的测试逻辑。
        /// </summary>
        public void _TR_Test()
        {
            _Test();
        }

        #endregion

        #region Private Methods

        // #redgin ------ 选择交替模块 ------

        /// <summary>
        /// 内部方法：根据当前层级和方向参数移动选中的选项。
        /// </summary>
        /// <param name="isRight">为 true 则向右移动，否则向左移动</param>
        private void _MoveSelection(bool isRight)
        {
            switch (m_eCurrentLayer)
            {
                case ESelectLayer.UILayer:
                    {
                        // 调用当前选项的离开回调
                        m_arrUILayerItems[m_iUILayerIndex].m_evOnExit?.Invoke();

                        // 计算新的索引（循环切换）
                        if (isRight)
                        {
                            m_iUILayerIndex = (m_iUILayerIndex + 1) % m_arrUILayerItems.Length;
                        }
                        else
                        {
                            m_iUILayerIndex = (m_iUILayerIndex - 1 + m_arrUILayerItems.Length) % m_arrUILayerItems.Length;
                        }
                        // 调用新选项的进入回调
                        m_arrUILayerItems[m_iUILayerIndex].m_evOnEnter?.Invoke();
                        break;
                    }
                case ESelectLayer.FuncLayer:
                    {
                        // 调用当前功能层选项的离开回调
                        m_arrFuncLayerItems[m_iFuncLayerIndex].m_evOnExit?.Invoke();

                        // 计算新的索引（循环切换）
                        if (isRight)
                        {
                            m_iFuncLayerIndex = (m_iFuncLayerIndex + 1) % m_arrFuncLayerItems.Length;
                        }
                        else
                        {
                            m_iFuncLayerIndex = (m_iFuncLayerIndex - 1 + m_arrFuncLayerItems.Length) % m_arrFuncLayerItems.Length;
                        }
                        // 调用新功能层选项的进入回调
                        m_arrFuncLayerItems[m_iFuncLayerIndex].m_evOnEnter?.Invoke();
                        break;
                    }
            }
        }

        // #redgin ------ 返回系统层模块 ------

        /// <summary>
        /// 内部方法：根据当前层级确认选中的选项。
        /// </summary>
        private void _ConfirmSelection()
        {
            switch (m_eCurrentLayer)
            {
                case ESelectLayer.UILayer:
                    {
                        // 调用当前 UI 层选项的确认回调
                        m_arrUILayerItems[m_iUILayerIndex].m_evOnConfirm?.Invoke();
                        // 示例逻辑：如果存在功能层选项，则切换到功能层，并默认选中第一个
                        if (m_arrFuncLayerItems != null && m_arrFuncLayerItems.Length > 0)
                        {
                            m_eCurrentLayer = ESelectLayer.FuncLayer;
                            m_iFuncLayerIndex = 0;
                            m_arrFuncLayerItems[m_iFuncLayerIndex].m_evOnEnter?.Invoke();
                        }
                        break;
                    }
                case ESelectLayer.FuncLayer:
                    {
                        // 调用当前功能层选项的确认回调
                        m_arrFuncLayerItems[m_iFuncLayerIndex].m_evOnConfirm?.Invoke();
                        break;
                    }
            }
        }

        /// <summary>
        /// 内部方法：执行返回操作，从功能层返回到 UI 层。
        /// </summary>
        private void _BackAction()
        {
            if (m_eCurrentLayer == ESelectLayer.FuncLayer)
            {
                // 调用当前功能层选项的离开回调
                m_arrFuncLayerItems[m_iFuncLayerIndex].m_evOnExit?.Invoke();
                // 切换回 UI 层
                m_eCurrentLayer = ESelectLayer.UILayer;
                // 重新调用 UI 层当前选项的进入回调，确认返回后状态
                m_arrUILayerItems[m_iUILayerIndex].m_evOnEnter?.Invoke();
            }
        }

        /// <summary>
        /// 内部测试方法，用于验证调用链是否正确。
        /// </summary>
        private void _Test()
        {
            // Debug.Log("Test 方法已执行。");
        }

        #endregion
    }
}
