using System;
using Gameplay.Managers;
using UnityEngine;

namespace UI
{
    public class Block : BaseSelectable
    {
        protected override void SetNormal()
        {
            SetSprite(Color.white);
        }

        protected override void SetHighLight()
        {
            SetSprite(new Color(56f/255f, 43f/255f, 38f/255f));
            this.transform.parent.name.RB_Say();
            RhyAudioManager.Instance?.PlayTip();
        }

        private void SetSprite(Color color)
        {
            sprite.color = color;
        }
    }
}