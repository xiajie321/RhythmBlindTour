using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TestRevolver
{
    public class TestSelectAnim_Button : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        #region // ── 引用与原始数据 ──────────────────────────────
        [Header("引用与原始数据")]
        public TestSelectAnim_AnimActionHandler groupAnimator;

        [HideInInspector]
        public Vector3 originalScale;
        [HideInInspector]
        public Vector2 originalAnchoredPos;
        #endregion

        private Button _button;

        #region // ── 生命周期 ──────────────────────────────────
        private void Awake()
        {
            // 自动绑定 OnClick 到 Confirm 动作
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(() =>
                {
                    // 触发 Confirm 动作，而非直接调用事件
                    TestSelect_SaticAction.InputEvents.s_evConfirm.Invoke();
                });
            }
        }

        private void Start()
        {
            RectTransform rt = GetComponent<RectTransform>();
            originalScale = rt.localScale;
            originalAnchoredPos = rt.anchoredPosition;
        }
        #endregion

        #region // ── 私有处理方法 ─────────────────────────────
        private void _HandleSelect(BaseEventData eventData)
        {
            return;
            if (groupAnimator != null)
            {
                groupAnimator._SA_OnButtonSelected(this);
            }
        }

        private void _HandleDeselect(BaseEventData eventData)
        {
            return;
            if (groupAnimator != null)
            {
                groupAnimator._SA_OnButtonDeselected(this);
            }
        }
        #endregion

        #region // ── 接口显式实现 ─────────────────────────────
        void ISelectHandler.OnSelect(BaseEventData eventData) => _HandleSelect(eventData);
        void IDeselectHandler.OnDeselect(BaseEventData eventData) => _HandleDeselect(eventData);
        #endregion

        /// <summary>
        /// 应用选中状态：执行缩放和垂直/水平偏移动作
        /// </summary>
        public void _TB_ApplySelectState()
        {
            if (groupAnimator != null)
                groupAnimator._SA_OnButtonSelected(this);
        }

        /// <summary>
        /// 恢复取消选中状态：还原到原始缩放和偏移位置
        /// </summary>
        public void _TB_RestoreSelectState()
        {
            if (groupAnimator != null)
            {
                groupAnimator._SA_OnButtonDeselected(this);
                TestInput_CustomInputManager.Instance._TI_InterruptInputForDuration(0.15f);
            }
        }
    }
}
