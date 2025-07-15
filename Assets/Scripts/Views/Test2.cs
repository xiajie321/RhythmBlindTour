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
    [Header("节拍BPM UI")]
    public TMP_Dropdown dropdownBeatA;
    public TMP_Dropdown dropdownBeatB;
    public TMP_InputField inputBPM;

    [Header("UI 进度条/面板")]
    public Slider loadingSlider;
    public GameObject loadingPanel;

    public mUIDrumsInspectorPanel inspectorPanel;
    public UIAudioEditDrumsOrbit drumsOrbit;

    private float sliderTarget = 0f;
    private float sliderSpeed = 2f; // 控制平滑速度

    // 下拉选项
    private readonly int[] beatBOptions = { 1, 2, 4, 8 };

    private void Start()
    {
        // 初始化下拉选项（如已在编辑器设置可省略）
        if (dropdownBeatA != null)
        {
            dropdownBeatA.ClearOptions();
            dropdownBeatA.AddOptions(new List<string> { "1", "2", "3", "4", "5", "6" });
        }
        if (dropdownBeatB != null)
        {
            dropdownBeatB.ClearOptions();
            dropdownBeatB.AddOptions(new List<string> { "1", "2", "4", "8" });
        }

        // 可选：监听UI变更时写入本地model（如需要“实时”同步时才用，当前保存时统一写入即可）
        // dropdownBeatA.onValueChanged.AddListener(i => { ... });
        // dropdownBeatB.onValueChanged.AddListener(i => { ... });
        // inputBPM.onValueChanged.AddListener(v => { ... });

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

        // 读取后，刷新UI到model最新数据
        SyncModelToUI();
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
        // UI写回model
        var mdl = this.GetModel<AudioEditModel>();
        mdl.BeatA = dropdownBeatA.value + 1;
        mdl.BeatB = beatBOptions[Mathf.Clamp(dropdownBeatB.value, 0, beatBOptions.Length - 1)];
        int bpm = 60;
        int.TryParse(inputBPM.text, out bpm);
        mdl.BPM = bpm;

        mdl.Save();
    }

    public void Load()
    {
        inspectorPanel.ClearAll();
        drumsOrbit.ClearAllDrwmsUI();

        this.GetModel<AudioEditModel>().Load();

        // 关卡读取后，model同步到UI
        SyncModelToUI();

        inspectorPanel.RefreshList();
        drumsOrbit.ClearAllDrwmsUI();
    }

    /// <summary>
    /// 关卡数据读取后将model字段刷到UI控件
    /// </summary>
    private void SyncModelToUI()
    {
        var mdl = this.GetModel<AudioEditModel>();
        if (dropdownBeatA != null)
            dropdownBeatA.value = Mathf.Clamp(mdl.BeatA - 1, 0, dropdownBeatA.options.Count - 1);
        if (dropdownBeatB != null)
        {
            int bIdx = System.Array.IndexOf(beatBOptions, mdl.BeatB);
            dropdownBeatB.value = bIdx == -1 ? 0 : bIdx;
        }
        if (inputBPM != null)
            inputBPM.text = mdl.BPM.ToString();
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
