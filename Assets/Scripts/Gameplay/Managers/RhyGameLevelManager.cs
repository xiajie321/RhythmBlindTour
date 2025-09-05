using UnityEngine;
using Views.UIManager.UIPanels;

namespace Gameplay.Managers
{
    public class RhyGameLevelManager : MonoBehaviour
    {
        private void Update()
        {
            if (!RhyGameplayManager.Instance.IsPlaying) return;
            if (InputManager.Instance.inputMap.Gameplay.ESC.WasPressedThisFrame())
            {
                RhyGameplayManager.Instance.Pause();
                Views.UIManager.UIManager.Instance.ShowPanel<GameMenuPanel>();
            }
        }
    }
}