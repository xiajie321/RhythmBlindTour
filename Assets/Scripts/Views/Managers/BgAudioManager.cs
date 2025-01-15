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
        AudioSource audioSource;//��ƵԴ
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
                Debug.Log("�༭ģʽ");
                Mode = 0;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<OnPlayMode>(v =>
            {
                Debug.Log("����ģʽ");
                Mode = 1;
                PlayMode();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnRecordingMode>(v =>
            {
                Debug.Log("¼��ģʽ");
                Mode = 2;
                PlayMode();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<ExitPlayMode>(v =>
            {
                Debug.Log("�˳�����ģʽ");
                ExitPlayMode();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<ExitRecordingMode>(v =>
            {
                Debug.Log("�˳�¼��ģʽ");
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
            if (Mode == 0)//�༭ģʽ
            {

            }
            else if (Mode == 1) //����ģʽ
            {
                UpdateAll();
            }
            else if (Mode == 2)//¼��ģʽ
            {
                UpdateAll();
            }
        }
        
        float GetBPM()//��ֵ��ⷨ(׼ȷ�Ȳ���)
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
            Debug.Log($"[BgAudioManager] BPM:{bpm} ����:{60/bpm}");
            return bpm;
        }
        public IArchitecture GetArchitecture()
        {
            return GameBody.Interface;
        }
    }
}