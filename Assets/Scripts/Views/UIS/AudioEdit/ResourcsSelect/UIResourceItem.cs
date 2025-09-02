using DG.Tweening;
using Qf.ClassDatas.AudioEdit;
using Qf.Events;
using Qf.Managers;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIResourceItem : MonoBehaviour, IController,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("组件绑定")]
    [SerializeField] private TMP_Text _Name;   // UI显示文本（可能跑马灯/截断）
    [SerializeField] public Image _Image;

    [Header("TTS 专用名称（不参与UI截断/滚动）")]
    [SerializeField] private string _TTSName;  // 仅用于朗读/TTS

    private AudioClip audioClip;

    [Header("颜色设置")]
    [SerializeField] private Color mDefaultColor = new Color(1f, 1f, 1f);             // #FFFFFF
    [SerializeField] private Color mHoverColor = new Color(0.698f, 0.953f, 0.851f); // #B2F3D9
    [SerializeField] private Color mPressedColor = new Color(0.451f, 0.925f, 0.576f); // #73EC93

    private float mHoverCooldown = 1.0f;
    private float mLastHoverPlayTime = -10f;

    // 全局控制：是否允许“悬停自动试听”（默认 false）
    public static bool EnableHoverPreview = false;
    public static void SetHoverPreviewEnabled(bool v) => EnableHoverPreview = v;

    void Start()
    {
        if (_Name == null)
            _Name = transform.GetChild(0).GetComponent<TMP_Text>();

        if (_Image != null)
            _Image.color = mDefaultColor;
    }

    // —— 数据接口 ——
    public void SetAudioClip(AudioClip audioClip) => this.audioClip = audioClip;
    public AudioClip GetAudioClip() => audioClip;

    /// <summary>设置 UI 显示用名称（可被UI截断/滚动影响）</summary>
    public void SetName(string name)
    {
        if (_Name) _Name.text = name;
    }

    /// <summary>设置 TTS 专用名称（不参与 UI 效果处理）</summary>
    public void SetTTSName(string name)
    {
        _TTSName = name;
    }

    /// <summary>获取 TTS 专用名称（若未设置则返回 AudioClip.name 或 GameObject.name 兜底）</summary>
    public string GetTTSName()
    {
        if (!string.IsNullOrEmpty(_TTSName)) return _TTSName;
        if (audioClip) return audioClip.name;
        return gameObject.name;
    }

    public void SetImage(Sprite Sprite) { if (_Image) _Image.sprite = Sprite; }

    public IArchitecture GetArchitecture() => GameBody.Interface;

    // —— 交互 —— 
    public void OnPointerClick(PointerEventData eventData)
    {
        transform.DOScale(new Vector3(1.1f, 1.1f, 1.1f), 0.1f).SetEase(Ease.Linear).OnComplete(() =>
        {
            transform.DOScale(new Vector3(1f, 1f, 1f), 0.1f).SetEase(Ease.Linear);
        });

        this.SendEvent(new SelectOptions() { SelectValue = audioClip, SelectObject = gameObject });

        // 点击播放保留
        if (audioClip) AudioEditManager.Instance.OnePlay(audioClip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_Image != null) _Image.color = mHoverColor;

        if (EnableHoverPreview && audioClip != null && Time.time - mLastHoverPlayTime >= mHoverCooldown)
        {
            AudioEditManager.Instance.OnePlay(audioClip);
            mLastHoverPlayTime = Time.time;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_Image != null) _Image.color = mDefaultColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_Image != null) _Image.color = mPressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_Image != null) _Image.color = mHoverColor;
    }
}
