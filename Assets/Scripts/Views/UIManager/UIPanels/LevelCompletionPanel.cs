using Gameplay;
using Gameplay.Managers.Note;
using TMPro;
using UI;
using UnityEngine;

namespace Views.UIManager.UIPanels
{
    public class LevelCompletionPanel : BasePanel
    {
        public Block replay;
        public Block back;
        public Block next;
        public TMP_Text PnM;
        public TMP_Text point;

        protected override void Init()
        {
            replay.OnClick += () =>
            {
                UIManager.Instance.HidePanel<LevelCompletionPanel>();
                RhyGameplayManager.Instance.Reset();
                RhyGameplayManager.Instance.Play();
            };

            back.OnClick += () =>
            {
                UIManager.Instance.HidePanel<LevelCompletionPanel>();
                UIManager.Instance.ShowPanel<LevelSelectionPanel>();
            };
          
            ShowPnM();
        }

        private void ShowPnM()
        {
            int p = 0;
            int m = 0;

            if (RhyTapNoteManager.Instance != null)
            {
                p += RhyTapNoteManager.Instance.perfectCount;
                m += RhyTapNoteManager.Instance.missCount;
            }

            if (RhySlideLeftNoteManager.Instance != null)
            {
                p += RhySlideLeftNoteManager.Instance.perfectCount;
                m += RhySlideLeftNoteManager.Instance.missCount;
            }

            if (RhySlideRightNoteManager.Instance != null)
            {
                p += RhySlideRightNoteManager.Instance.perfectCount;
                m += RhySlideRightNoteManager.Instance.missCount;
            }

            if (RhySlideUpNoteManager.Instance != null)
            {
                p += RhySlideUpNoteManager.Instance.perfectCount;
                m += RhySlideUpNoteManager.Instance.missCount;
            }

            if (RhySlideDownNoteManager.Instance != null)
            {
                p += RhySlideDownNoteManager.Instance.perfectCount;
                m += RhySlideDownNoteManager.Instance.missCount;
            }

            int total = p + m;

            float acc = total > 0 ? (float)p / total : 0f;
            int accPercent = Mathf.RoundToInt(acc * 100f);

            if (PnM != null)
            {
                PnM.text = $"{p}/{total}";
            }

            if (point != null)
            {
                point.text = $"{accPercent*100:N0}";
            }
        }
    }
}