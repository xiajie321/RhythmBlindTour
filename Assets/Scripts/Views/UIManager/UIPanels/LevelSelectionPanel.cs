using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Views.UIManager.UIPanels
{
    public class LevelSelectionPanel : BasePanel
    {
        public List<EventTrigger> levelIndex;
        public EventTrigger prev;
        public EventTrigger next;
        public EventTrigger play;
        public EventTrigger replay;

        public EventTrigger menu;
        protected override void Init()
        {
            
        }
    }
}