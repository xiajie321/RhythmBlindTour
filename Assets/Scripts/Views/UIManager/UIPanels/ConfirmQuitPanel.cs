using UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Views.UIManager.UIPanels
{
    public class ConfirmQuitPanel : BasePanel
    {
        public Block yesBtn;
        public Block noBtn;
        protected override void Init()
        {
            yesBtn.OnClick +=(Application.Quit);
            noBtn.OnClick +=() =>
            {
                UIManager.Instance.HidePanel<ConfirmQuitPanel>();
            };
        }
    }
}