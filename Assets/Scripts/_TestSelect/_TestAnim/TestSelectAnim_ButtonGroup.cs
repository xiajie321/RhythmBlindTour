using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;


namespace TestRevolver 
{

[System.Serializable]
public class TestSelectAnim_ButtonGroup
{
    #region // ── 组管理 ─────────────────────────────────────
    [Header("组管理")]
    [Tooltip("指定本组的 Root 对象，通常为该组按钮的父节点")]
    public RectTransform groupRoot;
    [Tooltip("指定本组所有按钮（存放 UI Button 组件，如果为空则自动从 groupRoot 下查找）")]
    public List<Button> buttons;
    #endregion

    #region // ── 布局设置 ───────────────────────────────────
    [Header("布局设置")]
    [Tooltip("按钮之间的水平间距，建议保持与实际排列一致")]
    public float spacing = 100f;
    #endregion

    #region // ── 默认重置值 ─────────────────────────────────
    [Header("默认重置值")]
    [Tooltip("重置时按钮的默认 Scale")]
    public Vector3 defaultScale = Vector3.one;
    [Tooltip("重置时按钮的默认 Y 偏移量")]
    public float defaultOffsetY = 0f;
    #endregion
    
    #region // ── 缩放动画设置 ─────────────────────────────────
    [Header("缩放动画设置")]
    public bool useScaleEffect = true;
    public float scaleDuration = 0.3f;
    public float scaleFactor = 1.1f;
    public Ease scaleEase = Ease.OutQuad;
    public Ease deselectScaleEase = Ease.OutQuad;
    #endregion

    #region // ── 垂直偏移动画设置 ─────────────────────────────
    [Header("垂直偏移动画设置")]
    public bool useVerticalOffset = true;
    public float verticalOffset = 20f;
    public float offsetDuration = 0.3f;
    public Ease offsetEase = Ease.OutQuad;
    public Ease deselectOffsetEase = Ease.OutQuad;
    #endregion

    #region // ── 居中动画设置 ──────────────────────────────────
    [Header("居中动画设置")]
    [Tooltip("直接对按钮进行水平居中（仅移动按钮自身）")]
    public bool useCentering = true;
    [Tooltip("通过移动 groupRoot 实现居中（仅在未启用 Centering 时有效）")]
    public bool useRootCentering = true;
    public float centeringDuration = 0.3f;
    public float rootCenteringDuration = 0.3f;
    public Ease centeringEase = Ease.OutQuad;
    public Ease rootCenteringEase = Ease.OutQuad;
    #endregion

    #region // ── 按钮层级管理 ─────────────────────────────────
    [Header("按钮层级管理")]
    public bool bringToFront = true;
    #endregion

    #region // ── Only Arounding 模式 ─────────────────────────
    [Header("Only Arounding 模式")]
    [Tooltip("当开启时，将只启用 Arounding 循环排列（边缘跳跃），不自动执行居中更新。")]
    public bool onlyArounding = false;
    #endregion

    #region // ── 相机跟随设置 ─────────────────────────────────
    [Header("相机跟随设置")]
    [Tooltip("是否应用相机跟随效果（仅当全局摄像机设置不为空时有效）")]
    public bool applyCameraFollow = false;
    #endregion

    #region // ── 内部数据 ──────────────────────────────────────
    [HideInInspector]
    public List<Button> logicalOrder = new List<Button>();
    [HideInInspector]
    public Vector2 originalGroupRootAnchoredPos;

    // 新增字段：记录当前选中的 TestSelectAnim_Button
    [HideInInspector]
    public TestSelectAnim_Button lastSelectedButton;
    #endregion
}
}