using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace UI
{
    public class SlideSelectable : BaseSelectable
    {
        [Header("Slide Settings")]
        [SerializeField] private float slideSpeed = 50f;
        [SerializeField] private float slideSensitivity = 0.01f;
        
        [Header("UI References")]
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI valueText;
        
        private bool isSliding = false;
        private float targetValue;
        private float lastTapTime = 0f;
        private float tapCooldown = 0.2f; // 200毫秒冷却时间
        
        protected override void Awake()
        {
            base.Awake();
            
            // 如果没有手动指定，自动查找组件
            if (slider == null)
                slider = GetComponentInChildren<Slider>();
            if (valueText == null)
                valueText = GetComponentInChildren<TextMeshProUGUI>();
                
            if (slider != null)
            {
                targetValue = slider.value;
            }
        }
        
        protected override void SetNormal()
        {
            if (sprite != null)
            {
                sprite.color = new Color(184f/255f,194f/255f,195f/255f,1);
            }
            
            // 退出滑动模式
            if (isSliding)
            {
                isSliding = false;
            }
        }

        protected override void SetHighLight()
        {
            if (sprite != null)
            {
                sprite.color = new Color(0.22f, 0.17f, 0.15f); // 深色高亮
            }
        }
        
        private void Update()
        {
            if (isSliding)
            {
                UpdateSlide();
            }
        }
        
        private void UpdateSlide()
        {
            // 获取左右输入
            float horizontalInput = 0f;
            
            if (InputManager.Instance?.inputMap != null)
            {
                // 检查左右按键输入
                if (InputManager.Instance.inputMap.Gameplay.Left.ReadValue<float>() > 0.5f)
                {
                    horizontalInput = -1f;
                }
                else if (InputManager.Instance.inputMap.Gameplay.Right.ReadValue<float>() > 0.5f)
                {
                    horizontalInput = 1f;
                }
            }
            
            // 调整目标值
            if (horizontalInput != 0f && slider != null)
            {
                targetValue += horizontalInput * slideSpeed * slideSensitivity * Time.deltaTime;
                targetValue = Mathf.Clamp(targetValue, slider.minValue, slider.maxValue);
                slider.value = targetValue;
            }
        }
        
        private void EnterSlideMode()
        {
            isSliding = true;
            if (slider != null)
            {
                targetValue = slider.value;
            }
            
            // 橙色表示滑动模式
            if (sprite != null)
            {
                sprite.color = new Color(1f, 0.5f, 0f);
            }
        }
        
        private void ExitSlideMode()
        {
            isSliding = false;
            if (slider != null)
            {
                targetValue = slider.value;
            }
            
            // 恢复高亮状态
            if (IsFocused)
            {
                SetHighLight();
            }
        }
        

        
        // 重写BaseSelectable的输入方法
        protected override void OnTapInput(InputAction.CallbackContext obj)
        {
            // 检查冷却时间
            if (Time.time - lastTapTime < tapCooldown)
            {
                return;
            }
            
            lastTapTime = Time.time;
            
            if (!isSliding)
            {
                // 第一次点击进入滑动模式
                EnterSlideMode();
                OnClick?.Invoke(); // 调用父类的点击事件
            }
            else
            {
                // 第二次点击退出滑动模式
                ExitSlideMode();
            }
        }
        
        protected override void OnLeftInput(InputAction.CallbackContext context)
        {
            if (isSliding && slider != null)
            {
                // 在滑动模式下，左键减少值
                float step = (slider.maxValue - slider.minValue) / 100f; // 1%步长
                float newValue = slider.value - step;
                slider.value = Mathf.Clamp(newValue, slider.minValue, slider.maxValue);
            }
            else
            {
                // 正常导航
                ChangeTo(left);
            }
        }
        
        protected override void OnRightInput(InputAction.CallbackContext context)
        {
            if (isSliding && slider != null)
            {
                // 在滑动模式下，右键增加值
                float step = (slider.maxValue - slider.minValue) / 100f; // 1%步长
                float newValue = slider.value + step;
                slider.value = Mathf.Clamp(newValue, slider.minValue, slider.maxValue);
            }
            else
            {
                // 正常导航
                ChangeTo(right);
            }
        }
        
        protected override void OnUpInput(InputAction.CallbackContext context)
        {
            if (!isSliding)
            {
                ChangeTo(up);
            }
        }
        
        protected override void OnDownInput(InputAction.CallbackContext context)
        {
            if (!isSliding)
            {
                ChangeTo(down);
            }
        }
        
        // 公共方法，用于外部控制
        public void SetSlideMode(bool enabled)
        {
            if (enabled)
            {
                EnterSlideMode();
            }
            else
            {
                ExitSlideMode();
            }
        }
        
        public float GetCurrentValue()
        {
            return slider != null ? slider.value : 0f;
        }
        
        public void SetValue(float value)
        {
            if (slider != null)
            {
                slider.value = value;
            }
        }
    }
}