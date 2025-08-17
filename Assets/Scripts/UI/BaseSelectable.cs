using System;
using System.Collections.Generic;
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
        private static Stack<BaseSelectable> focusStack = new Stack<BaseSelectable>();

        public bool IsFocused
        {
            get => isFocused;
            private set
            {
                if (isFocused != value)
                {
                    isFocused = value;
                    if (value)
                    {
                        SubscribeToInput();
                        OnEnter?.Invoke();
                    }
                    else
                    {
                        UnsubscribeFromInput();
                        OnExit?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前焦点对象（栈顶）
        /// </summary>
        public static BaseSelectable CurrentFocus => focusStack.Count > 0 ? focusStack.Peek() : null;

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
                PushFocus(this);
            }
        }

        private void OnDestroy()
        {
            // 如果当前对象在栈中，移除它
            if (focusStack.Contains(this))
            {
                var tempStack = new Stack<BaseSelectable>();
                while (focusStack.Count > 0)
                {
                    var item = focusStack.Pop();
                    if (item != this)
                    {
                        tempStack.Push(item);
                    }
                }
                while (tempStack.Count > 0)
                {
                    focusStack.Push(tempStack.Pop());
                }
                
                // 如果移除的是栈顶元素，更新焦点状态
                if (CurrentFocus != null)
                {
                    CurrentFocus.IsFocused = true;
                }
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

        protected virtual void OnLeftInput(InputAction.CallbackContext context)
        {
            ChangeTo(left);
        }

        protected virtual void OnRightInput(InputAction.CallbackContext context)
        {
            ChangeTo(right);
        }

        protected virtual void OnUpInput(InputAction.CallbackContext context)
        {
            ChangeTo(up);
        }

        protected virtual void OnDownInput(InputAction.CallbackContext context)
        {
            ChangeTo(down);
        }

        protected void ChangeTo(BaseSelectable target)
        {
            if (target != null)
            {
                // 替换栈顶焦点
                if (focusStack.Count > 0)
                {
                    var currentTop = focusStack.Pop();
                    currentTop.IsFocused = false;
                }
                PushFocus(target);
            }
        }

        protected virtual void OnTapInput(InputAction.CallbackContext obj)
        {
            OnClick?.Invoke();
        }

        /// <summary>
        /// 推入焦点栈
        /// </summary>
        /// <param name="newFocus">新的焦点对象</param>
        public static void PushFocus(BaseSelectable newFocus)
        {
            if (newFocus == null) return;

            // 如果栈不为空，取消当前焦点
            if (focusStack.Count > 0)
            {
                var currentTop = focusStack.Peek();
                if (currentTop != null)
                {
                    currentTop.IsFocused = false;
                }
            }

            // 推入新焦点并激活
            focusStack.Push(newFocus);
            newFocus.IsFocused = true;
        }

        /// <summary>
        /// 弹出焦点栈
        /// </summary>
        public static void PopFocus()
        {
            if (focusStack.Count > 0)
            {
                // 移除栈顶并取消焦点
                var currentTop = focusStack.Pop();
                if (currentTop != null)
                {
                    currentTop.IsFocused = false;
                }

                // 如果栈中还有元素，激活新的栈顶
                if (focusStack.Count > 0)
                {
                    var newTop = focusStack.Peek();
                    if (newTop != null)
                    {
                        newTop.IsFocused = true;
                    }
                }
            }
        }

        /// <summary>
        /// 清空焦点栈
        /// </summary>
        public static void ClearFocusStack()
        {
            // 清除当前焦点状态
            if (focusStack.Count > 0)
            {
                var currentTop = focusStack.Peek();
                if (currentTop != null)
                {
                    currentTop.IsFocused = false;
                }
            }

            focusStack.Clear();
        }

        protected abstract void SetNormal();

        protected abstract void SetHighLight();
    }
}