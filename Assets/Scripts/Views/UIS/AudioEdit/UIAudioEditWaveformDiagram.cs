using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 稳定版音乐波形图绘制（支持 Mask & 左对齐）
/// - 延迟到 AudioClip Loaded 再绘制
/// - 列采样算法修正：考虑声道，列宽与纹理宽 1:1
/// - 仅波形像素着色，其他像素全透明（可被 UI Mask 裁剪）
/// - 生成后强制左对齐（锚点/枢轴为左边，Left=0）
/// - 广播 OnWaveformReady(AudioLength, PixelWidth)
/// </summary>
public class UIAudioEditWaveformDiagram : MonoBehaviour, IController
{
    // 向外广播“波形已就绪”，便于 Orbit 同步轨道长度
    public struct OnWaveformReady
    {
        public float AudioLength; // 秒
        public int PixelWidth;    // width = 秒 * PUPS
    }

    [Header("显示目标")]
    [SerializeField] private Image musicWaveFormDiagramShow;      // 目标 Image（必须）
    [SerializeField] private bool maskable = true;                // 是否参与 UI Mask
    [SerializeField] private bool raycastTarget = false;          // 是否阻挡点击

    [Header("视觉")]
    [SerializeField] private Color waveFormDiagramColor = Color.yellow;     // 波形颜色
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0); // 背景透明

    [Header("尺寸/缩放")]
    [SerializeField] private int pixelUnitsPerSecond = -1; // 每秒像素；<=0 则用 AudioEditConfig.PixelUnitsPerSecond
    [SerializeField] private int editHeight = -1;          // 纹理高度；<=0 则用 AudioEditConfig.EditHeight
    [SerializeField] private bool setNativeSize = false;   // 生成后是否调用 SetNativeSize()

    private AudioEditModel model;
    private AudioClip lastAudioClip;
    private Coroutine buildRoutine;

    // 便捷：从配置取默认
    int PUPS => (pixelUnitsPerSecond > 0) ? pixelUnitsPerSecond : AudioEditConfig.PixelUnitsPerSecond;
    int EditHeight => (editHeight > 0) ? editHeight : Mathf.Max(2, AudioEditConfig.EditHeight);

    void Start()
    {
        model = this.GetModel<AudioEditModel>();

        if (musicWaveFormDiagramShow == null)
            musicWaveFormDiagramShow = GetComponent<Image>();

        if (musicWaveFormDiagramShow != null)
        {
            musicWaveFormDiagramShow.maskable = maskable;
            musicWaveFormDiagramShow.raycastTarget = raycastTarget;
        }

        // 从 Level/SO 注入完成后也触发
        this.RegisterEvent<AudioEditModelLoad>(_ => Rebuild(force: true))
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // 主音频变更触发
        this.RegisterEvent<MainAudioChangeValue>(_ => Rebuild(force: true))
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // 启动时自检一次（避免错过事件）
        Rebuild(force: false);
    }

    /// <summary>对外统一入口：请求重建波形</summary>
    public void Rebuild(bool force)
    {
        if (buildRoutine != null) StopCoroutine(buildRoutine);
        buildRoutine = StartCoroutine(BuildWhenReady(force));
    }

    private IEnumerator BuildWhenReady(bool force)
    {
        // 等模型就绪
        yield return new WaitUntil(() => model != null);

        var clip = model.EditAudioClip;
        if (clip == null)
        {
            ClearImage();
            Debug.Log("[Waveform] 没有主音频可生成波形。");
            yield break;
        }

        // 同一 clip 且不强制 → 跳过
        if (!force && clip == lastAudioClip)
        {
            Debug.Log("[Waveform] 重复音频不做波形重建。");
            yield break;
        }

        // 等待音频加载（Streaming/未预加载时）
        if (clip.loadState == AudioDataLoadState.Unloaded || clip.loadState == AudioDataLoadState.Failed)
            clip.LoadAudioData();

        float waitStart = Time.realtimeSinceStartup;
        while (clip.loadState == AudioDataLoadState.Loading)
        {
            if (Time.realtimeSinceStartup - waitStart > 3f) break; // 最多等 3 秒
            yield return null;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning($"[Waveform] AudioClip 未就绪(loadState={clip.loadState})，无法生成波形。建议勾选 Preload，Load Type 设为 Decompress On Load。");
            yield break;
        }

        // 再等一帧，确保 RectTransform 布局稳定（避免宽度为 0）
        yield return null;

        BuildTextureFromClip(clip);
        lastAudioClip = clip;
        buildRoutine = null;
    }

    private void BuildTextureFromClip(AudioClip music)
    {
        if (musicWaveFormDiagramShow == null)
        {
            Debug.LogWarning("[Waveform] 未绑定显示 Image。");
            return;
        }

        // 尺寸
        int width = Mathf.Max(1, Mathf.CeilToInt(music.length * PUPS));
        int height = Mathf.Max(2, EditHeight);

        // 采样基础信息
        int channels = Mathf.Max(1, music.channels);
        int freq = Mathf.Max(1, music.frequency);

        // 每列对应的样本数（所有声道合计）
        int samplesPerSecondAllCh = freq * channels;
        int stride = Mathf.Max(1, Mathf.RoundToInt((1f / PUPS) * samplesPerSecondAllCh)); // 一列对应的样本数

        // 取样
        float[] sampling = new float[music.samples * channels];
        music.GetData(sampling, 0);

        // 列聚合：平均绝对值
        int columns = width;
        float[] colValues = new float[columns];
        float maxVal = 0f;

        int total = sampling.Length;
        for (int x = 0; x < columns; x++)
        {
            int start = x * stride;
            int end = Mathf.Min(start + stride, total);
            if (start >= end) { colValues[x] = 0f; continue; }

            float sumAbs = 0f;
            for (int i = start; i < end; i++)
                sumAbs += Mathf.Abs(sampling[i]);

            float avgAbs = sumAbs / (end - start);
            colValues[x] = avgAbs;
            if (avgAbs > maxVal) maxVal = avgAbs;
        }
        if (maxVal <= 1e-6f) maxVal = 1f; // 避免除 0

        // 生成纹理（背景全透明，支持 Mask）
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor; // 透明底
        tex.SetPixels(pixels);

        // 纵向缩放：最大值映射到半高
        int half = height / 2;
        float vScale = half / maxVal;

        // 逐列画竖线
        for (int x = 0; x < columns; x++)
        {
            int amp = Mathf.Clamp(Mathf.RoundToInt(colValues[x] * vScale), 0, half);
            int yStart = Mathf.Max(0, half - amp);
            int yEnd = Mathf.Min(height - 1, half + amp);

            for (int y = yStart; y <= yEnd; y++)
                pixels[y * width + x] = waveFormDiagramColor;
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);

        // 显示到 Image（仅波形像素可见，背景透明 → 可被 Mask 裁剪）
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                   new Vector2(0.5f, 0.5f), 100f);
        musicWaveFormDiagramShow.sprite = sprite;

        // —— 强制左对齐到 0 ——（锚点/枢轴均为左）
        var rt = musicWaveFormDiagramShow.rectTransform;
        rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
        rt.anchorMax = new Vector2(0f, rt.anchorMax.y);
        rt.pivot = new Vector2(0f, rt.pivot.y);
        rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);

        if (setNativeSize)
            musicWaveFormDiagramShow.SetNativeSize();
        else
            rt.sizeDelta = new Vector2(tex.width, tex.height);

        // 确保 Mask 设置生效
        musicWaveFormDiagramShow.maskable = maskable;
        musicWaveFormDiagramShow.raycastTarget = raycastTarget;

        // 广播“波形已就绪”，Orbit 可据此拉长轨道
        this.SendEvent(new OnWaveformReady
        {
            AudioLength = music.length,
            PixelWidth = tex.width
        });

        Debug.Log($"[Waveform] 生成完成：{music.name}, {tex.width}x{tex.height}, PUPS={PUPS}, ch={channels}, freq={freq}");
    }

    private void ClearImage()
    {
        if (musicWaveFormDiagramShow != null)
            musicWaveFormDiagramShow.sprite = null;
    }

    public IArchitecture GetArchitecture() => GameBody.Interface;
}
