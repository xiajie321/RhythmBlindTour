using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Qf.Querys.AudioEdit;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Test2 : MonoBehaviour, IController
{
    [Header("UI 进度条/面板")]
    public Slider loadingSlider;
    public GameObject loadingPanel;

    public mUIDrumsInspectorPanel inspectorPanel;
    public UIAudioEditDrumsOrbit drumsOrbit;

    [Header("节拍与BPM设置管理器")]
    public mBeatSetManager beatSetManager; // 新增引用，需在Inspector拖拽

    private float sliderTarget = 0f;
    private float sliderSpeed = 2f; // 控制平滑速度

    private void Start()
    {
        // 其余UI初始化已移交到 mBeatSetManager
        StartCoroutine(DelayedAutoLoad());
    }

    private IEnumerator DelayedAutoLoad()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (loadingSlider != null) loadingSlider.value = 0f;

        sliderTarget = 0f;
        float actualProgress = 0f;
        float minLoadTime = 2f;
        float startTime = Time.realtimeSinceStartup;

        Coroutine sliderCoroutine = StartCoroutine(SmoothSliderRoutine());

        yield return new WaitUntil(() => GameBody.Interface != null);
        actualProgress = 0.1f; sliderTarget = actualProgress;

        AudioEditModel model = null;
        while (model == null)
        {
            try { model = this.GetModel<AudioEditModel>(); }
            catch { }
            yield return null;
        }
        actualProgress = 0.2f; sliderTarget = actualProgress;

        yield return StartCoroutine(TryAutoLoadLatestLevelWithProgress(model, p => { actualProgress = p; sliderTarget = actualProgress; }));

        sliderTarget = 1f;
        float remain = Mathf.Max(0, minLoadTime - (Time.realtimeSinceStartup - startTime));
        float fillDuration = Mathf.Max(remain, 0.3f); // 最后阶段至少0.3秒推满
        float before = loadingSlider != null ? loadingSlider.value : 0.99f;
        float t = 0f;
        while (loadingSlider != null && loadingSlider.value < 0.999f)
        {
            t += Time.deltaTime / fillDuration;
            loadingSlider.value = Mathf.Lerp(before, 1f, t);
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (sliderCoroutine != null) StopCoroutine(sliderCoroutine);
    }

    private IEnumerator SmoothSliderRoutine()
    {
        while (loadingPanel == null || loadingPanel.activeSelf)
        {
            if (loadingSlider != null)
                loadingSlider.value = Mathf.MoveTowards(loadingSlider.value, sliderTarget, sliderSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator TryAutoLoadLatestLevelWithProgress(AudioEditModel model, System.Action<float> setProgress)
    {
        float stepBase = 0.2f;
        setProgress?.Invoke(stepBase);

        string levelRoot = Path.Combine(Application.streamingAssetsPath, "Levels");

        yield return new WaitForSeconds(0.1f); // 模拟耗时
        setProgress?.Invoke(stepBase + 0.1f);

        if (!Directory.Exists(levelRoot))
        {
            Debug.LogWarning($"[AutoLoad] Level directory not found: {levelRoot}");
            setProgress?.Invoke(1f);
            yield break;
        }
        setProgress?.Invoke(stepBase + 0.2f);

        var allLevelFiles = Directory.GetFiles(levelRoot, "*.Level", SearchOption.AllDirectories);
        yield return null;
        setProgress?.Invoke(stepBase + 0.3f);

        if (allLevelFiles.Length == 0)
        {
            Debug.Log("[AutoLoad] No .Level file found.");
            setProgress?.Invoke(1f);
            yield break;
        }
        setProgress?.Invoke(stepBase + 0.35f);

        string latestFile = allLevelFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First();
        Debug.Log($"[AutoLoad] Loading latest level file: {latestFile}");

        yield return null;
        setProgress?.Invoke(stepBase + 0.4f);

        var audioSaveData = this.GetUtility<Storage>().Load<AudioSaveData>(latestFile, true);

        if (audioSaveData == null)
        {
            Debug.LogError("[AutoLoad] Failed to load AudioSaveData.");

            // 自动应用默认音频设置
            var defaultInit = FindObjectOfType<mDefaultAudioInitializer>();
            if (defaultInit != null)
            {
                defaultInit.DoDefaultAudioInit();
                Debug.Log("[AutoLoad] Default audio settings initialized by mDefaultAudioInitializer.");
            }
            setProgress?.Invoke(1f);
            yield break;
        }

        // 数据同步阶段
        setProgress?.Invoke(stepBase + 0.6f);
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
        model.DefaultAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.DefaultAudioClip));
        model.DownTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.DownTipsAudioClip));
        model.UpTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.UpTipsAudioClip));
        model.LeftTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.LeftTipsAudioClip));
        model.RightTipsAudioClip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.RightTipsAudioClip));
        model.ClickTipsAudioCLip = model.SendQuery(new QueryAudioEditLoadAudio(audioSaveData.ClickTipsAudioCLip));

        setProgress?.Invoke(stepBase + 0.8f);

        this.SendEvent<OnUpdateAudioEditDrumsUI>();
        this.SendEvent<AudioEditModelLoad>();

        yield return null;
        setProgress?.Invoke(0.98f);

        // 读取后，刷新节拍UI到model最新数据
        if (beatSetManager != null)
            beatSetManager.Init(model);
    }

    public void Load()
    {
        inspectorPanel.ClearAll();
        drumsOrbit.ClearAllDrwmsUI();

        this.GetModel<AudioEditModel>().Load();

        // 移除此处的直接同步，由事件驱动
        // if (beatSetManager != null)
        //     beatSetManager.SyncModelToUI();

        inspectorPanel.RefreshList();
        drumsOrbit.ClearAllDrwmsUI();
    }

    private void OnEnable()
    {
        this.RegisterEvent<SelectOptions>(v =>
        {
            Debug.Log($"{v.SelectObject.name}");
        }).UnRegisterWhenDisabled(gameObject);

        // 新增：监听数据加载事件刷新UI
        this.RegisterEvent<AudioEditModelLoad>(v =>
        {
            if (beatSetManager != null)
                beatSetManager.SyncModelToUI();
        }).UnRegisterWhenDisabled(gameObject);
    }

    public void Save()
    {
        var mdl = this.GetModel<AudioEditModel>();
        if (beatSetManager != null)
            beatSetManager.WriteBackToModel();
        mdl.Save();
    }



    /// <summary>
    /// 仅用于兼容旧调用，不再负责节拍UI刷新
    /// </summary>
    private void SyncModelToUI()
    {
        if (beatSetManager != null)
            beatSetManager.SyncModelToUI();
    }

    public void Run()
    {
        this.SendEvent<TestEvent>();
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
