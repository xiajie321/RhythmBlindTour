using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace TestRevolver
{
    public enum CameraFollowMode
    {
        CameraLookUI,   // 相机移动看 UI
        UILookCamera    // UI 移动对齐相机
    }

    public class TestSelectAnim_GroupAnimator : MonoBehaviour
    {
        #region // ── 按钮组设置 ──────────────────────────────────
        [Header("按钮组设置")]
        [Tooltip("存储多个按钮组，每一组可以独立配置各自的动画效果和布局参数")]
        public List<TestSelectAnim_ButtonGroup> buttonGroups;
        #endregion

        #region // ── 目标与参照 ──────────────────────────────────
        [Header("目标与参照")]
        [Tooltip("用于计算水平居中效果的目标参考对象（与各组的 groupRoot 同级）")]
        public RectTransform centerTarget;
        #endregion

        #region // ── 摄像机跟随设置 ───────────────────────────────
        [Header("摄像机跟随设置（全局设置）")]
        [Tooltip("指定跟随的对象，可为摄像机或任意 UI/世界物体")]
        public GameObject followTargetObject;

        [Tooltip("摄像机跟随目标的偏移（例如 2D 场景中常用 -10 的 Z 轴偏移）")]
        public Vector3 cameraOffset = new Vector3(0, 0, -10f);

        [Tooltip("摄像机移动的动画时长")]
        public float cameraMoveDuration = 0.5f;

        [Tooltip("摄像机移动的缓动类型")]
        public Ease cameraMoveEase = Ease.OutQuad;

        [Tooltip("相机跟随模式：CameraLookUI = 相机跟随按钮，UILookCamera = UI反向移动以居中")]
        public CameraFollowMode followMode = CameraFollowMode.UILookCamera;

        private Camera activeCamera;
        private Transform activeTarget;
        #endregion

        #region // ── 初始化 ──────────────────────────────────────
        private void Start()
        {
            // 判断 followTargetObject 是否是摄像机或普通 transform
            if (followTargetObject != null)
            {
                activeCamera = followTargetObject.GetComponent<Camera>();
                activeTarget = followTargetObject.transform;
            }

            if (buttonGroups != null)
            {
                foreach (TestSelectAnim_ButtonGroup group in buttonGroups)
                {
                    if (group.buttons == null || group.buttons.Count == 0)
                    {
                        group.buttons = new List<Button>();
                        if (group.groupRoot != null)
                        {
                            foreach (Transform child in group.groupRoot)
                            {
                                Button btn = child.GetComponent<Button>();
                                if (btn != null)
                                    group.buttons.Add(btn);
                            }
                        }
                    }

                    group.logicalOrder = new List<Button>(group.buttons);
                    group.logicalOrder.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

                    if (group.groupRoot != null)
                        group.originalGroupRootAnchoredPos = group.groupRoot.anchoredPosition;

                    foreach (Button btn in group.buttons)
                    {
                        TestSelectAnim_Button animButton = btn.GetComponent<TestSelectAnim_Button>();
                        if (animButton == null)
                            animButton = btn.gameObject.AddComponent<TestSelectAnim_Button>();
                        animButton.groupAnimator = GetComponent<TestSelectAnim_AnimActionHandler>();
                    }
                }
            }
        }
        #endregion

        #region // ── 内部处理方法 ───────────────────────────────
        internal void HandleButtonSelected(TestSelectAnim_Button animButton)
        {
            Button btn = animButton.GetComponent<Button>();
            TestSelectAnim_ButtonGroup group = GetButtonGroupForButton(btn);
            if (group == null) return;

            RectTransform buttonRect = animButton.GetComponent<RectTransform>();
            Sequence seq = DOTween.Sequence();
            if (group.useScaleEffect)
                seq.Join(buttonRect.DOScale(group.scaleFactor, group.scaleDuration).SetEase(group.scaleEase));
            if (group.useVerticalOffset)
                seq.Join(buttonRect.DOAnchorPosY(animButton.originalAnchoredPos.y + group.verticalOffset, group.offsetDuration).SetEase(group.offsetEase));
            seq.Play();

            if (group.bringToFront)
                animButton.transform.SetAsLastSibling();

            if (!group.onlyArounding && centerTarget != null)
            {
                if (group.useCentering)
                    buttonRect.DOAnchorPosX(centerTarget.anchoredPosition.x, group.centeringDuration).SetEase(group.centeringEase);
                else if (group.useRootCentering)
                {
                    Vector2 newGroupPos = new Vector2(centerTarget.anchoredPosition.x - buttonRect.localPosition.x, group.groupRoot.anchoredPosition.y);
                    group.groupRoot.DOAnchorPos(newGroupPos, group.rootCenteringDuration).SetEase(group.rootCenteringEase);
                }
            }

            if (followTargetObject != null && group.applyCameraFollow)
            {
                if (followMode == CameraFollowMode.CameraLookUI && activeTarget != null)
                {
                    if (activeCamera != null)
                    {
                        Vector3 targetCameraPos = buttonRect.position + cameraOffset;
                        activeCamera.transform.DOMove(targetCameraPos, cameraMoveDuration).SetEase(cameraMoveEase);
                    }
                    else
                    {
                        Vector3 targetPos = buttonRect.position + cameraOffset;
                        activeTarget.DOMove(targetPos, cameraMoveDuration).SetEase(cameraMoveEase);
                    }
                }
                else if (followMode == CameraFollowMode.UILookCamera && activeTarget != null && group.groupRoot != null)
                {
                    Vector3 buttonWorldPos = buttonRect.position;
                    Vector3 worldCenterPos;

                    if (activeCamera != null)
                    {
                        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, buttonWorldPos.z - activeCamera.transform.position.z);
                        worldCenterPos = activeCamera.ScreenToWorldPoint(screenCenter);
                    }
                    else
                    {
                        worldCenterPos = activeTarget.position;
                    }

                    Vector3 groupRootWorldPos = group.groupRoot.position;
                    Vector3 offset = worldCenterPos - buttonWorldPos;
                    Vector3 newRootWorldPos = groupRootWorldPos + offset;

                    group.groupRoot.DOMove(newRootWorldPos, cameraMoveDuration).SetEase(cameraMoveEase);
                }
            }
        }

        internal void HandleButtonDeselected(TestSelectAnim_Button animButton)
        {
            Button btn = animButton.GetComponent<Button>();
            TestSelectAnim_ButtonGroup group = GetButtonGroupForButton(btn);
            if (group == null) return;

            RectTransform buttonRect = animButton.GetComponent<RectTransform>();
            if (group.useScaleEffect)
                buttonRect.DOScale(animButton.originalScale, group.scaleDuration).SetEase(group.deselectScaleEase);
            if (group.useVerticalOffset)
                buttonRect.DOAnchorPosY(animButton.originalAnchoredPos.y, group.offsetDuration).SetEase(group.deselectOffsetEase);

            if (!group.onlyArounding && centerTarget != null)
            {
                if (group.useCentering)
                    buttonRect.DOAnchorPosX(animButton.originalAnchoredPos.x, group.centeringDuration).SetEase(group.centeringEase);
                else if (group.useRootCentering)
                    group.groupRoot.DOAnchorPos(group.originalGroupRootAnchoredPos, group.rootCenteringDuration).SetEase(group.rootCenteringEase);
            }
        }

        internal void HandleShiftRight(int groupIndex)
        {
            if (buttonGroups == null || groupIndex < 0 || groupIndex >= buttonGroups.Count)
                return;

            TestSelectAnim_ButtonGroup group = buttonGroups[groupIndex];
            if (!group.onlyArounding || group.logicalOrder.Count == 0)
                return;

            float minX = float.MaxValue;
            Button leftmost = null;
            float maxX = float.MinValue;
            foreach (Button btn in group.logicalOrder)
            {
                float x = btn.transform.localPosition.x;
                if (x < minX) { minX = x; leftmost = btn; }
                if (x > maxX) maxX = x;
            }
            if (leftmost == null) return;

            RectTransform rt = leftmost.GetComponent<RectTransform>();
            Vector2 targetPos = new Vector2(maxX + group.spacing, rt.localPosition.y);
            rt.localPosition = new Vector3(targetPos.x, targetPos.y, rt.localPosition.z);
            group.logicalOrder.Remove(leftmost);
            group.logicalOrder.Add(leftmost);
        }

        internal void HandleShiftLeft(int groupIndex)
        {
            if (buttonGroups == null || groupIndex < 0 || groupIndex >= buttonGroups.Count)
                return;

            TestSelectAnim_ButtonGroup group = buttonGroups[groupIndex];
            if (!group.onlyArounding || group.logicalOrder.Count == 0)
                return;

            float maxX = float.MinValue;
            Button rightmost = null;
            float minX = float.MaxValue;
            foreach (Button btn in group.logicalOrder)
            {
                float x = btn.transform.localPosition.x;
                if (x > maxX)
                {
                    maxX = x;
                    rightmost = btn;
                }
                if (x < minX)
                    minX = x;
            }
            if (rightmost == null)
                return;

            RectTransform rt = rightmost.GetComponent<RectTransform>();
            Vector2 targetPos = new Vector2(minX - group.spacing, rt.localPosition.y);
            rt.localPosition = new Vector3(targetPos.x, targetPos.y, rt.localPosition.z);
            group.logicalOrder.Remove(rightmost);
            group.logicalOrder.Insert(0, rightmost);
        }

        /// <summary>
        /// 立即恢复指定组中所有按钮的 scale 和垂直 offset 到 默认值（不含缓动）
        /// </summary>
        public void _GA_ResetGroupAnimationStates(int groupIndex)
        {
            if (buttonGroups == null || groupIndex < 0 || groupIndex >= buttonGroups.Count)
                return;

            var group = buttonGroups[groupIndex];
            if (group.buttons == null)
                return;

            foreach (var btn in group.buttons)
            {
                if (btn == null) continue;
                var animBtn = btn.GetComponent<TestSelectAnim_Button>();
                var rt = btn.GetComponent<RectTransform>();
                if (animBtn == null || rt == null) continue;

                // 使用组里配置的默认 Scale 重置
                if (group.useScaleEffect)
                    rt.localScale = group.defaultScale;

                // 使用组里配置的默认 Y 偏移重置
                if (group.useVerticalOffset)
                {
                    var pos = rt.anchoredPosition;
                    pos.y = group.defaultOffsetY;
                    rt.anchoredPosition = pos;
                }
            }
        }

        /// <summary>
        /// 重置所有组的 Scale 和 Offset 到 默认值
        /// </summary>
        public void _GA_ResetGroupAnimationStates()
        {
            if (buttonGroups == null)
                return;

            for (int i = 0; i < buttonGroups.Count; i++)
                _GA_ResetGroupAnimationStates(i);
        }


        private TestSelectAnim_ButtonGroup GetButtonGroupForButton(Button btn)
        {
            if (buttonGroups != null)
            {
                foreach (TestSelectAnim_ButtonGroup group in buttonGroups)
                {
                    if (group.buttons != null && group.buttons.Contains(btn))
                        return group;
                }
            }
            return null;
        }
        #endregion
    
    }
}
