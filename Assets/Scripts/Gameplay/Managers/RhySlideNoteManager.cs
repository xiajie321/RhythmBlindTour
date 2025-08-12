using UnityEngine;

namespace Gameplay.Managers
{
    public class RhySlideNoteManager : RhyTapNoteManager
    {
        protected override bool TryJudge()
        {
            return RhyGameplayManager.Instance.IsPlaying;
        }
    }
}