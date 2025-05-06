using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TestRevolver
{
    [ExecuteInEditMode]
    public class TestSelectAction_StageManager : MonoBehaviour
    {
        public enum StageMode { Mode0, Mode1, Mode2, Mode3, Mode4, Mode5 }

        [Header("Revolver 实例 (按模式顺序排列)")]
        [SerializeField] private List<TestSelect_Revolver> mRevolvers = new List<TestSelect_Revolver>();

        [Header("Stage 对象 (UI 面板等)")]
        [SerializeField] private List<GameObject> mStageObjects = new List<GameObject>();

        [Header("每个模式对应的激活索引 (格式：\"0,2,3\")")]
        [SerializeField] private List<string> mTestIndicesList = new List<string> { "", "", "", "", "", "" };

        [Header("AllConfirm 脚本（包含所有 Stage 的 ConfirmEvents）")]
        [SerializeField] private TestSelectAction_AllConfirm mAllConfirm;

        [Header("当前选中模式")]
        [SerializeField] private StageMode mSelectedMode = StageMode.Mode0;

        [Header("离开模式时触发的动作（按 Mode 顺序，Mode0 → index 0, Mode1 → index 1 ...）")]
        [SerializeField] private List<UnityEvent> modeActions = new List<UnityEvent>();

        private StageMode mPreviousMode = StageMode.Mode0;

        private TestSelect_Revolver currentRevolver;
        private TestSelect_Base.Bullet[] currentMagazine;
        private int currentIndex;

        private void OnValidate()
        {
            if (!Application.isPlaying)
                AutoPopulateRevolvers();

            SwitchToMode((int)mSelectedMode);
            // 编辑器下也同步上一模式索引
            mPreviousMode = mSelectedMode;
        }

        private void Awake()
        {
            AutoPopulateRevolvers();
            mPreviousMode = mSelectedMode;
            SwitchToMode((int)mSelectedMode);
        }

        private void OnEnable()
        {
            TestSelect_SaticAction.InputEvents.s_evNextOption.AddListener(OnNext);
            TestSelect_SaticAction.InputEvents.s_evPrevOption.AddListener(OnPrev);
            TestSelect_SaticAction.InputEvents.s_evConfirm.AddListener(OnConfirm);
            TestSelect_SaticAction.InputEvents.s_evBack.AddListener(OnBack);
            TestSelect_SaticAction.InputEvents.s_evExit.AddListener(OnExit);
        }

        private void OnDisable()
        {
            TestSelect_SaticAction.InputEvents.s_evNextOption.RemoveListener(OnNext);
            TestSelect_SaticAction.InputEvents.s_evPrevOption.RemoveListener(OnPrev);
            TestSelect_SaticAction.InputEvents.s_evConfirm.RemoveListener(OnConfirm);
            TestSelect_SaticAction.InputEvents.s_evBack.RemoveListener(OnBack);
            TestSelect_SaticAction.InputEvents.s_evExit.RemoveListener(OnExit);
        }

        private void Update()
        {
            if (Application.isPlaying && mSelectedMode != mPreviousMode)
            {
                SwitchToMode((int)mSelectedMode);
                mPreviousMode = mSelectedMode;
            }
        }

        private void AutoPopulateRevolvers()
        {
            if (mRevolvers == null) mRevolvers = new List<TestSelect_Revolver>();
            if (mRevolvers.Count == 0)
                mRevolvers.AddRange(FindObjectsOfType<TestSelect_Revolver>());
        }

        /// <summary>
        /// 外部调用切换模式
        /// </summary>
        public void _SA_SwitchToMode(int modeIndex) => SwitchToMode(modeIndex);

        /// <summary>
        /// 执行模式切换：先触发离开旧模式事件，再切换 UI、Revolver、Confirm 回调
        /// </summary>
        private void SwitchToMode(int modeIndex)
        {
            // 触发离开旧模式的动作
            MoveActionSwitch((int)mPreviousMode);

            // 验证模式范围
            if (modeIndex < 0 || modeIndex >= mTestIndicesList.Count)
            {
                // Debug.LogWarning($"[StageManager] 模式索引 {modeIndex} 超出范围");
                return;
            }

            // 切换 UI 面板显示
            var enabledSet = ParseIndices(mTestIndicesList[modeIndex]);
            for (int i = 0; i < mStageObjects.Count; i++)
            {
                var go = mStageObjects[i];
                if (go != null)
                    go.SetActive(enabledSet.Contains(i));
            }

            // 切换 Revolver 实例
            if (modeIndex < mRevolvers.Count && mRevolvers[modeIndex] != null)
            {
                currentRevolver = mRevolvers[modeIndex];
                TestSelect_SaticAction.StaticData.s_currentRevolver = currentRevolver;

                // 初始化弹夹
                currentRevolver.InitializeMagazine();
                currentMagazine = currentRevolver.m_Magazine;

                // 绑定当前模式的 Confirm 事件
                BindConfirmActions(modeIndex);

                if (currentMagazine != null && currentMagazine.Length > 0)
                {
                    currentIndex = Mathf.Clamp(
                        currentRevolver.m_bulletCurrentIndex,
                        0,
                        currentMagazine.Length - 1
                    );
                    // 恢复并高亮上次停留的子弹
                    currentMagazine[currentIndex].m_evOnEnter?.Invoke();
                }
                else
                {
                    currentIndex = 0;
                }

                // Debug.Log($"[StageManager] 切换到 Revolver '{currentRevolver.gameObject.name}' (Mode{modeIndex}), restored bulletIndex={currentIndex}");
            }
            else
            {
                // Debug.LogWarning($"[StageManager] mRevolvers 列表中未找到索引 {modeIndex}");
                currentRevolver = null;
                currentMagazine = null;
                TestSelect_SaticAction.StaticData.s_currentRevolver = null;
            }

            // 切换完成后，同步记录当前模式为上一模式
            mPreviousMode = mSelectedMode = (StageMode)modeIndex;
        }

        /// <summary>
        /// 离开某模式时触发对应的 modeActions 中的事件
        /// </summary>
        private void MoveActionSwitch(int modeIndex)
        {
            if (modeActions != null && modeIndex >= 0 && modeIndex < modeActions.Count)
                modeActions[modeIndex]?.Invoke();
        }

        /// <summary>
        /// 从 AllConfirm.eventGroups 获取当前模式的 ConfirmEvents 并按序号绑定到每个 Bullet.m_evOnConfirm
        /// </summary>
        private void BindConfirmActions(int modeIndex)
        {
            if (currentMagazine == null || mAllConfirm == null) return;

            var group = mAllConfirm.eventGroups.Find(g => g.stageMode == (StageMode)modeIndex);
            var confirmEvents = group != null ? group.confirmEvents : null;
            if (confirmEvents == null) return;

            for (int i = 0; i < currentMagazine.Length; i++)
            {
                var bullet = currentMagazine[i];
                bullet.m_evOnConfirm.RemoveAllListeners();

                if (i < confirmEvents.Count && confirmEvents[i] != null)
                {
                    UnityEvent evt = confirmEvents[i];
                    bullet.m_evOnConfirm.AddListener(() => evt.Invoke());
                }
            }
        }

        private void OnNext()
        {
            if (currentMagazine == null || currentMagazine.Length == 0) return;
            currentMagazine[currentIndex].m_evOnExit?.Invoke();
            currentIndex = (currentIndex + 1) % currentMagazine.Length;
            currentRevolver.m_bulletCurrentIndex = currentIndex;
            currentRevolver?.m_evOnMoveRight?.Invoke();
            currentMagazine[currentIndex].m_evOnEnter?.Invoke();
        }

        private void OnPrev()
        {
            if (currentMagazine == null || currentMagazine.Length == 0) return;
            currentMagazine[currentIndex].m_evOnExit?.Invoke();
            currentIndex = (currentIndex - 1 + currentMagazine.Length) % currentMagazine.Length;
            currentRevolver.m_bulletCurrentIndex = currentIndex;
            currentRevolver?.m_evOnMoveLeft?.Invoke();
            currentMagazine[currentIndex].m_evOnEnter?.Invoke();
        }

        private void OnConfirm()
        {
            if (currentMagazine == null || currentMagazine.Length == 0) return;
            currentMagazine[currentIndex].m_evOnConfirm?.Invoke();
            currentRevolver?.m_evOnConfirmCommon?.Invoke();
        }

        private void OnBack()
        {
            if (currentMagazine == null || currentMagazine.Length == 0) return;
            currentMagazine[currentIndex].m_evOnCancel?.Invoke();
            currentRevolver?.m_evOnCancelCommon?.Invoke();
        }

        private void OnExit()
        {
            currentRevolver?.m_evOnExitCommon?.Invoke();
        }

        private HashSet<int> ParseIndices(string input)
        {
            var set = new HashSet<int>();
            if (string.IsNullOrEmpty(input)) return set;
            foreach (var part in input.Split(','))
            {
                if (int.TryParse(part.Trim(), out int idx) && idx >= 0 && idx < mStageObjects.Count)
                    set.Add(idx);
                else{}
                    // Debug.LogWarning($"[StageManager] 无法解析索引 '{part}'")
                    
            }
            return set;
        }
    }
}
