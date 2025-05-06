using UnityEngine;

namespace TestRevolver
{
    /// <summary>
    /// TestSelectAnim_AnimActionHandler：负责对外暴露动画操作接口，
    /// 并调用对应的 GroupAnimator 内部处理方法。
    /// </summary>
    public class TestSelectAnim_AnimActionHandler : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("指定需要调用动画操作的按钮组动画管理器")]
        [SerializeField] private TestSelectAnim_GroupAnimator groupAnimator;

        private void Awake()
        {
            // 如果没在 Inspector 中指定，则自动查找同一物体上的组件
            if (groupAnimator == null)
            {
                groupAnimator = GetComponent<TestSelectAnim_GroupAnimator>();
            }
        }

        /// <summary>
        /// 曝露按钮选中动画及相关处理（外部调用入口）
        /// </summary>
        public void _SA_OnButtonSelected(TestSelectAnim_Button animButton)
        {
            if (groupAnimator != null)
                groupAnimator.HandleButtonSelected(animButton);
        }

        /// <summary>
        /// 曝露按钮取消选中恢复处理（外部调用入口）
        /// </summary>
        public void _SA_OnButtonDeselected(TestSelectAnim_Button animButton)
        {
            if (groupAnimator != null)
                groupAnimator.HandleButtonDeselected(animButton);
        }

        /// <summary>
        /// 曝露向右切换（边缘跳跃）的功能，前缀 _SA_
        /// </summary>
        public void _SA_ShiftRight(int groupIndex)
        {
            if (groupAnimator != null)
                groupAnimator.HandleShiftRight(groupIndex);
        }

        /// <summary>
        /// 曝露向左切换（边缘跳跃）的功能，前缀 _SA_
        /// </summary>
        public void _SA_ShiftLeft(int groupIndex)
        {
            if (groupAnimator != null)
                groupAnimator.HandleShiftLeft(groupIndex);
        }
    }
}
