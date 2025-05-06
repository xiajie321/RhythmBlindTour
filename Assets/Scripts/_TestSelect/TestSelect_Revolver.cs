using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace TestRevolver
{
    public class TestSelect_Revolver : TestSelect_Base
    {
        private int m_iUILayerIndex = 0;
        public UnityEvent m_evOnCancelCommon;
        public UnityEvent m_evOnMoveLeft;
        public UnityEvent m_evOnMoveRight;
        public UnityEvent m_evOnConfirmCommon;
        public UnityEvent m_evOnExitCommon;
        private bool m_bEventsInitialized = false;
        public int m_selectedSceneIndex = 0;
        public int m_bulletCurrentIndex = 0;
        public string[] SceneNameEnum
        {
            get
            {
                if (m_manualSceneNameEnum == null || m_manualSceneNameEnum.Length == 0)
                    return new[] { "None" };
                return m_manualSceneNameEnum;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            TestSelect_SaticAction.StaticData.SetSceneNameEnum(m_manualSceneNameEnum);
            EditorApplication.delayCall += () =>
            {
                if (!Application.isPlaying)
                    InternalEditorUtility.RepaintAllViews();
            };
        }
#endif

        private void Start()
        {
            InitializeMagazine();
        }

        /// <summary>
        /// 初始化 m_Magazine 里每个 Bullet 的事件订阅（只执行一次）
        /// </summary>
        public void InitializeMagazine()
        {
            if (m_bEventsInitialized) return;
            m_bEventsInitialized = true;

            if (m_Magazine == null || m_Magazine.Length == 0) return;
            m_iUILayerIndex = 0;
            for (int i = 0; i < m_Magazine.Length; i++)
            {
                var bullet = m_Magazine[i];
                bullet.m_evOnEnter.RemoveListener(_DefaultEnterCallback);
                bullet.m_evOnEnter.AddListener(_DefaultEnterCallback);
                bullet.m_evOnConfirm.RemoveListener(_InvokeConfirmCommon);
                bullet.m_evOnConfirm.AddListener(_InvokeConfirmCommon);
                bullet.m_evOnCancel.RemoveListener(_InvokeCancelCommon);
                bullet.m_evOnCancel.AddListener(_InvokeCancelCommon);

                // 不在这里给 Button 绑定点击，UI 层统一触发 Confirm 动作
                var animBtn = bullet.m_Button?.GetComponent<TestSelectAnim_Button>();
                if (animBtn != null)
                {
                    bullet.m_evOnEnter.AddListener(() =>
                    {
                        animBtn._TB_ApplySelectState();
                    });
                    bullet.m_evOnExit.AddListener(() =>
                    {
                        animBtn._TB_RestoreSelectState();
                    });
                }
            }

            m_bulletCurrentIndex = 0;
            // 触发默认选中
            m_Magazine[m_iUILayerIndex].m_evOnEnter?.Invoke();
        }

        public void _TR_MoveSelection(bool isRight)
        {
            _MoveSelection(isRight);
        }

        public void _TR_ConfirmSelection()
        {
            _ConfirmSelection();
        }

        public void _TR_BackAction()
        {
            _BackAction();
        }

        public void _TR_Test()
        {
            // Debug.Log("Test 方法已执行。");
        }

        // 以下方法保留不变
        public void _TR_SetLayer(string newLayer)
        {
            if (!isActiveAndEnabled) return;
            TestSelect_SaticAction.StaticData.SetLayer(newLayer);
        }

        public void _TR_SetGameLevelNumber(string newGameLevelNumber)
        {
            if (!isActiveAndEnabled) return;
            if (int.TryParse(newGameLevelNumber, out var num))
                TestSelect_SaticAction.StaticData.SetGameLevelNumber(num);
        }

        public void _TR_UpdateUniqueLevelIDText(Text targetText)
        {
            if (!isActiveAndEnabled) return;
            targetText.text = TestSelect_SaticAction.StaticData.s_strUniqueLevelID;
        }

        public void _TR_LoadSceneAsyncByName(string sceneName)
        {
            if (!isActiveAndEnabled) return;
            StartCoroutine(TestSelect_SaticAction.AsyncLoadSceneByName(sceneName));
        }

        public void _TR_LoadSceneAsyncByIndex(int sceneIndex)
        {
            if (!isActiveAndEnabled) return;
            StartCoroutine(TestSelect_SaticAction.AsyncLoadSceneByIndex(sceneIndex));
        }

        public void _TR_LoadSceneAsync()
        {
            if (!isActiveAndEnabled) return;
            var scenes = SceneNameEnum;
            if (scenes.Length > 0)
            {
                m_selectedSceneIndex = Mathf.Clamp(m_selectedSceneIndex, 0, scenes.Length - 1);
                var sceneName = scenes[m_selectedSceneIndex];
                if (TestSelect_SceneManager.Instance != null)
                    TestSelect_SceneManager.Instance._TR_AsyncLoadScene(sceneName);
                else{}
                    // Debug.LogWarning("Manager 实例不存在！");
            }
            else
            {
                // Debug.LogWarning("场景名称列表为空，请在 Revolver 中设置场景标签。");
            }
        }

        private void _MoveSelection(bool isRight)
        {
            if (m_Magazine == null || m_Magazine.Length == 0) return;
            m_Magazine[m_iUILayerIndex].m_evOnExit?.Invoke();
            m_iUILayerIndex = isRight
                ? (m_iUILayerIndex + 1) % m_Magazine.Length
                : (m_iUILayerIndex - 1 + m_Magazine.Length) % m_Magazine.Length;
            if (isRight) m_evOnMoveRight?.Invoke(); else m_evOnMoveLeft?.Invoke();
            m_Magazine[m_iUILayerIndex].m_evOnEnter?.Invoke();
        }

        private void _ConfirmSelection()
        {
            var b = m_Magazine[m_iUILayerIndex];
            if (!string.IsNullOrEmpty(b.m_levelLayer))
            {
                TestSelect_SaticAction.StaticData.SetLayer(b.m_levelLayer);
                if (!b.m_levelLayer.Contains("_") && !string.IsNullOrEmpty(b.m_levelNumber))
                    TestSelect_SaticAction.StaticData.SetGameLevelNumber(int.Parse(b.m_levelNumber));
                else
                    TestSelect_SaticAction.StaticData.SetGameLevelNumber(0);
            }
            else if (!string.IsNullOrEmpty(b.m_levelNumber))
            {
                TestSelect_SaticAction.StaticData.SetGameLevelNumber(int.Parse(b.m_levelNumber));
            }
            b.m_evOnConfirm?.Invoke();
        }

        private void _BackAction()
        {
            if (m_Magazine != null && m_Magazine.Length > 0)
                m_Magazine[m_iUILayerIndex].m_evOnCancel?.Invoke();
        }

        private void _DefaultEnterCallback()
        {
            if (!isActiveAndEnabled) return;
            TestSelect_SaticAction.StaticData.s_iCurrentSelectedOption = m_iUILayerIndex;
            var b = m_Magazine[m_iUILayerIndex];
            if (b.m_Button != null) b.m_Button.Select();
            if (b.m_bApplySceneSwitch)
            {
                var scenes = SceneNameEnum;
                TestSelect_SaticAction.StaticData.s_overrideSceneName =
                    scenes[Mathf.Clamp(b.m_sceneListIndex, 0, scenes.Length - 1)];
                TestSelect_SaticAction.StaticData.s_bOverrideSceneSwitch = true;
            }
            else
            {
                TestSelect_SaticAction.StaticData.s_bOverrideSceneSwitch = false;
            }
        }

        private void _InvokeConfirmCommon()
        {
            if (!isActiveAndEnabled) return;
            m_evOnConfirmCommon?.Invoke();
        }

        private void _InvokeCancelCommon()
        {
            if (!isActiveAndEnabled) return;
            m_evOnCancelCommon?.Invoke();
        }

        public void _TR_Exit()
        {
            _InvokeExitCommon();
        }

        private void _InvokeExitCommon()
        {
            if (!isActiveAndEnabled) return;
            m_evOnExitCommon?.Invoke();
        }
    }
}
