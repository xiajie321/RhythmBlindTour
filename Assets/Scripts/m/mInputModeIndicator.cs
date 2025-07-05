// 挂载到预制体上，与InputMode共存
using UnityEngine;

public class mInputModeIndicator : MonoBehaviour
{
    public GameObject ClickIndicator; public GameObject SwipeLeftIndicator;
    public GameObject SwipeRightIndicator;
    public GameObject SwipeUpIndicator;
    public GameObject SwipeDownIndicator;


    private void Start()
    {
        var inputMode = GetComponent<InputMode>();
        if (inputMode == null)
        {
            Debug.LogWarning("未找到 InputMode 组件。");
            return;
        }

        DisableAllIndicators();

        switch (inputMode.GetOperation())
        {
            case TheTypeOfOperation.Click:
                if (ClickIndicator != null) ClickIndicator.SetActive(true);
                break;
            case TheTypeOfOperation.SwipeDown:
                if (SwipeDownIndicator != null) SwipeDownIndicator.SetActive(true);
                break;
            case TheTypeOfOperation.SwipeUp:
                if (SwipeUpIndicator != null) SwipeUpIndicator.SetActive(true);
                break;
            case TheTypeOfOperation.SwipeLeft:
                if (SwipeLeftIndicator != null) SwipeLeftIndicator.SetActive(true);
                break;
            case TheTypeOfOperation.SwipeRight:
                if (SwipeRightIndicator != null) SwipeRightIndicator.SetActive(true);
                break;
        }
    }

    private void DisableAllIndicators()
    {
        if (ClickIndicator != null) ClickIndicator.SetActive(false);
        if (SwipeDownIndicator != null) SwipeDownIndicator.SetActive(false);
        if (SwipeUpIndicator != null) SwipeUpIndicator.SetActive(false);
        if (SwipeLeftIndicator != null) SwipeLeftIndicator.SetActive(false);
        if (SwipeRightIndicator != null) SwipeRightIndicator.SetActive(false);
    }
}
