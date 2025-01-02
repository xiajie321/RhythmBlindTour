using Qf.ClassDatas;
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
            this.RegisterEvent<OnPlayMode>(v =>
            {
                Debug.Log("����ģʽ");
                PlayMode();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<ExitPlayMode>(v =>
            {
                Debug.Log("�˳�����ģʽ");
                ExitPlayMode();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnRecordingMode>(v =>
            {
                Debug.Log("¼��ģʽ");
                PlayMode();
            });

            this.RegisterEvent<ExitRecordingMode>(v =>
            {
                Debug.Log("�˳�¼��ģʽ");
                ExitPlayMode();
            });
        }

        void PlayMode()
        {
            audioSource.time = editModel.ThisTime;
            audioSource.clip = editModel.EditAudioClip;
        }
        void ExitPlayMode()
        {
            audioSource.Pause();
        }

        void Start()
        {
            Init();
        }
        public IArchitecture GetArchitecture()
        {
            return GameBody.Interface;
        }
    }
}