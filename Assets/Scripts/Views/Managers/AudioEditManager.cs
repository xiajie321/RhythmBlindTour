using Qf.ClassDatas;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models;
using Qf.Models.AudioEdit;
using QFramework;
using RhythmTool;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
namespace Qf.Managers
{
    public class AudioEditManager : MonoBehaviour , IController
    {
        [SerializeField]
        AudioSource audioSource;//��ƵԴ
        [SerializeField]
        RhythmPlayer rhythmPlayer;//��Ƶ������
        [SerializeField]
        RhythmAnalyzer rhythmAnalyzer;//��Ƶ������
        AudioEditModel editModel;
        int Mode;
        private void Awake()
        {

        }
        void Start()
        {
            Init();
            this.RegisterEvent<MainAudioChangeValue>(v => {
                UpdateData();
                GetBPM();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
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
        float sum;
        public async void GetBPM()
        {
            if(rhythmPlayer.rhythmData == null)
            {
                Debug.Log("[AudioEditManager] �޷�������");
                return;
            }
            Debug.Log("[AudioEditManager] �ȴ�����");
            await Task.Delay(3000);
            if (rhythmPlayer.rhythmData == null)
            {
                Debug.Log("[AudioEditManager] �޷�������");
                return;
            }
            Track<Beat> ls = rhythmPlayer.rhythmData.GetTrack<Beat>();
            sum = 0;
            for (int i = 0; i < ls.count; i++)
            {
                sum += ls[i].bpm;
            }
            sum /= ls.count;
            Debug.Log($"������{ls.count},{sum}");
            this.SendCommand(new SetAudioEditAudioBPMCommand((int)Mathf.Round(sum)));
            return;
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
            this.SendCommand(new SetAudioEditThisTimeCommand(audioSource.time)); //�������ʹ���ֶ��Ż���Ϊ��һֱ��������
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
        /// <summary>
        /// ��������,�����ڿ���
        /// </summary>
        public void ControlRun()
        {
            if (editModel.EditAudioClip == null) return;
            if (audioSource.isPlaying)
            {
                ExitPlayMode();
                return;
            }
            PlayMode();
        }
        void UpdateData()
        {
            audioSource.time = editModel.ThisTime;
            audioSource.clip = editModel.EditAudioClip;
            rhythmPlayer.rhythmData = rhythmAnalyzer.Analyze(editModel.EditAudioClip);
        }
        private void Update()
        {
            if(audioSource.isPlaying)
                UpdateAll();
        }
        
        public IArchitecture GetArchitecture()
        {
            return GameBody.Interface;
        }
    }
}