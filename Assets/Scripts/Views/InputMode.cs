using Qf.ClassDatas.AudioEdit;
using Qf.Models.AudioEdit;
using Qf.Systems;
using QFramework;
using UnityEngine;

public class InputMode : MonoBehaviour,IController
{
    [SerializeField]
    TheTypeOfOperation operation;
    AudioEditModel editModel;
    DrwmsData drwmsData = new();
    public DrwmsData DrwmsData { get { return drwmsData; } set { drwmsData = value; } }
    [SerializeField]
    float TimeOfExistence;//�ĵ����ʱ��
    [SerializeField]
    AudioClip _PreAdventClip;//����ǰ���ŵ���Ƶ
    public AudioClip PreAdventClip { get { return _PreAdventClip; } set { _PreAdventClip = value; } }
    [SerializeField]
    AudioClip _SucceedClip;//�ɹ�ʱ����Ƶ
    public AudioClip SuccessClip { get { return _SucceedClip; } set { _SucceedClip = value; } }
    [SerializeField]
    AudioClip _FailClip;//ʧ��ʱ����Ƶ
    public AudioClip FailClip { get { return _FailClip; }  set { _FailClip = value; } }
    //[SerializeField]
    //bool isPlay;//�Ƿ񱻵��(���ڴ���ͬʱ���ֵ����Ŀǰ��˵�ò���)
    public SpriteRenderer SpriteRenderer;
    private void OnEnable()
    {
        TimeOfExistence = 0;
    }
    void Init()
    {
        editModel = this.GetModel<AudioEditModel>();
    }
    void Start()
    {
        Init();
    }
    void Update()
    {
        TimeOfExistence += Time.deltaTime;
        if(editModel.Mode.Equals(SystemModeData.PlayMode))
            InputRun();
    }
    void InputRun()
    {
        switch (operation)
        {
            case TheTypeOfOperation.SwipeUp:
                SwipeUp();
                break;
            case TheTypeOfOperation.SwipeDown:
                SwipeDown();
                break;
            case TheTypeOfOperation.SwipeLeft:
                SwipeLeft();
                break;
            case TheTypeOfOperation.SwipeRight:
                SwipeRight();
                break;
            case TheTypeOfOperation.Click:
                Click();
                break;
            default:
                break;
        }
    }
    void SwipeUp()
    {
        if (InputSystems.SwipeUp)
        {
            Debug.Log("�ϻ�");
        }
    }
    void SwipeDown()
    {
        if (InputSystems.SwipeDown)
        {
            Debug.Log("�»�");
        }
    }
    void SwipeLeft()
    {
        if (InputSystems.SwipeLeft)
        {
            Debug.Log("��");
        }
    }
    void SwipeRight()
    {
        if (InputSystems.SwipeRight)
        {
            Debug.Log("�һ�");
        }
    }
    void Click()
    {
        if (InputSystems.Click)
        {
            Debug.Log("���");
        }
    }
    public void SetOperation(TheTypeOfOperation theTypeOfOperation)
    {
        operation = theTypeOfOperation;
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
/// <summary>
/// ��������
/// </summary>
public enum TheTypeOfOperation
{
    SwipeUp,
    SwipeDown,
    SwipeRight,
    SwipeLeft,
    Click
}
