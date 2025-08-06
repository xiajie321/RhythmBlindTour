using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Views.UIManager.UIPanels
{
    public class MainPanel : BasePanel
    {
        public EventTrigger startBtn;
        public EventTrigger quitBtn;
        public EventTrigger offsetTestBtn;
        public EventTrigger settingsBtn;
        public EventTrigger listBtn;

        protected override void Init()
        {
            startBtn.AddListener((data) =>
            {
                UIManager.Instance.ShowPanel<LevelSelectionPanel>();
                UIManager.Instance.HidePanel<MainPanel>();
            });

            quitBtn.AddListener((data) =>
            {
                UIManager.Instance.ShowPanel<ConfirmQuitPanel>();
            });

            offsetTestBtn.AddListener((data) =>
            {
                UIManager.Instance.ShowPanel<OffsetTestPanel>();
                UIManager.Instance.HidePanel<MainPanel>();
            });
            settingsBtn.AddListener((data) =>
            {
                UIManager.Instance.ShowPanel<GameSettingsPanel>();
                UIManager.Instance.HidePanel<MainPanel>();
            });
        }
    }
}