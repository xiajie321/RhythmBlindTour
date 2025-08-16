using TestOffset;
using UI;
using UnityEngine.EventSystems;

namespace Views.UIManager.UIPanels
{
    public class OffsetTestPanel : BasePanel
    {
        public Block playBtn;
        public Block quitBtn;

        protected override void Init()
        {
            var v = GetComponent<OffsetController>();
            playBtn.OnClick += () =>
            {
                v.AudioPlayScheduled();
            };
            quitBtn.OnClick += (() =>
            {
                UIManager.Instance.ShowPanel<MainPanel>();
                UIManager.Instance.HidePanel<OffsetTestPanel>();
            });
        }
    }
}