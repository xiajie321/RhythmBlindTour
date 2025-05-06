using UnityEngine;
using UnityEngine.Events;

namespace TestRevolver
{
    [System.Serializable]
    public class StringEvent : UnityEvent<string> { }

    /// <summary>
    /// TestSelectAnim_AnimActionMono：用于动画帧事件回调的组件，支持播放动画状态（支持Replay/Change控制）
    /// </summary>
    public class TestSelectAnim_AnimActionMono : MonoBehaviour
    {
        [Header("动画事件回调")]
        [Tooltip("动画开始帧事件回调")]
        [SerializeField] public UnityEvent onAnimEnter;

        [Tooltip("动画结束帧事件回调")]
        [SerializeField] public UnityEvent onAnimExit;

        [Tooltip("动画开始停留期间每帧调用的事件")]
        [SerializeField] public UnityEvent onAnimStartStay;

        [Tooltip("动画结束停留期间每帧调用的事件")]
        [SerializeField] public UnityEvent onAnimEndStay;

        [Header("动画名称触发接口")]
        [Tooltip("通过动画名（+R/C 后缀）触发播放")]
        [SerializeField] public StringEvent onPlayByName;

        private bool isStartStayActive = false;
        private bool isEndStayActive = false;

        private Animator m_animator;

        private void Awake()
        {
            m_animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (isStartStayActive)
            {
                onAnimStartStay?.Invoke();
            }

            if (isEndStayActive)
            {
                onAnimEndStay?.Invoke();
            }
        }

        /// <summary>
        /// 播放动画：使用字符串名称，支持 "nameR"（Replay）与 "nameC"（Change）模式
        /// </summary>
        /// <param name="animNameWithFlag">带有后缀的动画名，例如 "IdleR" 或 "RunC"</param>
        public void _TA_PlayAnimation(string animNameWithFlag)
        {
            if (string.IsNullOrEmpty(animNameWithFlag) || animNameWithFlag.Length < 2)
            {
                // Debug.LogWarning("动画名格式无效：" + animNameWithFlag);
                return;
            }

            char mode = animNameWithFlag[^1]; // 最后一个字符
            string animName = animNameWithFlag[..^1]; // 去掉最后一个字符

            if (m_animator == null)
                m_animator = GetComponent<Animator>();

            if (m_animator == null)
            {
                // Debug.LogWarning("Animator 未找到，播放失败");
                return;
            }

            AnimatorStateInfo currentState = m_animator.GetCurrentAnimatorStateInfo(0);

            if (mode == 'R') // Replay 模式：强制播放
            {
                m_animator.Play(animName, 0, 0f);
            }
            else if (mode == 'C') // Change 模式：不同才播放
            {
                if (!currentState.IsName(animName))
                {
                    m_animator.Play(animName, 0, 0f);
                }
            }
            else
            {
                // Debug.LogWarning($"未知播放模式：{mode}，请输入 'R' 或 'C'");
            }
        }

        #region 内部处理方法

        public void _TA_AnimEnter() => onAnimEnter?.Invoke();
        public void _TA_AnimExit() => onAnimExit?.Invoke();
        public void _TA_AnimStartStay(int index) { if (index == 0) isStartStayActive = true; }
        public void _TA_AnimEndStay(int index) { if (index == 1) { isStartStayActive = false; isEndStayActive = false; } }

        #endregion
    }
}
