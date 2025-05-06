//using UnityEngine;
//using UnityEngine.InputSystem;

//namespace TestRevolver
//{
//    public enum InputSourceType
//    {
//        None,
//        Keyboard,
//        Touchscreen
//    }

//    public enum SwipeDirection
//    {
//        None,
//        Left,
//        Right,
//        Up,
//        Down
//    }

//    public class TestSelect_InputManager : MonoBehaviour
//    {
//        public static InputSourceType s_inputMode = InputSourceType.Touchscreen;

//        public static bool IsKeyboardInputEnabled => s_inputMode == InputSourceType.Keyboard;
//        public static bool IsTouchInputEnabled => s_inputMode == InputSourceType.Touchscreen;

//        [Header("触控操作输入 Action")]
//        public InputAction swipeLeftAction;
//        public InputAction swipeRightAction;
//        public InputAction tapAction;
//        public InputAction swipeUpAction;
//        public InputAction swipeDownAction;
//        public InputAction longPressAction;

//        [Header("多指触控输入 Action")]
//        public InputAction swipeLeftDualAction;
//        public InputAction swipeRightDualAction;
//        public InputAction tapDualAction;
//        public InputAction swipeUpDualAction;
//        public InputAction swipeDownDualAction;
//        public InputAction longPressDualAction;

//        [Header("滑动状态记录")]
//        public SwipeDirection currentSwipeDirection = SwipeDirection.None;

//        [Header("滑动触发 InputAction（抬手触发）")]
//        public InputAction swipeTriggerAction; // Touch phase == Ended

//        [Header("键盘输入 Action")]
//        public InputAction leftAction;
//        public InputAction rightAction;
//        public InputAction enterAction;
//        public InputAction upAction;
//        public InputAction downAction;
//        public InputAction keyPressAction;

//        [Header("键盘双指模拟 Action")]
//        public InputAction leftDualAction;
//        public InputAction rightDualAction;
//        public InputAction enterDualAction;
//        public InputAction upDualAction;
//        public InputAction downDualAction;
//        public InputAction keyPressDualAction;

//        #region 输入模式控制方法

//        public void SetInputMode(InputSourceType mode)
//        {
//            s_inputMode = mode;
//            Debug.Log($"输入模式切换为：{mode}");
//        }

//        public void ToggleInputMode()
//        {
//            switch (s_inputMode)
//            {
//                case InputSourceType.Keyboard:
//                    SetInputMode(InputSourceType.Touchscreen);
//                    break;
//                case InputSourceType.Touchscreen:
//                    SetInputMode(InputSourceType.None);
//                    break;
//                case InputSourceType.None:
//                    SetInputMode(InputSourceType.Keyboard);
//                    break;
//            }
//        }

//        #endregion

//        private void OnEnable()
//        {
//            // 启用触控 Action
//            swipeLeftAction?.Enable();
//            swipeRightAction?.Enable();
//            tapAction?.Enable();
//            swipeUpAction?.Enable();
//            swipeDownAction?.Enable();
//            longPressAction?.Enable();

//            swipeLeftDualAction?.Enable();
//            swipeRightDualAction?.Enable();
//            tapDualAction?.Enable();
//            swipeUpDualAction?.Enable();
//            swipeDownDualAction?.Enable();
//            longPressDualAction?.Enable();

//            swipeTriggerAction?.Enable();
//            swipeTriggerAction.performed += OnSwipeTrigger;

//            // 启用键盘 Action（仅在 PC 或测试时手动切换）
//            leftAction?.Enable();
//            rightAction?.Enable();
//            enterAction?.Enable();
//            upAction?.Enable();
//            downAction?.Enable();
//            keyPressAction?.Enable();

//            leftDualAction?.Enable();
//            rightDualAction?.Enable();
//            enterDualAction?.Enable();
//            upDualAction?.Enable();
//            downDualAction?.Enable();
//            keyPressDualAction?.Enable();
//        }

//        private void OnDisable()
//        {
//            swipeLeftAction?.Disable();
//            swipeRightAction?.Disable();
//            tapAction?.Disable();
//            swipeUpAction?.Disable();
//            swipeDownAction?.Disable();
//            longPressAction?.Disable();

//            swipeLeftDualAction?.Disable();
//            swipeRightDualAction?.Disable();
//            tapDualAction?.Disable();
//            swipeUpDualAction?.Disable();
//            swipeDownDualAction?.Disable();
//            longPressDualAction?.Disable();

//            swipeTriggerAction?.Disable();
//            swipeTriggerAction.performed -= OnSwipeTrigger;

//            leftAction?.Disable();
//            rightAction?.Disable();
//            enterAction?.Disable();
//            upAction?.Disable();
//            downAction?.Disable();
//            keyPressAction?.Disable();

//            leftDualAction?.Disable();
//            rightDualAction?.Disable();
//            enterDualAction?.Disable();
//            upDualAction?.Disable();
//            downDualAction?.Disable();
//            keyPressDualAction?.Disable();
//        }

//        private void OnSwipeTrigger(InputAction.CallbackContext context)
//        {
//            if (!IsTouchInputEnabled)
//                return;

//            var phase = context.ReadValue<UnityEngine.InputSystem.TouchPhase>();
//            if (phase != UnityEngine.InputSystem.TouchPhase.Ended)
//                return;

//            // ✅ 只在抬手后触发
//            switch (currentSwipeDirection)
//            {
//                case SwipeDirection.Left:
//                    TestSelect_SaticAction.InputEvents.s_evPrevOption.Invoke();
//                    break;
//                case SwipeDirection.Right:
//                    TestSelect_SaticAction.InputEvents.s_evNextOption.Invoke();
//                    break;
//                case SwipeDirection.Up:
//                    Debug.Log("⬆️ 上滑触发");
//                    break;
//                case SwipeDirection.Down:
//                    TestSelect_SaticAction.InputEvents.s_evBack.Invoke();
//                    break;
//            }

//            currentSwipeDirection = SwipeDirection.None;
//        }

//    }
//}
