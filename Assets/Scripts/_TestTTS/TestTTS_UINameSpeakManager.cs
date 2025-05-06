using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class TestTTS_UINameSpeakManager : MonoBehaviour
{
    public enum ReadModeTest
    {
        MP3,
        UAP,
        None
    }

    [Header("Test Settings")]
    [Tooltip("选择朗读模式 (仅用于测试，打包前使用)")]
    public ReadModeTest TestReadMode = ReadModeTest.MP3;

    // 每次在 Inspector 中修改该值时自动更新
    private void OnValidate()
    {
        SetStaticReadMode();
        SetTipColor();
    }

    // 在 Start 时设置当前的朗读模式
    void Start()
    {
        SetStaticReadMode();
        SetTipColor();
    }

    // 更新静态脚本中的朗读模式枚举，并将数据存入 PlayerPrefs
    private void SetStaticReadMode()
    {
        switch (TestReadMode)
        {
            case ReadModeTest.MP3:
                TestTTS_StaticActionUAP.SetReadModeToMP3();
                break;
            case ReadModeTest.UAP:
                TestTTS_StaticActionUAP.SetReadModeToUAP();
                break;
            case ReadModeTest.None:
                TestTTS_StaticActionUAP.SetReadModeToNone();
                break;
        }
    }

    public Color titleColor = Color.yellow;
    public Color separatorColor = Color.green;

    private void SetTipColor() 
    {
        TestTTS_StaticActionUAP.titleColor = titleColor;
        TestTTS_StaticActionUAP.separatorColor = separatorColor;
    }
}
