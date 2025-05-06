using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;
using System.IO;

namespace TestRevolver
{
    [CustomEditor(typeof(TestSelect_Revolver))]
    public class TestSelect_RevolverEditor : Editor
    {
        private ReorderableList magazineList;
        private GUIStyle commonStyle;
        private GUIStyle bulletStyle;
        private float toggleRowHeight;
        private float detailRowHeight;
        private float padding = 2f;
        private List<bool> showDetailsList = new List<bool>();

        private int sharedEventTypeIndex = 0;
        private string[] eventOptions = new string[] { "Enter", "Exit", "Confirm", "Cancel" };

        private enum DisplayOption { None, Layer, Number }
        private List<DisplayOption> bulletDisplayOptions = new List<DisplayOption>();

        private List<bool> showBulletSceneSwitchSettings = new List<bool>();

        // Common SceneSwitch 折页开关，默认展开
        private bool showCommonSceneSwitchSettings = true;

        private Color selectedColor = Color.yellow;
        private Color deepGray = new Color(0.4f, 0.4f, 0.4f);

        private Color HexToColor(string hex)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(hex, out color))
                return color;
            return Color.white;
        }

        private void OnEnable()
        {
            SerializedProperty magazineProp = serializedObject.FindProperty("m_Magazine");
            showDetailsList.Clear();
            bulletDisplayOptions.Clear();
            showBulletSceneSwitchSettings.Clear();

            for (int i = 0; i < magazineProp.arraySize; i++)
            {
                showDetailsList.Add(false);
                bulletDisplayOptions.Add(DisplayOption.None);
                showBulletSceneSwitchSettings.Add(false);
                SerializedProperty bulletProp = magazineProp.GetArrayElementAtIndex(i);
                SerializedProperty modeProp = bulletProp.FindPropertyRelative("m_LevelSetMode");
                bulletDisplayOptions[i] = (DisplayOption)modeProp.enumValueIndex;

                SerializedProperty autoSubProp = bulletProp.FindPropertyRelative("m_bAutoSubscribeOnSelected");
                autoSubProp.boolValue = true;
            }

            toggleRowHeight = EditorGUIUtility.singleLineHeight;
            detailRowHeight = EditorGUIUtility.singleLineHeight;

            magazineList = new ReorderableList(serializedObject, magazineProp, true, true, true, true);
            magazineList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Magazine");
            };

            magazineList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = magazineList.serializedProperty.GetArrayElementAtIndex(index);
                float y = rect.y + 2;

                // 绘制 Button 字段
                Rect rButtonField = new Rect(rect.x, y, 120, toggleRowHeight);
                SerializedProperty buttonProp = element.FindPropertyRelative("m_Button");
                EditorGUI.PropertyField(rButtonField, buttonProp, GUIContent.none);

                // 自动订阅按钮
                Rect rAutoSubButton = new Rect(rect.x + 120, y, 30, toggleRowHeight);
                SerializedProperty autoSubProp = element.FindPropertyRelative("m_bAutoSubscribeOnSelected");
                Color autoSaved = GUI.backgroundColor;
                if (autoSubProp.boolValue)
                    GUI.backgroundColor = HexToColor("#00FF00");
                if (GUI.Button(rAutoSubButton, new GUIContent("A", "订阅到Button-OnSelected")))
                    autoSubProp.boolValue = !autoSubProp.boolValue;
                GUI.backgroundColor = autoSaved;

                // 折页按钮显示 Button 名称
                Rect rToggleButton = new Rect(rect.x + 155, y, rect.width - 155, toggleRowHeight);
                string btnName = "Button";
                if (buttonProp.objectReferenceValue != null)
                {
                    Button btn = buttonProp.objectReferenceValue as Button;
                    if (btn != null)
                        btnName = btn.gameObject.name;
                }
                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = showDetailsList[index] ? HexToColor("#4A90E2") : HexToColor("#B3D9FF");
                if (GUI.Button(rToggleButton, btnName))
                    showDetailsList[index] = !showDetailsList[index];
                GUI.backgroundColor = prevColor;

                y += toggleRowHeight + padding;
                if (!showDetailsList[index])
                    return;

                Rect boxRect = new Rect(rect.x, y, rect.width, magazineList.elementHeightCallback(index) - toggleRowHeight - padding);
                EditorGUI.DrawRect(boxRect, Color.white);
                GUI.BeginGroup(boxRect, GetBulletStyle());
                float innerY = padding;
                float innerWidth = rect.width - 10f;

                Rect lineRect = new Rect(10f, innerY, innerWidth, 1f);
                EditorGUI.DrawRect(lineRect, HexToColor("#B3B3B3"));
                innerY += 3f;

                SerializedProperty eventProp = null;
                string eventLabel = "";
                switch (sharedEventTypeIndex)
                {
                    case 0: eventProp = element.FindPropertyRelative("m_evOnEnter"); eventLabel = "On"; break;
                    case 1: eventProp = element.FindPropertyRelative("m_evOnExit"); eventLabel = "On"; break;
                    case 2: eventProp = element.FindPropertyRelative("m_evOnConfirm"); eventLabel = "On"; break;
                    case 3: eventProp = element.FindPropertyRelative("m_evOnCancel"); eventLabel = "On"; break;
                }
                float eventHeight = EditorGUI.GetPropertyHeight(eventProp, new GUIContent(eventLabel), true);
                Rect rEvent = new Rect(10f, innerY, innerWidth, eventHeight);
                EditorGUI.PropertyField(rEvent, eventProp, new GUIContent(eventLabel), true);
                float btnWidth = innerWidth / 4f;
                for (int i = 0; i < 4; i++)
                {
                    Rect rBtn = new Rect(10f + i * btnWidth, innerY, btnWidth, toggleRowHeight);
                    Color oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = (sharedEventTypeIndex == i) ? HexToColor("#F1C27D") : Color.white;
                    if (GUI.Button(rBtn, eventOptions[i]))
                        sharedEventTypeIndex = i;
                    GUI.backgroundColor = oldBg;
                }
                innerY += eventHeight + padding;

                SerializedProperty modeProp = element.FindPropertyRelative("m_LevelSetMode");
                bulletDisplayOptions[index] = (DisplayOption)modeProp.enumValueIndex;
                float noneButtonWidth = 20f;
                float remainingWidth = innerWidth - noneButtonWidth;
                float enumBtnWidth = remainingWidth / 2f;
                float standardHeight = EditorGUI.GetPropertyHeight(element.FindPropertyRelative("m_levelLayer"), new GUIContent("Level Layer"), true);
                float propertyFieldHeight = (bulletDisplayOptions[index] == DisplayOption.Layer) ?
                    EditorGUI.GetPropertyHeight(element.FindPropertyRelative("m_levelLayer"), new GUIContent("Level Layer"), true) :
                    (bulletDisplayOptions[index] == DisplayOption.Number ?
                        EditorGUI.GetPropertyHeight(element.FindPropertyRelative("m_levelNumber"), new GUIContent("Level Number"), true) :
                        standardHeight);
                float extraBottomHeight = 6f;
                float levelBoxHeight = toggleRowHeight + padding + propertyFieldHeight + padding + extraBottomHeight;
                Rect layerBoxRect = new Rect(10f, innerY - padding, innerWidth, levelBoxHeight);
                EditorGUI.DrawRect(layerBoxRect, HexToColor("#4D4D4D"));

                Rect noneButtonRect = new Rect(10f, innerY, noneButtonWidth, toggleRowHeight);
                Color savedColor = GUI.backgroundColor;
                if (bulletDisplayOptions[index] == DisplayOption.None)
                    GUI.backgroundColor = HexToColor("#FF0000");
                if (GUI.Button(noneButtonRect, "X"))
                {
                    bulletDisplayOptions[index] = DisplayOption.None;
                    modeProp.enumValueIndex = 0;
                }
                GUI.backgroundColor = savedColor;
                Rect layerButtonRect = new Rect(10f + noneButtonWidth, innerY, enumBtnWidth, toggleRowHeight);
                Color layerBtnSaved = GUI.backgroundColor;
                if (bulletDisplayOptions[index] == DisplayOption.Layer)
                    GUI.backgroundColor = HexToColor("#00FF00");
                if (GUI.Button(layerButtonRect, "Layer"))
                {
                    bulletDisplayOptions[index] = DisplayOption.Layer;
                    modeProp.enumValueIndex = 1;
                }
                GUI.backgroundColor = layerBtnSaved;
                Rect numberButtonRect = new Rect(10f + noneButtonWidth + enumBtnWidth, innerY, enumBtnWidth, toggleRowHeight);
                Color numberBtnSaved = GUI.backgroundColor;
                if (bulletDisplayOptions[index] == DisplayOption.Number)
                    GUI.backgroundColor = HexToColor("#00FF00");
                if (GUI.Button(numberButtonRect, "Number"))
                {
                    bulletDisplayOptions[index] = DisplayOption.Number;
                    modeProp.enumValueIndex = 2;
                }
                GUI.backgroundColor = numberBtnSaved;
                innerY += toggleRowHeight + padding;

                if (bulletDisplayOptions[index] == DisplayOption.Layer)
                {
                    SerializedProperty levelLayerProp = element.FindPropertyRelative("m_levelLayer");
                    float hLayer = EditorGUI.GetPropertyHeight(levelLayerProp, new GUIContent(""), true);
                    EditorGUI.PropertyField(new Rect(10f + noneButtonWidth, innerY, enumBtnWidth, hLayer), levelLayerProp, new GUIContent(""), true);
                }
                else if (bulletDisplayOptions[index] == DisplayOption.Number)
                {
                    SerializedProperty levelNumberProp = element.FindPropertyRelative("m_levelNumber");
                    float hNumber = EditorGUI.GetPropertyHeight(levelNumberProp, new GUIContent(""), true);
                    EditorGUI.PropertyField(new Rect(10f + noneButtonWidth + enumBtnWidth, innerY, enumBtnWidth, hNumber), levelNumberProp, new GUIContent(""), true);
                }
                else if (bulletDisplayOptions[index] == DisplayOption.None)
                {
                    EditorGUI.LabelField(new Rect(10f + noneButtonWidth, innerY, remainingWidth, propertyFieldHeight),
                        "//选中设置对标记/////////////////////////////////////////");
                }
                innerY += toggleRowHeight + padding;

                innerY += toggleRowHeight + padding;

                // —— Bullet SceneSwitch 设置区域 —— 
                float sceneSwitchAreaX = 10f;
                float sceneSwitchAreaWidth = innerWidth;
                float halfArea = sceneSwitchAreaWidth / 2f;
                // 绘制折页按钮
                Rect sceneSwitchToggleRect = new Rect(sceneSwitchAreaX, innerY, halfArea * 0.8f, toggleRowHeight);
                if (GUI.Button(sceneSwitchToggleRect, "Scene Switch"))
                    showBulletSceneSwitchSettings[index] = !showBulletSceneSwitchSettings[index];
                // 绘制 Bullet 应用开关 Toggle
                Rect applyToggleRect = new Rect(sceneSwitchAreaX + halfArea * 0.8f, innerY, halfArea * 0.2f, toggleRowHeight);
                SerializedProperty applyProp = element.FindPropertyRelative("m_bApplySceneSwitch");
                applyProp.boolValue = EditorGUI.Toggle(applyToggleRect, new GUIContent("Apply", "启用后采用 Bullet 的场景设置"), applyProp.boolValue);
                innerY += toggleRowHeight + padding;
                if (showBulletSceneSwitchSettings[index])
                {
                    string[] bulletSceneOptions = ((TestSelect_Revolver)target).SceneNameEnum;
                    SerializedProperty bulletSceneIndexProp = element.FindPropertyRelative("m_sceneListIndex");
                    int newIndex = EditorGUI.Popup(new Rect(sceneSwitchAreaX, innerY, sceneSwitchAreaWidth, toggleRowHeight), "", bulletSceneIndexProp.intValue, bulletSceneOptions);
                    bulletSceneIndexProp.intValue = newIndex;
                    innerY += toggleRowHeight + padding;
                }
                GUI.EndGroup();
            };

            magazineList.elementHeightCallback = (int index) =>
            {
                float height = toggleRowHeight + padding;
                if (index < showDetailsList.Count && showDetailsList[index])
                {
                    float h = detailRowHeight + padding;
                    SerializedProperty tempProp = magazineList.serializedProperty.GetArrayElementAtIndex(index);
                    int evIndex = sharedEventTypeIndex;
                    SerializedProperty eventProp = tempProp.FindPropertyRelative(evIndex == 0 ? "m_evOnEnter" :
                                                             evIndex == 1 ? "m_evOnExit" :
                                                             evIndex == 2 ? "m_evOnConfirm" : "m_evOnCancel");
                    h += 3f;
                    h += EditorGUI.GetPropertyHeight(eventProp, new GUIContent(""), true) + padding;
                    h += toggleRowHeight + padding;

                    DisplayOption currentOption = bulletDisplayOptions[index];
                    if (currentOption == DisplayOption.Layer)
                        h += EditorGUI.GetPropertyHeight(tempProp.FindPropertyRelative("m_levelLayer"), new GUIContent("Level Layer"), true) + padding;
                    else if (currentOption == DisplayOption.Number)
                        h += EditorGUI.GetPropertyHeight(tempProp.FindPropertyRelative("m_levelNumber"), new GUIContent("Level Number"), true) + padding;
                    else
                    {
                        float standardHeight = EditorGUI.GetPropertyHeight(tempProp.FindPropertyRelative("m_levelLayer"), new GUIContent("Level Layer"), true);
                        h += standardHeight + padding;
                    }
                    h += toggleRowHeight + padding;
                    if (showBulletSceneSwitchSettings[index])
                        h += toggleRowHeight + padding;
                    height += h;
                }
                return height;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty magazineProp = serializedObject.FindProperty("m_Magazine");
            while (showDetailsList.Count < magazineProp.arraySize)
            {
                showDetailsList.Add(false);
                bulletDisplayOptions.Add(DisplayOption.None);
                showBulletSceneSwitchSettings.Add(false);
            }
            while (showDetailsList.Count > magazineProp.arraySize)
            {
                showDetailsList.RemoveAt(showDetailsList.Count - 1);
                bulletDisplayOptions.RemoveAt(bulletDisplayOptions.Count - 1);
                showBulletSceneSwitchSettings.RemoveAt(showBulletSceneSwitchSettings.Count - 1);
            }

            magazineList.DoLayoutList();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("当前选中选项", TestSelect_SaticAction.StaticData.GetNumber_SelectedOption().ToString());
            EditorGUILayout.Space();

            // —— Common 区域 —— 
            EditorGUILayout.LabelField("Common Variables（公共变量）", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GetCommonStyle());
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_evOnCancelCommon"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_evOnMoveLeft"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_evOnMoveRight"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_evOnConfirmCommon"), true);

                EditorGUILayout.Space();
                // 同一行中显示 Scene Switch 折页按钮与场景下拉枚举（下拉框不显示标签文本）
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Scene Switch", GUILayout.Width(100)))
                        showCommonSceneSwitchSettings = !showCommonSceneSwitchSettings;
                    if (showCommonSceneSwitchSettings)
                    {
                        // 新增：显示 m_manualSceneNameEnum 字段

                        string[] sceneOptions = ((TestSelect_Revolver)target).SceneNameEnum;
                        SerializedProperty selectedIndexProp = serializedObject.FindProperty("m_selectedSceneIndex");
                        int newIndex = EditorGUILayout.Popup(selectedIndexProp.intValue, sceneOptions, GUILayout.ExpandWidth(true));
                        selectedIndexProp.intValue = newIndex;
                    }

                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_manualSceneNameEnum"), new GUIContent("Scene 名称列表"), true);

            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private GUIStyle GetCommonStyle()
        {
            if (commonStyle == null)
            {
                GUIStyle fallback = EditorStyles.helpBox ?? GUI.skin.box;
                commonStyle = new GUIStyle(fallback);
                commonStyle.normal.background = MakeTex(2, 2, HexToColor("#CCFFCC"));
            }
            return commonStyle;
        }

        private GUIStyle GetBulletStyle()
        {
            if (bulletStyle == null)
            {
                bulletStyle = new GUIStyle();
                bulletStyle.normal.background = MakeTex(2, 2, HexToColor("#B3B3B3"));
                bulletStyle.padding = new RectOffset(6, 6, 4, 4);
            }
            return bulletStyle;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
