using System;
using System.Collections.Generic;
using Gameplay.Managers;
using Gameplay.Managers.Note;
using UnityEngine;

namespace Gameplay.Chart
{
    [Serializable]
    public class RhyChart
    {
        public int AudioOffset;
        public List<RhyTapNote> TapNotes = new();
        public List<RhySlideLeftNote> SlideLeftNotes = new();
        public List<RhySlideRightNote> SlideRightNotes = new();
        public List<RhySlideUpNote> SlideUpNotes = new();
        public List<RhySlideDownNote> SlideDownNotes = new();
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

    [Serializable]
    public class RhySlideLeftNote : RhyNote
    {
        public RhySlideLeftNote()
        {
        }

        public RhySlideLeftNote(int timing)
        {
            Timing = timing;
        }

        public override RhyEvent Clone()
        {
            return new RhySlideLeftNote()
            {
                Timing = Timing,
            };
        }

        public override void Instantiate()
        {
            Instance = GameObject.Instantiate(RhySlideLeftNoteManager.Instance.NotePrefab,
                RhySlideLeftNoteManager.Instance.NoteLayer);
        }
    }

    [Serializable]
    public class RhySlideRightNote : RhyNote
    {
        public RhySlideRightNote()
        {
        }

        public RhySlideRightNote(int timing)
        {
            Timing = timing;
        }

        public override RhyEvent Clone()
        {
            return new RhySlideRightNote()
            {
                Timing = Timing,
            };
        }

        public override void Instantiate()
        {
            Instance = GameObject.Instantiate(RhySlideRightNoteManager.Instance.NotePrefab,
                RhySlideRightNoteManager.Instance.NoteLayer);
        }
    }

    [Serializable]
    public class RhySlideUpNote : RhyNote
    {
        public RhySlideUpNote()
        {
        }

        public RhySlideUpNote(int timing)
        {
            Timing = timing;
        }

        public override RhyEvent Clone()
        {
            return new RhySlideUpNote()
            {
                Timing = Timing,
            };
        }

        public override void Instantiate()
        {
            Instance = GameObject.Instantiate(RhySlideUpNoteManager.Instance.NotePrefab,
                RhySlideUpNoteManager.Instance.NoteLayer);
        }
    }

    [Serializable]
    public class RhySlideDownNote : RhyNote
    {
        public RhySlideDownNote()
        {
        }

        public RhySlideDownNote(int timing)
        {
            Timing = timing;
        }

        public override RhyEvent Clone()
        {
            return new RhySlideUpNote()
            {
                Timing = Timing,
            };
        }

        public override void Instantiate()
        {
            Instance = GameObject.Instantiate(RhySlideDownNoteManager.Instance.NotePrefab,
                RhySlideDownNoteManager.Instance.NoteLayer);
        }
    }
}