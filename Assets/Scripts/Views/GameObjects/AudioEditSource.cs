using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using Qf.Events;
using System;
using Qf.Models.AudioEdit;
using Cysharp.Threading.Tasks;

public class AudioEditSource : MonoBehaviour, IController
{
    private AudioSource audioSource;
    private AudioEditModel model;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        model = this.GetModel<AudioEditModel>();
        this.RegisterEvent<AudioEditChangeEvent>(OnEditAudioChange);
        this.RegisterEvent<AudioEditTimeChangeEvent>(v=>OnEditTimeChange());
    }

    private void OnEditAudioChange(AudioEditChangeEvent obj)
    {
        audioSource.clip = obj.clip;
    }

    private async UniTaskVoid OnEditTimeChange()
    {
        if (audioSource.clip == null)
            return;
        audioSource.time = model.ThisTime;
        if (!audioSource.isPlaying)
        {
            audioSource.Play(); 
        }
        await UniTask.Delay(10);
        audioSource.Stop();

    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
