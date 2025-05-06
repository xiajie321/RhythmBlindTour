using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TestRevolver
{
    public enum InputSourceType
    {
        None,
        Keyboard,
        Touchscreen
    }

    public enum SwipeDirection
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// TestInput_CustomInputManager：管理自定义输入，
    /// 可处理触屏和键盘输入。
    /// 在触屏模式下：
    /// - 单指滑动操作输出前缀“S”，双指输出前缀“D”；
    /// 键盘输入则直接按按键对应操作输出。
    /// 所有操作结果会更新到 UI 文本中，形如 "SLeft: 0.0400"，
    /// 而不再打印到 Console。
    /// </summary>
    public class TestInput_CustomInputManager : MonoBehaviour
    {
        #region // ── 输入模式与阈值设置 ─────────────────────────────
        public static InputSourceType s_inputMode = InputSourceType.Touchscreen;
        public InputSourceType inputMode = InputSourceType.Touchscreen;
        public static bool IsKeyboardInputEnabled => s_inputMode == InputSourceType.Keyboard;
        public static bool IsTouchInputEnabled => s_inputMode == InputSourceType.Touchscreen;

        [Header("输入阈值设置")]
        [Tooltip("滑动判定最小距离（像素）")]
        public float swipeThreshold = 5f;   // 调整为 5 像素
        [Tooltip("点击判定最大移动距离（像素）")]
        public float tapMaxDistance = 10f;
        [Tooltip("点击判定最大按下时间（秒）")]
        public float tapMaxTime = 0.3f;
        [Tooltip("长按阈值（秒）")]
        public float longPressThreshold = 1.0f;
        #endregion

        #region // ── 状态记录与调试输出 ─────────────────────────────
        [Header("状态记录")]
        public SwipeDirection currentSwipeDirection = SwipeDirection.None;

        [Header("调试输出")]
        [Tooltip("场景中用于显示响应延迟及操作信息的 Text 组件")]
        public Text responseDelayText;
        #endregion

        public static TestInput_CustomInputManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }


        private void Start()
        {
            s_inputMode = inputMode;
        }

        #region // ── 内部数据 ─────────────────────────────────────────
        // 触屏数据
        private Vector2 touchStartPos;
        private float touchStartTime;
        private bool longPressTriggered = false;
        private bool isMultiTouch = false;
        private bool gestureTriggered = false; // 标记是否已触发滑动事件

        // 键盘数据
        private float lastKeyDownTime = 0f;
        private bool keyLongPressActive = false;
        #endregion

        #region // ── Unity 生命周期 ─────────────────────────────────
        private void Update()
        {
            if (IsTouchInputEnabled)
            {
                ProcessTouchInput();
            }
            else if (IsKeyboardInputEnabled)
            {
                ProcessKeyboardInput();
            }
        }
        #endregion

        #region // ── 触控输入处理 ─────────────────────────────────
        private void ProcessTouchInput()
        {
            if (Input.touchCount == 0)
            {
                currentSwipeDirection = SwipeDirection.None;
                longPressTriggered = false;
                gestureTriggered = false;
                return;
            }

            // ────── 多指触控处理 ─────────────────────────────
            if (Input.touchCount >= 2)
            {
                isMultiTouch = true;
                Touch touch1 = Input.GetTouch(0);
                Touch touch2 = Input.GetTouch(1);
                Vector2 avgDelta = (touch1.deltaPosition + touch2.deltaPosition) * 0.5f;

                if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended)
                {
                    float dualDelay = Time.time - touchStartTime;
                    currentSwipeDirection = GetSwipeDirection(avgDelta);
                    TriggerDualTouchEvent(currentSwipeDirection, dualDelay);
                    isMultiTouch = false;
                }
                return;
            }
            else
            {
                isMultiTouch = false;
                Touch touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchStartPos = touch.position;
                        touchStartTime = Time.time;
                        longPressTriggered = false;
                        gestureTriggered = false;
                        break;
                    case TouchPhase.Moved:
                        if (!longPressTriggered && (Time.time - touchStartTime) >= longPressThreshold)
                        {
                            longPressTriggered = true;
                            float delay = Time.time - touchStartTime;
                            TriggerLongPressEvent(true, false, delay);
                            UpdateResponseDelay(delay);
                        }
                        if (!gestureTriggered)
                        {
                            Vector2 moveDelta = touch.position - touchStartPos;
                            if (moveDelta.magnitude >= swipeThreshold)
                            {
                                gestureTriggered = true;
                                currentSwipeDirection = GetSwipeDirection(moveDelta);
                                float delay = Time.time - touchStartTime;
                                TriggerSwipeEvent(currentSwipeDirection, false, delay);
                                UpdateResponseDelay(delay);
                            }
                        }
                        break;
                    case TouchPhase.Ended:
                        float touchDuration = Time.time - touchStartTime;
                        Vector2 delta = touch.position - touchStartPos;
                        if (!gestureTriggered && delta.magnitude <= tapMaxDistance && touchDuration <= tapMaxTime)
                        {
                            TriggerTapEvent(false, touchDuration);
                            UpdateResponseDelay(touchDuration);
                        }
                        else if (!gestureTriggered && delta.magnitude >= swipeThreshold)
                        {
                            currentSwipeDirection = GetSwipeDirection(delta);
                            TriggerSwipeEvent(currentSwipeDirection, false, touchDuration);
                            UpdateResponseDelay(touchDuration);
                        }
                        currentSwipeDirection = SwipeDirection.None;
                        longPressTriggered = false;
                        gestureTriggered = false;
                        break;
                }
            }
        }
        #endregion

        #region // ── 键盘输入处理 ─────────────────────────────────
        private void ProcessKeyboardInput()
        {
            if (Input.anyKeyDown)
                lastKeyDownTime = Time.time;

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerSwipeEvent(SwipeDirection.Left, false, delay);
                UpdateResponseDelay(delay);
            }
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerSwipeEvent(SwipeDirection.Right, false, delay);
                UpdateResponseDelay(delay);
            }
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerSwipeEvent(SwipeDirection.Up, false, delay);
                UpdateResponseDelay(delay);
            }
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerSwipeEvent(SwipeDirection.Down, false, delay);
                UpdateResponseDelay(delay);
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerTapEvent(false, delay);
                UpdateResponseDelay(delay);
            }

            if (Input.GetKey(KeyCode.Space))
            {
                if (!keyLongPressActive)
                {
                    keyLongPressActive = true;
                    lastKeyDownTime = Time.time;
                }
                else if ((Time.time - lastKeyDownTime) >= longPressThreshold)
                {
                    float delay = Time.time - lastKeyDownTime;
                    TriggerLongPressEvent(true, false, delay);
                    UpdateResponseDelay(delay);
                }
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (keyLongPressActive)
                {
                    float delay = Time.time - lastKeyDownTime;
                    TriggerLongPressEvent(false, false, delay);
                    keyLongPressActive = false;
                }
            }

            // 键盘双键组合操作
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q) ||
                (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.K)))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerSwipeEvent(SwipeDirection.Down, false, delay);
                UpdateResponseDelay(delay);
            }
            if (Input.GetKey(KeyCode.W) && Input.GetKeyDown(KeyCode.I))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerSwipeEvent(SwipeDirection.Up, false, delay);
                UpdateResponseDelay(delay);
            }
            if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.J))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerSwipeEvent(SwipeDirection.Left, false, delay);
                UpdateResponseDelay(delay);
            }
            if (Input.GetKey(KeyCode.D) && Input.GetKeyDown(KeyCode.L))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerSwipeEvent(SwipeDirection.Right, false, delay);
                UpdateResponseDelay(delay);
            }
            if (Input.GetKey(KeyCode.Space) && Input.GetKey(KeyCode.M))
            {
                float delay = Time.time - lastKeyDownTime;
                TriggerLongPressEvent(true, true, delay);
                UpdateResponseDelay(delay);
            }
        }
        #endregion

        #region // ── 通用辅助方法 ───────────────────────────────────
        private SwipeDirection GetSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            else
                return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }

        /// <summary>
        /// 更新 UI 显示，直接将操作名称和延迟显示到 responseDelayText 中。
        /// </summary>
        /// <param name="operation">操作名称（带前缀，例如 "SLeft"）</param>
        /// <param name="delay">延迟值</param>
        private void UpdateUIDisplay(string operation, float delay)
        {
            if (responseDelayText != null)
                responseDelayText.text = operation + ": " + delay.ToString("F4");
        }

        /// <summary>
        /// 触发滑动事件，更新 UI 显示操作类型和延迟。
        /// 若触屏输入，则前缀为 "S"（单指）或 "D"（双指）；键盘输入直接输出按键对应名称。
        /// </summary>
        private void TriggerSwipeEvent(SwipeDirection direction, bool isDual, float delay)
        {
            string prefix = "";
            if (s_inputMode == InputSourceType.Touchscreen)
                prefix = isDual ? "D" : "S";

            switch (direction)
            {
                case SwipeDirection.Left:
                    UpdateUIDisplay(prefix + "Left", delay);
                    TestSelect_SaticAction.InputEvents.s_evPrevOption.Invoke();
                    break;
                case SwipeDirection.Right:
                    UpdateUIDisplay(prefix + "Right", delay);
                    TestSelect_SaticAction.InputEvents.s_evNextOption.Invoke();
                    break;
                case SwipeDirection.Up:
                    UpdateUIDisplay(prefix + "Up", delay);
                    break;
                case SwipeDirection.Down:
                    UpdateUIDisplay(prefix + "Down", delay);
                    if (s_inputMode == InputSourceType.Touchscreen && isDual)
                        TestSelect_SaticAction.InputEvents.s_evExit.Invoke();
                    else
                        TestSelect_SaticAction.InputEvents.s_evBack.Invoke();
                    break;
            }
        }

        /// <summary>
        /// 触发双指滑动事件，更新 UI 显示。
        /// </summary>
        private void TriggerDualTouchEvent(SwipeDirection direction, float delay)
        {
            string prefix = "";
            if (s_inputMode == InputSourceType.Touchscreen)
                prefix = "D";

            switch (direction)
            {
                case SwipeDirection.Left:
                    UpdateUIDisplay(prefix + "Left", delay);
                    break;
                case SwipeDirection.Right:
                    UpdateUIDisplay(prefix + "Right", delay);
                    break;
                case SwipeDirection.Up:
                    UpdateUIDisplay(prefix + "Up", delay);
                    break;
                case SwipeDirection.Down:
                    UpdateUIDisplay(prefix + "Down", delay);
                    TestSelect_SaticAction.InputEvents.s_evExit.Invoke();
                    break;
            }
        }

        /// <summary>
        /// 触发点击事件，更新 UI 显示操作类型和延迟。
        /// </summary>
        private void TriggerTapEvent(bool isDual, float delay)
        {
            string prefix = "";
            if (s_inputMode == InputSourceType.Touchscreen)
                prefix = isDual ? "D" : "S";
            UpdateUIDisplay(prefix + "Tap", delay);
            TestSelect_SaticAction.InputEvents.s_evConfirm.Invoke();
        }

        /// <summary>
        /// 触发长按事件，更新 UI 显示操作类型和延迟。
        /// </summary>
        private void TriggerLongPressEvent(bool isActive, bool isDual, float delay)
        {
            string prefix = "";
            if (s_inputMode == InputSourceType.Touchscreen)
                prefix = isDual ? "D" : "S";
            if (isActive)
                UpdateUIDisplay(prefix + "LongPress", delay);
            else
                UpdateUIDisplay(prefix + "LongPressEnd", delay);
        }

        /// <summary>
        /// 更新响应延迟显示文本（仅显示数值及小数格式，通过 UpdateUIDisplay 更新）。
        /// </summary>
        private void UpdateResponseDelay(float delay)
        {
            // 此方法可用于单独更新UI显示，当前已由各触发函数调用 UpdateUIDisplay 完成
        }

        public void SetInputMode(InputSourceType mode)
        {
            s_inputMode = mode;
            if (responseDelayText != null)
                responseDelayText.text = mode.ToString();
        }

        public void ToggleInputMode()
        {
            switch (s_inputMode)
            {
                case InputSourceType.Keyboard:
                    SetInputMode(InputSourceType.Touchscreen);
                    break;
                case InputSourceType.Touchscreen:
                    SetInputMode(InputSourceType.None);
                    break;
                case InputSourceType.None:
                    SetInputMode(InputSourceType.Keyboard);
                    break;
            }
        }

        #endregion

        #region // ── 中断输入方法 ───────────────────────────────────
        /// <summary>
        /// 通过协程中断输入，指定中断时长，期间将输入模式设为 None，
        /// 待指定时长结束后恢复为原来的输入模式。
        /// </summary>
        /// <param name="duration">中断时长（秒）</param>
        public void _TI_InterruptInputForDuration(float duration)
        {
            // 保存当前输入模式
            InputSourceType originalMode = s_inputMode;
            StartCoroutine(InterruptCoroutine(duration, originalMode));
        }

        private System.Collections.IEnumerator InterruptCoroutine(float duration, InputSourceType originalMode)
        {
            s_inputMode = InputSourceType.None;
            yield return new WaitForSeconds(duration);
            s_inputMode = originalMode;
        }
        #endregion
    }
}
