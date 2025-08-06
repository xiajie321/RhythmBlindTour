using UnityEngine;
using UnityEngine.EventSystems;

namespace Views.UIManager.UIPanels
{
    public class ConfirmQuitPanel : BasePanel
    {
        public EventTrigger yesBtn;
        public EventTrigger noBtn;
        protected override void Init()
        {
            yesBtn.AddListener((data) =>
            {
                Application.Quit();
            });
            noBtn.AddListener((data) =>
            {
                UIManager.Instance.HidePanel<ConfirmQuitPanel>();
            });
        }
    }
}