using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Qf.Querys.AudioEdit;

public class Test2 : MonoBehaviour, IController
{
    private void Start()
    {
        StartCoroutine(DelayedAutoLoad());
    }

    private System.Collections.IEnumerator DelayedAutoLoad()
    {
        // 等待 QFramework 初始化完成
        yield return new WaitUntil(() => GameBody.Interface != null);

        AudioEditModel model = null;

        while (model == null)
        {
            try
            {
                model = this.GetModel<AudioEditModel>();
            }
            catch { }
            yield return null;
        }

        TryAutoLoadLatestLevel(model);
    }

    private void OnEnable()
    {
        this.RegisterEvent<SelectOptions>(v =>
        {
            Debug.Log($"{v.SelectObject.name}");
        }).UnRegisterWhenDisabled(gameObject);
    }

    public void Save()
    {
        this.GetModel<AudioEditModel>().Save();
    }

    public void Load()
    {
        this.GetModel<AudioEditModel>().Load();
    }

    public void Run()
    {
        this.SendEvent<TestEvent>();
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }

    /// <summary>
    /// 自动从目标目录读取最近修改的 .Level 文件
    /// </summary>
    private void TryAutoLoadLatestLevel(AudioEditModel model)
    {
        string levelRoot = Path.Combine(Application.streamingAssetsPath, "Levels");

        if (!Directory.Exists(levelRoot))
        {
            Debug.LogWarning($"[AutoLoad] Level directory not found: {levelRoot}");
            return;
        }

        var allLevelFiles = Directory.GetFiles(levelRoot, "*.Level", SearchOption.AllDirectories);
        if (allLevelFiles.Length == 0)
        {
            Debug.Log("[AutoLoad] No .Level file found.");
            return;
        }

        string latestFile = allLevelFiles
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .First();

        Debug.Log($"[AutoLoad] Loading latest level file: {latestFile}");

        var audioSaveData = this.GetUtility<Storage>().Load<AudioSaveData>(latestFile, true);
        if (audioSaveData == null)
        {
            Debug.LogError("[AutoLoad] Failed to load AudioSaveData.");
            return;
        }

        model.EditAudioClipVolume.Value = audioSaveData.EditAudioClipVolume;
        model.TipOffset.Value = audioSaveData.TipOffset;
        model.ThisTime = audioSaveData.ThisTime;
        model.TimeLineData = audioSaveData.TimeLineData;
        model.TimeOfExistence.Value = audioSaveData.TimeOfExistence;

        model.EditAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.EditAudioClip));
        model.DownSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.DownSucceedAudioClip));
        model.UpSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.UpSucceedAudioClip));
        model.LeftSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.LeftSucceedAudioClip));
        model.RightSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.RightSucceedAudioClip));
        model.ClickSucceedAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.ClickSucceedAudioClip));
        model.LoseAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.LoseAudioClip));

        model.DownTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.DownTipsAudioClip));
        model.UpTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.UpTipsAudioClip));
        model.LeftTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.LeftTipsAudioClip));
        model.RightTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.RightTipsAudioClip));
        model.ClickTipsAudioCLip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.ClickTipsAudioCLip));

        this.SendEvent<OnUpdateAudioEditDrumsUI>();
        this.SendEvent<AudioEditModelLoad>();
    }

}
