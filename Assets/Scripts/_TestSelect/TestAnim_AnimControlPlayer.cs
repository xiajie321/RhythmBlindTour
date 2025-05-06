using UnityEngine;

public class TestAnim_AnimControlPlayer : MonoBehaviour
{
    [SerializeField]
    private Animator m_animator;

    /// <summary>
    /// 外部设置 Animator
    /// </summary>
    /// <param name="animator">目标 Animator</param>
    public void SetAnimator(Animator animator)
    {
        m_animator = animator;
    }

    /// <summary>
    /// 重新播放指定动画
    /// 支持格式："layerName" 或 "0-Walk"（代表在第0层播放"Walk"动画）
    /// </summary>
    /// <param name="layerAndName">动画名或"layer-name"格式</param>
    public void _TA_PlayAnimation(string layerAndName)
    {
        if (m_animator == null)
        {
            // Debug.LogWarning("Animator is not assigned.");
            return;
        }

        int layer = 0;
        string stateName = layerAndName;

        // 检查是否包含 '-'，如果有则尝试解析 layer 和 name
        int separatorIndex = layerAndName.IndexOf('-');
        if (separatorIndex > 0)
        {
            string layerPart = layerAndName.Substring(0, separatorIndex);
            string namePart = layerAndName.Substring(separatorIndex + 1);

            if (int.TryParse(layerPart, out int parsedLayer))
            {
                layer = parsedLayer;
                stateName = namePart;
            }
            else
            {
                // Debug.LogWarning($"Invalid layer format: '{layerPart}', fallback to layer 0.");
            }
        }

        // 重新播放动画：将 normalizedTime 设置为 0，确保动画从头开始播放
        m_animator.Play(stateName, layer, 0f);
    }
}
