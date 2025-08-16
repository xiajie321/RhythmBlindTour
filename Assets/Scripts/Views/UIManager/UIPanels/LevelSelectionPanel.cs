using System.Collections.Generic;
using UI;
using UnityEngine.EventSystems;

namespace Views.UIManager.UIPanels
{
    public class LevelSelectionPanel : BasePanel
    {
        public List<Block> levelIndex;
        public Block prev;
        public Block next;
        public Block play;
        public Block replay;

        public Block menu;
        protected override void Init()
        {
            menu.OnClick+=(() =>
            {
                UIManager.Instance.ShowPanel<GameMenuPanel>();
            });
        }
    }
}