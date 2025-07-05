using Qf.ClassDatas;
using Qf.ClassDatas.AudioEdit;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models;
using Qf.Models.AudioEdit;
using QFramework;
using RhythmTool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
namespace Qf.Managers
{
    public class AudioEditManager : MonoBehaviour, IController
    {
        [SerializeField]
        AudioSource audioSource;//音频源
        [SerializeField]
        List<AudioSource> vfxSource;//音效音频源
        [SerializeField]
        RhythmPlayer rhythmPlayer;//音频处理器
        [SerializeField]
        RhythmAnalyzer rhythmAnalyzer;//音频分析器
        AudioEditModel editModel;
        int Mode;
        public static AudioEditManager Instance;
        private CreateDrumsManager drumsManager;


        // [曝露 ControlRun 的激活状态] --mixyao/25/07/02
        public bool IsControlRunning => audioSource != null && audioSource.isPlaying;

        /// <summary>
        /// 播放特效音
        /// </summary>
        /// <param name="audioClip"></param>
        int index;
        public void Play(AudioClip[] audioClip, float[] volume = null)
        {
            int startindex = index;
            for (int i = 0; i < audioClip.Length; i++)
            {
                if (i >= volume.Length)
                    vfxSource[startindex].volume = 1;
                else
                {
                    vfxSource[startindex].volume = volume[i];
                }

                startindex++;
                if (startindex >= vfxSource.Count)
                    startindex = 0;
            }
            foreach (var i in audioClip)
            {
                vfxSource[index].clip = i;
                vfxSource[index].Play();
                index++;

                if (index >= vfxSource.Count)
                    index = 0;
            }
        }
        public void Play(AudioClip audioClip, float volume = 1)
        {
            vfxSource[index].volume = volume;
            vfxSource[index].clip = audioClip;
            vfxSource[index].Play();
            index++;
            if (index >= vfxSource.Count)
                index = 0;
        }
        public async void OnePlay(AudioClip audioClip)
        {
            vfxSource[0].clip = audioClip;
            vfxSource[0].Play();
            if (audioClip.length >= 3)
            {
                await Task.Delay(3000);
                if (vfxSource[0].clip == audioClip)
                {
                    vfxSource[0].Pause();
                }
            }
        }
        private void Awake()
        {
            Instance = this;
        }
        void Start()
        {
            Init();
            drumsManager = FindObjectOfType<CreateDrumsManager>();
            this.RegisterEvent<MainAudioChangeValue>(v =>
            {
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
        bool isRunGetBPM;
        public async void GetBPM()
        {
            if (isRunGetBPM) return;
            isRunGetBPM = true;
            if (rhythmPlayer.rhythmData == null)
            {
                Debug.Log("[AudioEditManager] 无分析对象");
                return;
            }
            Debug.Log("[AudioEditManager] 等待分析");
            await Task.Delay(3000);
            isRunGetBPM = false;
            if (rhythmPlayer.rhythmData == null)
            {
                Debug.Log("[AudioEditManager] 无分析对象");
                return;
            }
            Track<Beat> ls = rhythmPlayer.rhythmData.GetTrack<Beat>();
            sum = 0;
            for (int i = 0; i < ls.count; i++)
            {
                sum += ls[i].bpm;
            }
            sum /= ls.count;
            Debug.Log($"数据组{ls.count},{sum}");
            this.SendCommand(new SetAudioEditAudioBPMCommand((int)Mathf.Round(sum)));
            return;
        }
        void Init()
        {
            editModel = this.GetModel<AudioEditModel>();
            if (editModel.EditAudioClip != null)
            {
                audioSource.clip = editModel.EditAudioClip;
            }
            this.RegisterEvent<OnEditMode>(v =>
            {
                Debug.Log("切换至-->编辑模式");
                Mode = 0;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<OnPlayMode>(v =>
            {
                Debug.Log("切换至-->游玩模式");
                Mode = 1;
                PlayMode();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnRecordingMode>(v =>
            {
                Debug.Log("切换至-->录制模式");
                Mode = 2;
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
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<OnUpdateThisTime>(v =>
            {
                editModel.ThisTime = v.ThisTime;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<OnStartThisTime>(v =>
            {
                thisTime = v.ThisTime;
                audioSource.time = v.ThisTime;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        float thisTime;
        float ls;
        void UpdateAll()
        {
            thisTime += 0.01f;
            ls += 0.01f;
            if (ls >= 0.01f)
            {
                ls = 0;
                this.SendEvent(new OnUpdateThisTime()
                {
                    ThisTime = (float)(Math.Round(thisTime, 2, MidpointRounding.ToEven))
                });
            }
        }
        void PlayMode()
        {
            UpdateData();
            audioSource.volume = editModel.EditAudioClipVolume.Value;
            audioSource.Play();
        }
        void ExitPlayMode()
        {
            audioSource.Pause();
        }
        /// <summary>
        /// 控制运行,适用于开关
        /// </summary>
        public void ControlRun()
        {
            if (editModel.EditAudioClip == null) return;

            if (audioSource.isPlaying)
            {
                ExitPlayMode();
                mInputModeVisualController.BroadcastPauseToAll(true);  // 事件方式暂停鼓点      //暂停自动失败的计时，以及停止鼓点的移动 - mixyao/06/23 
                return;
            }

            PlayMode();
            mInputModeVisualController.BroadcastPauseToAll(false);     // 事件方式恢复鼓点
        }

        void UpdateData()
        {
            audioSource.time = editModel.ThisTime;
            audioSource.clip = editModel.EditAudioClip;
            rhythmPlayer.rhythmData = rhythmAnalyzer.Analyze(editModel.EditAudioClip);
        }
        private void Update()
        {

        }
        private void FixedUpdate()
        {
            if (audioSource.isPlaying)
                UpdateAll();
        }
        public IArchitecture GetArchitecture()
        {
            return GameBody.Interface;
        }
    }
}