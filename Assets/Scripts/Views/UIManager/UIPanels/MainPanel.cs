using UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Views.UIManager.UIPanels
{
    public class MainPanel : BasePanel
    {
        public Block startBtn;
        public Block quitBtn;
        public Block offsetTestBtn;
        public Block settingsBtn;
        public Block listBtn;

        protected override void Init()
        {
            startBtn.OnClick += (() =>
            {
                UIManager.Instance.ShowPanel<LevelSelectionPanel>();
                UIManager.Instance.HidePanel<MainPanel>();
            });

            quitBtn.OnClick += (() => { UIManager.Instance.ShowPanel<ConfirmQuitPanel>(); });

            offsetTestBtn.OnClick += (() =>
            {
                UIManager.Instance.ShowPanel<OffsetTestPanel>();
                UIManager.Instance.HidePanel<MainPanel>();
            });
            settingsBtn.OnClick += (() =>
            {
                UIManager.Instance.ShowPanel<GameSettingsPanel>();
                UIManager.Instance.HidePanel<MainPanel>();
            });
        }
    }
}