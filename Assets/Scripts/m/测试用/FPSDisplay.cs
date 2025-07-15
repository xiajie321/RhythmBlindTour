using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    [SerializeField] public Text fpsText;

    int frameCount = 0;
    float elapsedTime = 0f;
    float refreshInterval = 0.5f; // 刷新间隔（秒）
    int currentFPS = 0;

    void Update()
    {
        frameCount++;
        elapsedTime += Time.unscaledDeltaTime;

        if (elapsedTime >= refreshInterval)
        {
            currentFPS = Mathf.RoundToInt(frameCount / elapsedTime);
            fpsText.text = $"FPS: {currentFPS}";
            frameCount = 0;
            elapsedTime = 0f;
        }
    }
}
