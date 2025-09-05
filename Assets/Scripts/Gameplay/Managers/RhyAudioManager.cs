using System;
using UnityEngine;

namespace Gameplay.Managers
{
    public class RhyAudioManager : MonoBehaviour
    {
        public static RhyAudioManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // 设置音乐音量
            source.volume = PlayerPrefs.GetFloat("MusicVolume", 40f) / 100f;
        }

        private void Update()
        {
            source.volume = PlayerPrefs.GetFloat("MusicVolume", 40f) / 100f;
            tipSource.volume = PlayerPrefs.GetFloat("IntervalSoundVolume", 40f) / 100f;
        }

        public AudioSource source;
        public AudioClip tipClip;
        public AudioSource tipSource;

        public float Timing
        {
            get => source.time;
            set => source.time = value;
        }

        public AudioClip Clip
        {
            get => source.clip;
            set => source.clip = value;
        }

        public void Load(AudioClip clip)
        {
            Clip = clip;
            Clip.LoadAudioData();
        }

        public void Play()
        {
            source.Play();
        }

        public void Pause()
        {
            source.Pause();
        }

        public void PlayTip()
        {
            tipSource.PlayOneShot(tipClip);
        }
    }
}