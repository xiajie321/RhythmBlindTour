using System.Collections.Generic;
using Gameplay.Chart;
using UnityEngine;

namespace Gameplay.Managers.Note
{
    public class RhySlideDownNoteManager : MonoBehaviour
    {
        public static RhySlideDownNoteManager Instance { get;private set; }

        private void Awake()
        {
            Instance = this;
        }

        public List<RhySlideDownNote> Notes = new();
        public float Lanes;
        public GameObject NotePrefab;
        public Transform NoteLayer;

        public AudioSource audioSource;
        public AudioClip TapNoteSound;
        public AudioClip TapSuccessSound;
        public int previewTiming = 1000;
        public int judgeDuration=110;

        public void Init()
        {
            foreach (var t in Notes)
            {
                t.Instantiate();
            }
        }

        private void Update()
        {
            PlayPreviewSound();
            UpdateRender();
            if (TryJudge())
            {
                JudgeTapNote();
            }
        }

        private void PlayPreviewSound()
        {
            if (RhyGameplayManager.Instance.IsPlaying)
            {
                foreach (var t in Notes)
                {
                    if (t.Previewed) continue;
                    int delta = Mathf.Abs(t.Timing - RhyGameplayManager.Instance.ChartTiming);
                    if (delta < previewTiming)
                    {
                        audioSource.PlayOneShot(TapNoteSound);
                        t.Previewed = true;
                    }
                }
            }
        }

        private void UpdateRender()
        {
            foreach (var t in Notes)
            {
                if (t.Judged)
                {
                    t.Enable = false;
                    continue;
                }

                t.Enable = true;
                t.Position = RhyTimingManager.Instance.CalculatePositionByTiming(t.Timing);
                float y = Lanes;
                t.transform.localPosition = new Vector3(t.Position / 1000f, y, 0);
            }
        }

        public void ResetJudgeAndPreview()
        {
            foreach (var t in Notes)
            {
                t.Judged = false;
                t.Previewed = false;
            }
        }

        private bool JudgeTapNote()
        {
            foreach (var t in Notes)
            {
                if (t.Judged) continue;
                float currentTime = RhyGameplayManager.Instance.ChartTiming;
                if (Mathf.Abs(currentTime - t.Timing) < judgeDuration)
                {
                    t.Judged = true;
                    audioSource.PlayOneShot(TapSuccessSound);
                }
            }

            return true;
        }

        private bool TryJudge()
        {
            return RhyGameplayManager.Instance.IsPlaying && InputManager.Instance.CheckSlideDown();
        }
    }
}