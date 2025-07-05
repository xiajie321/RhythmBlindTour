using Qf.ClassDatas.AudioEdit;
using Qf.Events;
using Qf.Managers;
using Qf.Models.AudioEdit;
using Qf.Systems;
using QFramework;
using UnityEngine;
using System;

public class InputMode : MonoBehaviour, IController
{
    [SerializeField] TheTypeOfOperation Operation;
    AudioEditModel editModel;

    DrumsLoadData drwmsData = new();
    public DrumsLoadData DrwmsData { get => drwmsData; set => drwmsData = value; }

    public float StartTime;
    public float EndTime;
    public float PreAdventTime;

    [SerializeField] float TimeOfExistence;
    [SerializeField] AudioClip _PreAdventClip;
    [SerializeField] AudioClip _SucceedClip;
    [SerializeField] AudioClip _LoseClip;

    public AudioClip PreAdventClip { get => _PreAdventClip; set => _PreAdventClip = value; }
    public AudioClip SuccessClip { get => _SucceedClip; set => _SucceedClip = value; }
    public AudioClip LoseClip { get => _LoseClip; set => _LoseClip = value; }

    public SpriteRenderer SpriteRenderer;
    public bool IsActive = true;
    public bool HasJudged = false;

    private float? realEnterTime = null;
    private float Duration = 0f;
    public bool PauseAutoFail = false;

    private bool isAutoJudge = false;

    [SerializeField]
    public bool IsDemoInputMode { get; private set; } = false;

    public void SetIsDemoInputMode(bool isDemo)
    {
        IsDemoInputMode = isDemo;
    }

    private static readonly System.Collections.Generic.Dictionary<TheTypeOfOperation, float> TypeYOffset = new()
    {
        { TheTypeOfOperation.Click, 1.5f },
        { TheTypeOfOperation.SwipeUp, -0.75f },
        { TheTypeOfOperation.SwipeDown, -1.5f },
        { TheTypeOfOperation.SwipeLeft, 0.75f },
        { TheTypeOfOperation.SwipeRight, 0f }
    };

    private void OnEnable()
    {
        TimeOfExistence = 0;
        mInputModeVisualController.OnPauseInputModeVisual += HandlePauseEvent;
    }

    private void OnDisable()
    {
        mInputModeVisualController.OnPauseInputModeVisual -= HandlePauseEvent;
    }

    private void HandlePauseEvent(bool pause)
    {
        PauseAutoFail = pause;
    }

    void Start()
    {
        editModel = this.GetModel<AudioEditModel>();

        float centerTime = drwmsData.DrwmsData.CenterTime;
        float existence = drwmsData.DrwmsData.VTimeOfExistence;
        float preOffset = drwmsData.DrwmsData.VPreAdventAudioClipOffsetTime;

        StartTime = centerTime - existence / 2f;
        EndTime = centerTime + existence / 2f;
        PreAdventTime = centerTime - preOffset;

        if (TypeYOffset.TryGetValue(Operation, out float offsetY))
        {
            Vector3 pos = transform.localPosition;
            pos.y += offsetY;
            transform.localPosition = pos;
        }

        if (PreAdventClip != null)
        {
            AudioEditManager.Instance.Play(
                new AudioClip[] { PreAdventClip },
                new float[] { drwmsData.MusicData.SPreAdventVolume });
        }

        if (Mathf.Approximately(EndTime, StartTime))
        {
            isAutoJudge = true;
            return;
        }
    }

    void Update()
    {
        if (!editModel.Mode.Equals(SystemModeData.PlayMode) || HasJudged || PauseAutoFail)
            return;

        TimeOfExistence += Time.deltaTime;
        float now = editModel.ThisTime;

        if (isAutoJudge)
        {
            float centerTime = drwmsData.DrwmsData.CenterTime;
            if (now >= centerTime)
            {
                SucceedByManager();
                HasJudged = true;
            }
            return;
        }

        if (now > EndTime)
        {
            Duration = Time.realtimeSinceStartup - (realEnterTime ?? Time.realtimeSinceStartup);
            LoseByManager();
            HasJudged = true;
        }
    }

    public void InitializeTimes(float preAdventTime, float startTime, float endTime)
    {
        PreAdventTime = preAdventTime;
        StartTime = startTime;
        EndTime = endTime;
    }

    public bool ReceiveInput(TheTypeOfOperation inputType)
    {
        if (!editModel.Mode.Equals(SystemModeData.PlayMode) || HasJudged)
            return false;

        if (Mathf.Approximately(StartTime, EndTime))
            return false;

        float now = editModel.ThisTime;

        if (now < StartTime)
        {
            return false;
        }
        else if (now >= StartTime && now <= EndTime)
        {
            if (inputType == Operation)
            {
                SucceedByManager();
                HasJudged = true;
                return true;
            }
            else
            {
                LoseByManager();
                HasJudged = true;
                return false;
            }
        }

        return false;
    }

    public void SucceedByManager()
    {
        if (!editModel.Mode.Equals(SystemModeData.PlayMode)) return;

        AudioEditManager.Instance.Play(
            new AudioClip[] { _SucceedClip },
            new float[] { drwmsData.MusicData.SSucceedVolume });

        this.SendEvent<SucceedTrigger>();
        Destroy(gameObject);
    }

    public void LoseByManager()
    {
        if (!editModel.Mode.Equals(SystemModeData.PlayMode)) return;

        AudioEditManager.Instance.Play(
            new AudioClip[] { _LoseClip },
            new float[] { drwmsData.MusicData.SLoseVolume });

        this.SendEvent<LoseTrigger>();
        Destroy(gameObject);
    }

    public void SetOperation(TheTypeOfOperation type) => Operation = type;
    public TheTypeOfOperation GetOperation() => Operation;

    public IArchitecture GetArchitecture() => GameBody.Interface;
}

public enum TheTypeOfOperation
{
    SwipeUp,
    SwipeDown,
    SwipeLeft,
    SwipeRight,
    Click
}
