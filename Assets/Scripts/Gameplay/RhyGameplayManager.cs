using System;
using Gameplay.Chart;
using Gameplay.Managers;
using Gameplay.Managers.Note;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay
{
    public class RhyGameplayManager : MonoBehaviour
    {
        public static RhyGameplayManager Instance { get; private set; }
        public RhyChart chart;

        private void Awake()
        {
            Instance = this;
            //Test
            Length = 10000;
            //
        }

        #region TimeAbout

        //相对于游戏开始（关卡开始）的时间
        private double audioTiming;

        public int AudioTiming
        {
            get => (int)Math.Round(audioTiming * 1000d);
            set
            {
                audioTiming = value / 1000d;
                //other
            }
        }

        //相对于音频片段开始的时间
        public double AudioTimingWithoutGlobalOffset
        {
            get => audioTiming + GlobalAudioOffset / 1000f;
            set => audioTiming = value - GlobalAudioOffset / 1000f;
        }

        //谱面音频的偏移
        public int ChartAudioOffset { get; set; }

        //相对于音频片段开始的偏移
        public int GlobalAudioOffset { get; set; }

        //谱面时间
        public int ChartTiming
        {
            get => AudioTiming - ChartAudioOffset;
            set => AudioTiming = value + ChartAudioOffset;
        }

        //持续时间
        public float Length { get; private set; }

        #endregion

        #region StateAbout

        public bool IsPlaying { get; set; }

        #endregion

        #region 事件

        public UnityEvent OnMusicEnd = new();

        #endregion

        private double lastDspTime;
        private double deltaDspTime;

        private void Update()
        {
            deltaDspTime = AudioSettings.dspTime - lastDspTime;
            lastDspTime = AudioSettings.dspTime;
            if (IsPlaying)
            {
                AudioTimingWithoutGlobalOffset += deltaDspTime;
                if (AudioTimingWithoutGlobalOffset <= Length)
                {
                    float t = RhyAudioManager.Instance.Timing;
                    if (deltaDspTime > 0 && (AudioTimingWithoutGlobalOffset >= 0 || t > 0))
                    {
                        double delta = t - AudioTimingWithoutGlobalOffset;
                        //处理缓存导致的偏差，超过2帧就硬同步
                        AudioSettings.GetDSPBufferSize(out int bufferSize, out _);
                        var maxDelta = 2f * bufferSize / AudioSettings.outputSampleRate;
                        if (Math.Abs(delta) > maxDelta)
                        {
                            AudioTimingWithoutGlobalOffset = t;
                        }
                    }
                }
            }

            if (AudioTiming > Length)
            {
                OnMusicEnd.Invoke();
            }
        }

        public void Play()
        {
            IsPlaying = true;
            RhyAudioManager.Instance.Play();
        }

        public void Pause()
        {
            IsPlaying = false;
            RhyAudioManager.Instance.Pause();
        }

        public void Stop()
        {
            IsPlaying = false;
            audioTiming = 0;
            RhyAudioManager.Instance.Pause();
            RhyAudioManager.Instance.Timing = 0;
            RhyTapNoteManager.Instance.ResetJudgeAndPreview();
            RhySlideLeftNoteManager.Instance.ResetJudgeAndPreview();
            RhySlideRightNoteManager.Instance.ResetJudgeAndPreview();
            RhySlideUpNoteManager.Instance.ResetJudgeAndPreview();
            RhySlideDownNoteManager.Instance.ResetJudgeAndPreview();
        }

        public void LoadChart()
        {
            chart = new();
            chart = JsonMgr.Instance.LoadData<RhyChart>("test");
            RhyTapNoteManager.Instance.Taps = chart.TapNotes;
            RhyTapNoteManager.Instance.Init();
        }
    }
}