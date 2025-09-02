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

        // —— 统一坐标系：在共同父节点的本地坐标计算“出现位置”和“目标位置”的水平距离 —— 
        Transform commonParent = transform.parent != null ? transform.parent
                               : (judgeLineTarget ? judgeLineTarget.parent : null);
        Vector3 from = commonParent ? commonParent.InverseTransformPoint(transform.position) : transform.position;
        Vector3 to = commonParent ? commonParent.InverseTransformPoint(judgeLineTarget.position) : judgeLineTarget.position;

        float dx = to.x - from.x;
        float distanceX = Mathf.Abs(dx);

        // —— 时间参数（都来自 InputMode，口径一致）——
        float start = inputMode.StartTime;                         // 进入判定
        float end = inputMode.EndTime;                           // 判定结束
        float center = 0.5f * (start + end);                        // 中心
        float lead = Mathf.Max(center - inputMode.PreAdventTime, 0f); // ★提示音提前时间长度

        // —— 速度：距离 / 提前时间长度（按你的要求）——
        if (lead <= 1e-6f || distanceX <= 1e-6f)
        {
            moveSpeedPerSecond = 0f;
        }
        else
        {
            moveSpeedPerSecond = distanceX / lead; // ★ v = s / lead
        }

        // —— 判定条长度：判定时长 / 速度（= v * 判定时长 也等价）——
        float judgeDuration = Mathf.Max(end - start, 0f);
        float barLength = (moveSpeedPerSecond <= 0f) ? 0f : (judgeDuration / (1f / moveSpeedPerSecond)); // = v * judgeDuration

        // —— 视觉口径：鼓点在“条的正中”，条从中心向两侧延展，整体沿 +X 移动 —— 
        if (judgmentBarTransform != null)
        {
            var s = judgmentBarTransform.localScale;
            s.x = barLength;
            judgmentBarTransform.localScale = s;

            // 条的局部位置与鼓点中心对齐（不再把前沿钉在物体上）
            var lp = judgmentBarTransform.localPosition;
            lp.x = 0f;                    // ★ 鼓点在条的中间
            judgmentBarTransform.localPosition = lp;
        }

        // —— 移动方向固定为 +X（画面右侧）——
        moveDirection = Vector3.right;

        // 可选：如果你希望一开始就把本地 X 归零（视觉上“从 0 开始向右跑”）
        // transform.localPosition = new Vector3(0f, transform.localPosition.y, transform.localPosition.z);

        // 若需要：记录“到达 target 的世界 X”，后续可用于夹紧（防穿越）
        _targetWorldX = judgeLineTarget ? judgeLineTarget.position.x : float.PositiveInfinity;
    }

    private float _targetWorldX = float.PositiveInfinity;

    void FixedUpdate()
    {
        if (inputMode == null || inputMode.HasJudged || inputMode.PauseAutoFail)
            return;

        float step = moveSpeedPerSecond * Time.fixedDeltaTime;
        if (step <= 0f) return;

        // —— 鼓点与判定条一起向 +X 匀速移动 —— 
        var pos = transform.position;
        pos += moveDirection * step;

        // 可选：到达 target 后夹紧，避免浮点穿越（不影响判定逻辑，只为视觉贴合）
        if (pos.x >= _targetWorldX) pos.x = _targetWorldX;

        transform.position = pos;
    }

    /// <summary>
    /// 外部调用这个方法，触发所有 InputMode 的暂停/继续
    /// </summary>
    public static void BroadcastPauseToAll(bool pause)
    {
        OnPauseInputModeVisual?.Invoke(pause);
    }
}
