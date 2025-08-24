using Gameplay;
using UI;

namespace Views.UIManager.UIPanels
{
    public class LevelCompletionPanel : BasePanel
    {
        public Block replay;
        public Block back;
        public Block next;

        protected override void Init()
        {
            replay.OnClick += () =>
            {
                UIManager.Instance.HidePanel<LevelCompletionPanel>();
                RhyGameplayManager.Instance.Reset();
                RhyGameplayManager.Instance.Play();
            };
        }
    }
}