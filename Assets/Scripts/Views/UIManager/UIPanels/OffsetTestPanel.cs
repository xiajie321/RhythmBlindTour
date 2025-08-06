using UnityEngine.EventSystems;

namespace Views.UIManager.UIPanels
{
    public class OffsetTestPanel : BasePanel
    {
        public EventTrigger quitBtn;
        protected override void Init()
        {
            quitBtn.AddListener((data) =>
            {
                UIManager.Instance.ShowPanel<MainPanel>();
                UIManager.Instance.HidePanel<OffsetTestPanel>();
            });
        }
    }
}