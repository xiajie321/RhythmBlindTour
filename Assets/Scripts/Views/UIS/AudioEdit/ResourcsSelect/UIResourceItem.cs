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
    [SerializeField] private TMP_Text _Name;
    [SerializeField] public Image _Image; //  改为 Inspector 中手动指定

    private AudioClip audioClip;

    [Header("颜色设置")]
    [SerializeField] private Color mDefaultColor = new Color(1f, 1f, 1f);             // #FFFFFF
    [SerializeField] private Color mHoverColor = new Color(0.698f, 0.953f, 0.851f);   // #B2F3D9
    [SerializeField] private Color mPressedColor = new Color(0.451f, 0.925f, 0.576f); // #73EC93

    private float mHoverCooldown = 1.0f;
    private float mLastHoverPlayTime = -10f;

    void Start()
    {
        if (_Name == null)
            _Name = transform.GetChild(0).GetComponent<TMP_Text>();

        if (_Image != null)
            _Image.color = mDefaultColor;
    }

    public void SetAudioClip(AudioClip audioClip)
    {
        this.audioClip = audioClip;
    }

    public AudioClip GetAudioClip()
    {
        return audioClip;
    }

    public void SetName(string Name)
    {
        _Name.text = Name;
    }

    public void SetImage(Sprite Sprite)
    {
        _Image.sprite = Sprite;
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        transform.DOScale(new Vector3(1.1f, 1.1f, 1.1f), 0.1f).SetEase(Ease.Linear).OnComplete(() =>
        {
            transform.DOScale(new Vector3(1f, 1f, 1f), 0.1f).SetEase(Ease.Linear);
        });

        this.SendEvent(new SelectOptions() { SelectValue = audioClip, SelectObject = gameObject });
        AudioEditManager.Instance.OnePlay(audioClip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_Image != null) _Image.color = mHoverColor;

        if (audioClip != null && Time.time - mLastHoverPlayTime >= mHoverCooldown)
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
