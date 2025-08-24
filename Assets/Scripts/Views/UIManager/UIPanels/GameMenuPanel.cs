using Gameplay;
using UI;
using UnityEngine.EventSystems;

namespace Views.UIManager.UIPanels
{
    public class GameMenuPanel : BasePanel
    {
        public Block continueBtn;
        public Block backBtn;
        public Block skipBtn;
        public Block offsetTestBtn;
        public Block settingsBtn;
        public Block quitBtn;

        protected override void Init()
        {
            continueBtn.OnClick += () =>
            {
                UIManager.Instance.HidePanel<GameMenuPanel>();
                RhyGameplayManager.Instance.Play();
            };

            backBtn.OnClick += () =>
            {
                UIManager.Instance.ShowPanel<LevelSelectionPanel>();
                UIManager.Instance.HidePanel<GameMenuPanel>();
            };

            offsetTestBtn.OnClick += () =>
            {
                UIManager.Instance.ShowPanel<OffsetTestPanel>();
                UIManager.Instance.HidePanel<GameMenuPanel>();
            };
            settingsBtn.OnClick += () =>
            {
                UIManager.Instance.ShowPanel<GameSettingsPanel>();
                UIManager.Instance.HidePanel<GameMenuPanel>();
            };
            quitBtn.OnClick += () =>
            {
                UIManager.Instance.ShowPanel<ConfirmQuitPanel>();
            };
        }
    }
}