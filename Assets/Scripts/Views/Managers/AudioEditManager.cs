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
        public AudioSource audioSource;//音频源
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

        // 新增于public区域
        public AudioSource GetVFXSource(TheTypeOfOperation op)
        {
            int idx = (int)op;
            if (vfxSource != null && idx < vfxSource.Count)
                return vfxSource[idx];
            return vfxSource != null && vfxSource.Count > 0 ? vfxSource[0] : null;
        }
        public void PlayVFXWithFallback(TheTypeOfOperation op, AudioClip clip, float volume = 1f)
        {
            int idx = (int)op;
            if (vfxSource == null || idx >= vfxSource.Count) return;

            var primary = vfxSource[idx];

            // 日志输出：类型、轨道号、音效名、播放器名
            //Debug.Log($"[LOG] {GetOpName(op)}（index={idx}）鼓点分配到扬声器：{primary.name}，播放Clip={clip?.name}");

            primary.volume = volume;
            primary.clip = clip;
            primary.Play();
        }

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

                return;     // 将BPM数据于节拍设置打包并上传，以来“关卡文件设置”，有更加自由的难度把控，而非根据目标音频自动设置。
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

                // ★ 在真正开始播放前，重置鼓点运行态
                if (drumsManager == null) drumsManager = FindObjectOfType<CreateDrumsManager>();
                if (drumsManager != null)
                {
                    var model = this.GetModel<AudioEditModel>();
                    float startAt = model != null ? model.ThisTime : 0f;
                    drumsManager.ResetRuntimeForPlaySession(startAt, destroyExisting: true);
                }

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
                float eventTime = (float)(Math.Round(thisTime, 2, MidpointRounding.ToEven));
                this.SendEvent(new OnUpdateThisTime() { ThisTime = eventTime });
                Debug.Log($"[AudioSyncTest] OnUpdateThisTime sent: eventTime={eventTime:F2}");
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
        /// 【旧】控制运行：播放/暂停切换。
        /// </summary>
        [Obsolete("请使用 PauseRun()/ResumeRun() 替代，以便直接控制播放状态并与 IsControlRunning 对齐。")]
        public void ControlRun()
        {
            if (editModel.EditAudioClip == null) return;

            if (audioSource.isPlaying)
            {
                PauseRun();
                return;
            }

            ResumeRun();
        }

        /// <summary>
        /// 暂停（仅当当前处于播放状态时生效）。会广播暂停给可视化控制器。
        /// </summary>
        public void PauseRun()
        {
            if (audioSource == null) return;
            if (!audioSource.isPlaying) return;

            ExitPlayMode();
            mInputModeVisualController.BroadcastPauseToAll(true);  // 事件方式暂停鼓点
        }

        /// <summary>
        /// 继续（仅当当前处于暂停状态时生效）。会广播恢复给可视化控制器。
        /// </summary>
        public void ResumeRun()
        {
            if (editModel == null || editModel.EditAudioClip == null) return;
            if (audioSource != null && audioSource.isPlaying) return;

            PlayMode();
            mInputModeVisualController.BroadcastPauseToAll(false); // 事件方式恢复鼓点
        }

        /// <summary>
        /// （可选）根据目标状态直接设置运行状态：true=继续，false=暂停。
        /// </summary>
        public void SetControlRunning(bool run)
        {
            if (run) ResumeRun();
            else PauseRun();
        }

        void UpdateData()
        {
            audioSource.time = editModel.ThisTime;
            audioSource.clip = editModel.EditAudioClip;
            rhythmPlayer.rhythmData = rhythmAnalyzer.Analyze(editModel.EditAudioClip);
        }

        private float lastThisTime = 0f; // 在类内作为字段保存
        private bool wasPlaying = false; // 仅在类内定义一次

        private void Update()
        {
            if (!IsControlRunning)
                return;

            bool isNowPlaying = audioSource.isPlaying;

            // 刚开始播放（从暂停→播放的瞬间）时，把 lastThisTime 对齐
            if (isNowPlaying && !wasPlaying)
            {
                lastThisTime = audioSource.time;
            }

            float nowTime = audioSource.time;
            float roundedLast = (float)Math.Round(lastThisTime, 2, MidpointRounding.ToEven);
            float roundedNow = (float)Math.Round(nowTime, 2, MidpointRounding.ToEven);

            // 【关键点】无论播放或SetTime，lastThisTime和audioSource.time不一致都立即补发一次事件
            if (roundedNow != roundedLast)
            {
                float start = Math.Min(roundedLast, roundedNow);
                float end = Math.Max(roundedLast, roundedNow);
                float step = roundedNow > roundedLast ? 0.01f : -0.01f;

                // 向前或向后都能同步
                for (float t = start + step; (step > 0 ? t <= end : t >= end); t += step)
                {
                    float timePoint = (float)Math.Round(t, 2, MidpointRounding.ToEven);
                    this.SendEvent(new OnUpdateThisTime() { ThisTime = timePoint });
                }

                lastThisTime = nowTime;
            }

            wasPlaying = isNowPlaying; // 更新历史状态
        }

        public void SetLastThisTime(float t)
        {
            lastThisTime = t;
        }

        public IArchitecture GetArchitecture()
        {
            return GameBody.Interface;
        }

        public static string GetOpName(TheTypeOfOperation op)
        {
            return op switch
            {
                TheTypeOfOperation.Click => "点击",
                TheTypeOfOperation.SwipeUp => "上滑",
                TheTypeOfOperation.SwipeDown => "下滑",
                TheTypeOfOperation.SwipeLeft => "左滑",
                TheTypeOfOperation.SwipeRight => "右滑",
                _ => "未知"
            };
        }
    }
}
