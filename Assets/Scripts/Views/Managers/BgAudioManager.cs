using Qf.ClassDatas;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models;
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace Qf.Managers
{
    public class BgAudioManager : MonoBehaviour , IController
    {
        [SerializeField]
        AudioSource audioSource;//音频源
        AudioEditModel editModel;
        float _PlaySpeed;
        int Mode;
        private void Awake()
        {

        }
        void Start()
        {
            Init();
            this.RegisterEvent<MainAudioChangeValue>(v => UpdateData()).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<BPMChangeValue>(v => { _PlaySpeed = v.BPM / GetBPM(); audioSource.pitch = _PlaySpeed; }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        public void EnterPlayMode()
        {
            this.SendCommand(new SetAudioEditModeCommand(ClassDatas.AudioEdit.SystemModeData.PlayMode));
        }
        public void EnterEditMode()
        {
            this.SendCommand(new SetAudioEditModeCommand(ClassDatas.AudioEdit.SystemModeData.EditMode));
        }
        public void EnterRecordingMode()
        {
            this.SendCommand(new SetAudioEditModeCommand(ClassDatas.AudioEdit.SystemModeData.RecordingMode));
        }
        void Init()
        {
            editModel = this.GetModel<AudioEditModel>();
            if(editModel.EditAudioClip != null)
            {
                audioSource.clip = editModel.EditAudioClip;
            }
            this.RegisterEvent<OnEditMode>(v =>
            {
                Debug.Log("编辑模式");
                Mode = 0;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<OnPlayMode>(v =>
            {
                Debug.Log("游玩模式");
                Mode = 1;
                PlayMode();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnRecordingMode>(v =>
            {
                Debug.Log("录制模式");
                Mode = 2;
                PlayMode();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<ExitPlayMode>(v =>
            {
                Debug.Log("退出游玩模式");
                ExitPlayMode();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<ExitRecordingMode>(v =>
            {
                Debug.Log("退出录制模式");
                ExitPlayMode();
            });
        }
        void UpdateAll()
        {
            this.SendCommand(new SetAudioEditThisTimeCommand(audioSource.time));
        }
        void PlayMode()
        {
            UpdateData();
            audioSource.Play();
        }
        void ExitPlayMode()
        {
            audioSource.Pause();
        }
        void UpdateData()
        {
            audioSource.time = editModel.ThisTime;
            audioSource.clip = editModel.EditAudioClip;
            this.SendCommand(new SetAudioEditAudioBPMCommand((int)GetBPM()));
        }
        private void Update()
        {
            if (Mode == 0)//编辑模式
            {

            }
            else if (Mode == 1) //游玩模式
            {
                UpdateAll();
            }
            else if (Mode == 2)//录制模式
            {
                UpdateAll();
            }
        }
        
        float GetBPM()//峰值检测法(准确度不高)
        {
            if(audioSource.clip==null) return 0f;
            float sampleRate = audioSource.clip.frequency;
            float timeStep = 1.0f / sampleRate;
            float[] samples = new float[audioSource.clip.samples];
            audioSource.clip.GetData(samples, 0);
            float maxPeak = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) > maxPeak)
                {
                    maxPeak = Mathf.Abs(samples[i]);
                }
            }

            float timeBetweenPeaks = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) > maxPeak * 0.5f)
                {
                    timeBetweenPeaks += timeStep;
                }
            }

            float bpm = 60.0f / timeBetweenPeaks;
            Debug.Log($"[BgAudioManager] BPM:{bpm} 拍数:{60/bpm}");
            return bpm;
        }
        public IArchitecture GetArchitecture()
        {
            return GameBody.Interface;
        }
    }
}