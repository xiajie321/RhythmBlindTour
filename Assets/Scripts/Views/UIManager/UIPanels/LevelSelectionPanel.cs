using System.Collections.Generic;
using Gameplay;
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

        public string[] levelPath;
        
        
        public Block back;
        protected override void Init()
        {
            back.OnClick+=(() =>
            {
                UIManager.Instance.ShowPanel<MainPanel>();
            });
            for (int i = 0; i < levelIndex.Count; i++)
            {
                var i1 = i;
                levelIndex[i].OnClick += () =>
                {
                    RhyGameplayManager.Instance.LoadChart(levelPath[i1]);
                };
            }
        }
    }
}