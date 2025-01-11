using Qf.ClassDatas;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models;
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Qf.Managers
{
    public class BgAudioManager : MonoBehaviour , IController
    {
        [SerializeField]
        AudioSource audioSource;//��ƵԴ
        AudioEditModel editModel;
        [SerializeField]
        int Mode;
        private void Awake()
        {
                
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
        }
        void ExitPlayMode()
        {
            audioSource.Pause();
        }
        void UpdateData()
        {
            audioSource.time = editModel.ThisTime;
            audioSource.clip = editModel.EditAudioClip;
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
        void Start()
        {
            Init();
            this.RegisterEvent<MainAudioChangeValue>(v => UpdateData()).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        public IArchitecture GetArchitecture()
        {
            return GameBody.Interface;
        }
    }
}