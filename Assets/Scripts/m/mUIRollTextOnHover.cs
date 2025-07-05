using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Text;

public class mUIRollTextOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("需要滚动的文本组件")]
    [SerializeField] private TextMeshProUGUI mScrollText;

    [Header("可视字符数（固定窗口宽度）")]
    [SerializeField] private int mVisibleCharCount = 10;

    [Header("滚动速度（秒/字符）")]
    [SerializeField] private float mScrollInterval = 0.2f;

    [Header("衔接空格数")]
    [SerializeField] private int mGapSpaceCount = 5;

    private string mOriginalText; // 始终保持原始文本，不随text显示改变
    private string mLoopText;
    private Coroutine mScrollCoroutine;
    private bool mIsHovering = false;
    private bool mHasSavedOriginal = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mIsHovering) return;
        mIsHovering = true;

        StartCoroutine(PrepareLoopTextAndStart());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mIsHovering = false;

        if (mScrollCoroutine != null)
        {
            StopCoroutine(mScrollCoroutine);
            mScrollCoroutine = null;
        }

        // 还原为原始文本的前可视部分
        if (!string.IsNullOrEmpty(mOriginalText))
        {
            mScrollText.text = mOriginalText.Length > mVisibleCharCount
                ? mOriginalText.Substring(0, mVisibleCharCount)
                : mOriginalText;
        }
    }

    private IEnumerator PrepareLoopTextAndStart()
    {
        yield return null; // 等待一帧确保文本赋值完成

        if (!mHasSavedOriginal)
        {
            mOriginalText = mScrollText.text;
            mHasSavedOriginal = true;
        }

        if (mOriginalText.Length <= mVisibleCharCount)
        {
            mLoopText = mOriginalText;
            mScrollText.text = mOriginalText;
            yield break;
        }

        // 构建拼接字符串：原文 + 空格 + 原文
        var gap = new string(' ', mGapSpaceCount);
        mLoopText = mOriginalText + gap + mOriginalText;

        mScrollText.text = mLoopText.Substring(0, mVisibleCharCount);
        mScrollCoroutine = StartCoroutine(RollText());
    }

    private IEnumerator RollText()
    {
        int start = 0;
        int len = mLoopText.Length;

        while (mIsHovering)
        {
            string segment = GetSubstringSafe(mLoopText, start, mVisibleCharCount);
            mScrollText.text = segment;

            yield return new WaitForSeconds(mScrollInterval);

            start++;
            if (start >= len) start = 0;
        }
    }

    private string GetSubstringSafe(string str, int startIndex, int length)
    {
        StringBuilder sb = new();
        int count = 0;

        for (int i = startIndex; count < length; i++)
        {
            sb.Append(i >= str.Length ? ' ' : str[i]);
            count++;
        }

        return sb.ToString();
    }
}
