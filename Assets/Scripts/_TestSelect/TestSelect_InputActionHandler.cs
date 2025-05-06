//using UnityEngine;
//using UnityEngine.InputSystem;

//namespace TestRevolver
//{
//    public class TestSelect_InputActionHandler : MonoBehaviour
//    {
//        private TestSelect_InputManager inputManager;

//        private void OnEnable()
//        {
//            inputManager = FindObjectOfType<TestSelect_InputManager>();

//            if (inputManager == null)
//            {
//                Debug.LogWarning("未找到 TestSelect_InputManager，无法绑定 InputActions！");
//                return;
//            }

//            // 触控动作绑定（用于 Touchscreen 模式）
//            Bind(inputManager.swipeLeftAction, OnSwipeLeft_Single);
//            Bind(inputManager.swipeRightAction, OnSwipeRight_Single);
//            Bind(inputManager.tapAction, OnTap_Single);
//            Bind(inputManager.swipeUpAction, OnSwipeUp_Single);
//            Bind(inputManager.swipeDownAction, OnSwipeDown_Single);
//            Bind(inputManager.longPressAction, OnLongPress_Single);

//            Bind(inputManager.swipeLeftDualAction, OnSwipeLeft_Dual);
//            Bind(inputManager.swipeRightDualAction, OnSwipeRight_Dual);
//            Bind(inputManager.tapDualAction, OnTap_Dual);
//            Bind(inputManager.swipeUpDualAction, OnSwipeUp_Dual);
//            Bind(inputManager.swipeDownDualAction, OnSwipeDown_Dual);
//            Bind(inputManager.longPressDualAction, OnLongPress_Dual);

//            // 键盘动作绑定（直接触发）
//            Bind(inputManager.leftAction, OnSwipeLeft_Single);
//            Bind(inputManager.rightAction, OnSwipeRight_Single);
//            Bind(inputManager.enterAction, OnTap_Single);
//            Bind(inputManager.upAction, OnSwipeUp_Single);
//            Bind(inputManager.downAction, OnSwipeDown_Single);
//            Bind(inputManager.keyPressAction, OnLongPress_Single);

//            Bind(inputManager.leftDualAction, OnSwipeLeft_Dual);
//            Bind(inputManager.rightDualAction, OnSwipeRight_Dual);
//            Bind(inputManager.enterDualAction, OnTap_Dual);
//            Bind(inputManager.upDualAction, OnSwipeUp_Dual);
//            Bind(inputManager.downDualAction, OnSwipeDown_Dual);
//            Bind(inputManager.keyPressDualAction, OnLongPress_Dual);
//        }

//        private void OnDisable()
//        {
//            if (inputManager == null) return;

//            Unbind(inputManager.swipeLeftAction, OnSwipeLeft_Single);
//            Unbind(inputManager.swipeRightAction, OnSwipeRight_Single);
//            Unbind(inputManager.tapAction, OnTap_Single);
//            Unbind(inputManager.swipeUpAction, OnSwipeUp_Single);
//            Unbind(inputManager.swipeDownAction, OnSwipeDown_Single);
//            Unbind(inputManager.longPressAction, OnLongPress_Single);

//            Unbind(inputManager.swipeLeftDualAction, OnSwipeLeft_Dual);
//            Unbind(inputManager.swipeRightDualAction, OnSwipeRight_Dual);
//            Unbind(inputManager.tapDualAction, OnTap_Dual);
//            Unbind(inputManager.swipeUpDualAction, OnSwipeUp_Dual);
//            Unbind(inputManager.swipeDownDualAction, OnSwipeDown_Dual);
//            Unbind(inputManager.longPressDualAction, OnLongPress_Dual);

//            Unbind(inputManager.leftAction, OnSwipeLeft_Single);
//            Unbind(inputManager.rightAction, OnSwipeRight_Single);
//            Unbind(inputManager.enterAction, OnTap_Single);
//            Unbind(inputManager.upAction, OnSwipeUp_Single);
//            Unbind(inputManager.downAction, OnSwipeDown_Single);
//            Unbind(inputManager.keyPressAction, OnLongPress_Single);

//            Unbind(inputManager.leftDualAction, OnSwipeLeft_Dual);
//            Unbind(inputManager.rightDualAction, OnSwipeRight_Dual);
//            Unbind(inputManager.enterDualAction, OnTap_Dual);
//            Unbind(inputManager.upDualAction, OnSwipeUp_Dual);
//            Unbind(inputManager.downDualAction, OnSwipeDown_Dual);
//            Unbind(inputManager.keyPressDualAction, OnLongPress_Dual);
//        }

//        private void Bind(InputAction action, System.Action<InputAction.CallbackContext> callback)
//        {
//            if (action != null)
//                action.performed += callback;
//        }

//        private void Unbind(InputAction action, System.Action<InputAction.CallbackContext> callback)
//        {
//            if (action != null)
//                action.performed -= callback;
//        }

//        #region 单指操作
//        private void OnSwipeLeft_Single(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                TestSelect_SaticAction.InputEvents.s_evPrevOption.Invoke();
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.Left;
//        }

//        private void OnSwipeRight_Single(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                TestSelect_SaticAction.InputEvents.s_evNextOption.Invoke();
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.Right;
//        }

//        private void OnTap_Single(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                TestSelect_SaticAction.InputEvents.s_evConfirm.Invoke();
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.None; // 点击不视为滑动
//        }

//        private void OnSwipeUp_Single(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                Debug.Log("⬆️ 上滑（键盘）");
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.Up;
//        }

//        private void OnSwipeDown_Single(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                TestSelect_SaticAction.InputEvents.s_evBack.Invoke();
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.Down;
//        }

//        private void OnLongPress_Single(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                Debug.Log("⏱️ 单指长按（键盘）");
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.None; // 长按也不作为滑动方向处理
//        }
//        #endregion

//        #region 双指操作
//        private void OnSwipeLeft_Dual(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                Debug.Log("👆👆 双指左滑");
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.Left;
//        }

//        private void OnSwipeRight_Dual(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                Debug.Log("👆👆 双指右滑");
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.Right;
//        }

//        private void OnTap_Dual(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                Debug.Log("👆👆 双指点击");
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.None;
//        }

//        private void OnSwipeUp_Dual(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                Debug.Log("👆👆 双指上滑");
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.Up;
//        }

//        private void OnSwipeDown_Dual(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                TestSelect_SaticAction.InputEvents.s_evExit.Invoke();
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.Down;
//        }

//        private void OnLongPress_Dual(InputAction.CallbackContext _)
//        {
//            if (TestSelect_InputManager.IsKeyboardInputEnabled)
//                Debug.Log("⏱️ 双指长按（键盘）");
//            else if (TestSelect_InputManager.IsTouchInputEnabled)
//                inputManager.currentSwipeDirection = SwipeDirection.None;
//        }
//        #endregion
//    }
//}
