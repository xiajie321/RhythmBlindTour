using UnityEngine;
using TMPro;
using Qf.Events;
using QFramework;
using System.Collections;

/// <summary>
/// 需要添加“可被选择 selectable”组件
///     
// *用来朗读刻度，目前存在一个bug，类似-重复触发抖动-，还未查明原因。
/// </summary>
public class TestTTS_TMPTextSpeaker : MonoBehaviour, IController
{
    private TMP_Text mTMPText;
    // 防止重复触发朗读的标记
    private bool _speakPending = false;

    private void Awake()
    {
        mTMPText = GetComponent<TMP_Text>();
        if (mTMPText == null)
        {
            ("TestTTS_TMPTextSpeaker 脚本需要挂载在拥有 TMP_Text 组件的 GameObject 上！").TTSLog(Color.red);
        }
    }

    private IEnumerator Start()
    {
        // 等待 Start 的最后一帧注册事件
        yield return new WaitForEndOfFrame();

        this.RegisterEvent<OnUpdateThisTime>(v =>
        {
            // 检查 A、D、LeftArrow、RightArrow 的按下和抬起，或鼠标左键抬起
            if (
                Input.GetKeyDown(KeyCode.A) || Input.GetKeyUp(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.D) || Input.GetKeyUp(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.LeftArrow) ||
                Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.RightArrow) ||
                Input.GetMouseButtonUp(0)
            )
            {
                if (!_speakPending)
                {
                    _speakPending = true;
                    StartCoroutine(DelayedSpeak());
                }
            }
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private IEnumerator DelayedSpeak()
    {
        // 延迟 0.1 秒，确保数据更新完成（可根据实际情况调整延迟时间）
        yield return new WaitForSeconds(0.1f);
        if (mTMPText != null && !string.IsNullOrEmpty(mTMPText.text))
        {
            mTMPText.text.UAPSpeak();
        }
        _speakPending = false;
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
