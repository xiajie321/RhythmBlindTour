using UnityEngine;
using Qf.Models.AudioEdit;

[RequireComponent(typeof(InputMode))]
public class mInputModeVisualController : MonoBehaviour
{
    public Transform judgeLineTarget;
    public Transform judgmentBarTransform;

    private InputMode inputMode;
    private AudioEditModel editModel;

    private Vector3 moveDirection;
    private float moveSpeedPerSecond;
    private float distanceToMove;

    public static event System.Action<bool> OnPauseInputModeVisual;

    private void OnEnable()
    {
        OnPauseInputModeVisual += HandlePauseEvent;
    }

    private void OnDisable()
    {
        OnPauseInputModeVisual -= HandlePauseEvent;
    }

    private void HandlePauseEvent(bool pause)
    {
        if (inputMode != null)
            inputMode.PauseAutoFail = pause;
    }

    void Start()
    {
        inputMode = GetComponent<InputMode>();
        editModel = inputMode.GetArchitecture().GetModel<AudioEditModel>();

        Vector3 targetPos = judgeLineTarget.position;
        Vector3 currentPos = transform.position;

        distanceToMove = targetPos.x - currentPos.x;
        moveDirection = distanceToMove >= 0 ? Vector3.right : Vector3.left;

        float centerTime = (inputMode.StartTime + inputMode.EndTime) / 2f;
        float timeToReachCenter = centerTime - inputMode.PreAdventTime;
        moveSpeedPerSecond = Mathf.Abs(distanceToMove) / timeToReachCenter;

        float judgmentDuration = inputMode.EndTime - inputMode.StartTime;
        float barLength = moveSpeedPerSecond * judgmentDuration;

        if (judgmentBarTransform != null)
        {
            Vector3 scale = judgmentBarTransform.localScale;
            scale.x = barLength;
            judgmentBarTransform.localScale = scale;
        }
    }

    void FixedUpdate()
    {
        if (inputMode == null || inputMode.HasJudged || inputMode.PauseAutoFail)
            return;

        float deltaMove = moveSpeedPerSecond * Time.fixedDeltaTime;
        transform.position += moveDirection * deltaMove;
    }

    /// <summary>
    /// 外部调用这个方法，触发所有 InputMode 的暂停/继续
    /// </summary>
    public static void BroadcastPauseToAll(bool pause)
    {
        OnPauseInputModeVisual?.Invoke(pause);
    }
}
