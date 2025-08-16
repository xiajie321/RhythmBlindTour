using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    public abstract class BaseSelectable : MonoBehaviour
    {
        protected Image sprite;
        public UnityAction OnDown;
        public UnityAction OnEnter;
        public UnityAction OnExit;
        public UnityAction OnUp;
        public UnityAction OnClick;

        public BaseSelectable left;
        public BaseSelectable right;
        public BaseSelectable up;
        public BaseSelectable down;

        [SerializeField] private bool startWithFocus = false;
        private bool isFocused = false;
        private static BaseSelectable currentFocused = null;
        private static System.Collections.Generic.Stack<BaseSelectable> focusStack = new System.Collections.Generic.Stack<BaseSelectable>();

        public bool IsFocused
        {
            get => isFocused;
            set
            {
                if (isFocused != value)
                {
                    if (value)
                    {
                        // 取消之前的焦点
                        if (currentFocused != null && currentFocused != this)
                        {
                            currentFocused.IsFocused = false;
                        }
                        
                        // 设置新焦点
                        currentFocused = this;
                        isFocused = true;
                        SubscribeToInput();
                        OnEnter?.Invoke();
                    }
                    else
                    {
                        // 失去焦点
                        if (currentFocused == this)
                        {
                            currentFocused = null;
                        }
                        
                        isFocused = false;
                        UnsubscribeFromInput();
                        OnExit?.Invoke();
                    }
                }
            }
        }

        protected virtual void Awake()
        {
            sprite = transform.GetComponent<Image>();
            OnEnter += SetHighLight;
            OnExit += SetNormal;
        }

        private void Start()
        {
            if (startWithFocus)
            {
                IsFocused = true;
            }
        }

        private void OnDestroy()
        {
            if (currentFocused == this)
            {
                currentFocused = null;
            }
            UnsubscribeFromInput();
        }

        private void SubscribeToInput()
        {
            if (InputManager.Instance?.inputMap != null)
            {
                InputManager.Instance.inputMap.Gameplay.Left.performed += OnLeftInput;
                InputManager.Instance.inputMap.Gameplay.Right.performed += OnRightInput;
                InputManager.Instance.inputMap.Gameplay.Up.performed += OnUpInput;
                InputManager.Instance.inputMap.Gameplay.Down.performed += OnDownInput;
                InputManager.Instance.inputMap.Gameplay.Tap.performed += OnTapInput;
            }
        }

        private void UnsubscribeFromInput()
        {
            if (InputManager.Instance?.inputMap != null)
            {
                InputManager.Instance.inputMap.Gameplay.Left.performed -= OnLeftInput;
                InputManager.Instance.inputMap.Gameplay.Right.performed -= OnRightInput;
                InputManager.Instance.inputMap.Gameplay.Up.performed -= OnUpInput;
                InputManager.Instance.inputMap.Gameplay.Down.performed -= OnDownInput;
                InputManager.Instance.inputMap.Gameplay.Tap.performed -= OnTapInput;
            }
        }

        private void OnLeftInput(InputAction.CallbackContext context)
        {
            ChangeTo(left);
        }

        private void OnRightInput(InputAction.CallbackContext context)
        {
            ChangeTo(right);
        }

        private void OnUpInput(InputAction.CallbackContext context)
        {
            ChangeTo(up);
        }

        private void OnDownInput(InputAction.CallbackContext context)
        {
            ChangeTo(down);
        }

        private void ChangeTo(BaseSelectable target)
        {
            if (target != null)
            {
                target.IsFocused = true;
            }
        }
        private void OnTapInput(InputAction.CallbackContext obj)
        {
            OnClick?.Invoke();
        }

        /// <summary>
        /// 推入焦点栈（当打开二级菜单时调用）
        /// </summary>
        /// <param name="newFocus">二级菜单中的焦点对象</param>
        public static void PushFocus(BaseSelectable newFocus)
        {
            if (currentFocused != null)
            {
                focusStack.Push(currentFocused);
                currentFocused.IsFocused = false;
            }
            
            if (newFocus != null)
            {
                newFocus.IsFocused = true;
            }
        }

        /// <summary>
        /// 弹出焦点栈（当关闭二级菜单时调用）
        /// </summary>
        public static void PopFocus()
        {
            // 清除当前焦点
            if (currentFocused != null)
            {
                currentFocused.IsFocused = false;
            }

            // 恢复之前的焦点
            if (focusStack.Count > 0)
            {
                BaseSelectable previousFocus = focusStack.Pop();
                if (previousFocus != null)
                {
                    previousFocus.IsFocused = true;
                }
            }
        }

        /// <summary>
        /// 获取当前焦点对象
        /// </summary>
        public static BaseSelectable GetCurrentFocus()
        {
            return currentFocused;
        }

        /// <summary>
        /// 清空焦点栈（在场景切换时调用）
        /// </summary>
        public static void ClearFocusStack()
        {
            focusStack.Clear();
            currentFocused = null;
        }

        protected abstract void SetNormal();

        protected abstract void SetHighLight();
    }
}