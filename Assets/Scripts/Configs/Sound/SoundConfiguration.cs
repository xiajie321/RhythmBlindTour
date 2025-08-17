using System.Collections.Generic;
using UnityEngine;

namespace Configs.Sound
{
    public class SoundConfiguration
    {
        public enum SoundClipType
        {
            Tap,
            Left,
            Right,
            Up,
            Down
        }

        public class SoundClip
        {
            public AudioClip clip;
            public SoundClipType type;
        }

        [CreateAssetMenu(menuName = "Config/New Sound Configuration")]
        public class SoundConfig : ScriptableObject
        {
            public List<SoundClip> clips;

            public SoundClip FindByType(SoundClipType type)
            {
                return clips.Find(x => x.type == type);
            }
        }
    }
}