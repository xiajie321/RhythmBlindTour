using UnityEngine.EventSystems;

namespace Views.UIManager.UIPanels
{
    public class GameMenuPanel : BasePanel
    {
        public EventTrigger continueBtn;
        public EventTrigger backBtn;
        public EventTrigger skipBtn;
        public EventTrigger offsetTestBtn;
        public EventTrigger settingsBtn;
        public EventTrigger quitBtn;
        
        protected override void Init()
        {
            continueBtn.AddListener(() =>
            {
                UIManager.Instance.HidePanel<GameMenuPanel>();
            });
            
            backBtn.AddListener(() =>
            {
                UIManager.Instance.ShowPanel<LevelSelectionPanel>();
                UIManager.Instance.HidePanel<GameMenuPanel>();
            });
            
            offsetTestBtn.AddListener((data) =>
            {
                UIManager.Instance.ShowPanel<OffsetTestPanel>();
                UIManager.Instance.HidePanel<GameMenuPanel>();
            });
            settingsBtn.AddListener((data) =>
            {
                UIManager.Instance.ShowPanel<GameSettingsPanel>();
                UIManager.Instance.HidePanel<GameMenuPanel>();
            });
            quitBtn.AddListener((data) =>
            {
                UIManager.Instance.ShowPanel<ConfirmQuitPanel>();
            });
        }
    }
}