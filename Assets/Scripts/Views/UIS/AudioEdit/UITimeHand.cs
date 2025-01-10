using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Querys.AudioEdit;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITimeHand : MonoBehaviour, IController, IPointerClickHandler
{
    [SerializeField]
    RectTransform TimeHand;
    [SerializeField]
    TMP_Text[] ShowTimes;
    [SerializeField]
    ScrollRect ScrollRect;

    public Vector2 TimeHandPos
    {
        get
        {
            return TimeHand.anchoredPosition;
        }
        set
        {
            TimeHand.anchoredPosition = value;
        }
    }
    private void Start()
    {
        this.RegisterEvent<OnUpdateThisTime>(v =>
        {
            UpdateThisTime();
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    private void OnEnable()
    {
        this.SendEvent<OnUpdateThisTime>();
    }
    void UpdateThisTime()
    {
        float newTime = this.SendQuery(new QueryAudioEditAudioClipThisTime());
        TimeHand.anchoredPosition = new Vector2(newTime * 100, 0);
        foreach (var t in ShowTimes)
        {
            t.text = newTime.ToString("0.00");
        }
    }
    int mode = 0;//这里的模式指:时间轴的添加或删除模式
    float Speed;
    float PressTime;
    private void Update()
    {
        #region 模式切换
        if (mode == 0)
        {
            PressTime = 0;
        }
        else if (mode == 1)
        {
            if (PressTime >= 0.5f)
            {
                AddTime(Speed);
            }
            else
            {
                PressTime += Time.deltaTime;
            }
        }
        else if (mode == 2)
        {
            if (PressTime >= 0.5f)
            {
                RemoveTime(Speed);
            }
            else
            {
                PressTime += Time.deltaTime;
            }
        }
        #endregion
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            RemoveTime(1);
            RemoveTimeMode(1);
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            StopTimeMode(0);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AddTime(1);
            AddTimeMode(1);
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            StopTimeMode(0);
        }
    }
    public void AddTimeMode(float Speed)
    {
        mode = 1;
        this.Speed = Speed;
    }
    public void StopTimeMode(float Speed)
    {
        mode = 0;
        this.Speed = Speed;
    }
    public void RemoveTimeMode(float Speed)
    {
        mode = 2;
        this.Speed = Speed;
    }
    public void AddTime(float Speed)
    {
        this.SendCommand(
            new SetAudioEditThisTimeCommand(
                this.SendQuery(
                    new QueryAudioEditAudioClipThisTime()) + 0.01f * Speed));
        ScrollRect.horizontalScrollbar.value = (TimeHand.anchoredPosition.x / 100)/this.SendQuery(new QueryAudioEditAudioClipLength());
    }
    public void RemoveTime(float Speed)
    {
        this.SendCommand(
            new SetAudioEditThisTimeCommand(
                this.SendQuery(
                    new QueryAudioEditAudioClipThisTime()) - 0.01f * Speed));
        ScrollRect.horizontalScrollbar.value = (TimeHand.anchoredPosition.x / 100) / this.SendQuery(new QueryAudioEditAudioClipLength());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TimeHand.position = eventData.position;
        this.SendCommand(new SetAudioEditThisTimeCommand((TimeHand.anchoredPosition.x) / 100));
    }
    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }


}
