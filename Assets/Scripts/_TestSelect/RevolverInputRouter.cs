using UnityEngine;

namespace TestRevolver
{
    /// <summary>
    /// 统一订阅静态输入事件，转发到 StaticData.s_currentRevolver
    /// </summary>
    public class RevolverInputRouter : MonoBehaviour
    {
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

        private void OnNext()    => TestSelect_SaticAction.StaticData.s_currentRevolver?._TR_MoveSelection(true);
        private void OnPrev()    => TestSelect_SaticAction.StaticData.s_currentRevolver?._TR_MoveSelection(false);
        private void OnConfirm() => TestSelect_SaticAction.StaticData.s_currentRevolver?._TR_ConfirmSelection();
        private void OnBack()    => TestSelect_SaticAction.StaticData.s_currentRevolver?._TR_BackAction();
        private void OnExit()    => TestSelect_SaticAction.StaticData.s_currentRevolver?._TR_Exit();
    }
}
