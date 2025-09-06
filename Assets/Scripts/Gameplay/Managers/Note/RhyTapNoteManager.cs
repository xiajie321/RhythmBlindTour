using System;
using System.Collections.Generic;
using Gameplay.Chart;
using UnityEngine;

namespace Gameplay.Managers.Note
{
    public class RhyTapNoteManager : MonoBehaviour
    {
        public static RhyTapNoteManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public List<RhyTapNote> Taps = new();
        public float Lanes;
        public GameObject TapNotePrefab;
        public Transform NoteLayer;

        public AudioSource audioSource;
        public AudioClip TapNoteSound;
        public AudioClip TapSuccessSound;
        public AudioClip FailSound;
        public int previewTiming = 1000;
        public int judgeDuration=110;

        public int perfectCount = 0;
        public int missCount = 0;

        private void Start()
        {
            Init();
            // 设置音效音量
            audioSource.volume = PlayerPrefs.GetFloat("IntervalSoundVolume", 40f) / 100f;
        }

        public void Init()
        {
            foreach (var t in Taps)
            {
                if (t.transform != null)
                {
                    DestroyImmediate(t.transform.gameObject);
                }
                t.Destroy();
            }
            
            foreach (var t in Taps)
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
            PlayFailedSound();
            audioSource.volume = PlayerPrefs.GetFloat("TipSoundVolume", 40f) / 100f;
        }
        private void PlayFailedSound()
        {
            foreach (var t in Taps)
            {
                if (t.Judged || !t.Enable) continue;
                if (RhyGameplayManager.Instance.ChartTiming - t.Timing > judgeDuration / 2)
                {
                    t.Judged = true;
                    missCount++;
                    audioSource.PlayOneShot(FailSound);
                    return;
                }
            }
        }
        private void PlayPreviewSound()
        {
            if (RhyGameplayManager.Instance.IsPlaying)
            {
                foreach (var t in Taps)
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
            foreach (var t in Taps)
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
            foreach (var t in Taps)
            {
                t.Judged = false;
                t.Previewed = false;
            }
        }

        protected virtual bool TryJudge()
        {
            return RhyGameplayManager.Instance.IsPlaying && InputManager.Instance.CheckTap();
        }

        private bool JudgeTapNote()
        {
            foreach (var t in Taps)
            {
                if (t.Judged) continue;
                float currentTime = RhyGameplayManager.Instance.ChartTiming;
                float  offset = PlayerPrefs.GetFloat("offset", 0f);
                if (Mathf.Abs(currentTime-offset - t.Timing) < judgeDuration)
                {
                    perfectCount++;
                    t.Judged = true;
                    audioSource.PlayOneShot(TapSuccessSound);
                }
            }

            return true;
        }

    }
}