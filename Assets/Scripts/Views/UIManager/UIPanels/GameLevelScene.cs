using UI;

namespace Views.UIManager.UIPanels
{
    public class GameLevelScene : BasePanel
    {
        public Block menu;

        protected override void Init()
        {
            menu.OnClick += (() => { UIManager.Instance.ShowPanel<GameMenuPanel>(); });
        }
    }
}