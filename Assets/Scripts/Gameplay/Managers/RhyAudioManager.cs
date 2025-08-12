using System;
using UnityEngine;

namespace Gameplay.Managers
{
    public class RhyAudioManager : MonoBehaviour
    {
        public static RhyAudioManager Instance { get;private set; }

        private void Awake()
        {
            Instance = this;
        }

        public AudioSource source;

        public float Timing
        {
            get => source.time;
            set=> source.time = value;
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
    }
}