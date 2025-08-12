using System;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Chart
{
    public class RhyChart
    {
        
    }

    [Serializable]
    public abstract class RhyEvent
    {
        public int Timing;

        public virtual void Assign(RhyEvent other)
        {
            Timing = other.Timing;
        }

        public abstract RhyEvent Clone();
    }

    [Serializable]
    public abstract class RhyNote : RhyEvent
    {
        public bool Judged;
        public bool Previewed;
        public float Position;
        
        protected GameObject instance;

        public GameObject Instance
        {
            get => instance;
            set
            {
                if (instance != null) Destroy();
                instance = value;
                transform = value.transform;
                spriteRenderer = value.GetComponent<SpriteRenderer>();
            }
        }

        public Transform transform;
        public SpriteRenderer spriteRenderer;

        protected bool enable;

        public virtual bool Enable
        {
            get => enable;
            set
            {
                if (enable != value)
                {
                    enable = value;
                    if (spriteRenderer != null) spriteRenderer.enabled = enable;
                }
            }
        }

        public abstract void Instantiate();

        public virtual void Destroy()
        {
            if (instance != null) UnityEngine.Object.Destroy(instance);
            instance = null;
        }
    }

    [Serializable]
    public class RhyTapNote : RhyNote
    {
        public RhyTapNote()
        {
        }

        public RhyTapNote(int timing)
        {
            Timing = timing;
        }

        public override RhyEvent Clone()
        {
            return new RhyTapNote()
            {
                Timing = Timing,
            };
        }

        public override void Instantiate()
        {
            Instance = GameObject.Instantiate(RhyTapNoteManager.Instance.TapNotePrefab,
                RhyTapNoteManager.Instance.NoteLayer);
        }
    }
}