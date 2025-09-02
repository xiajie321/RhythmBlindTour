using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 非TMP的 Text
using TMPro;          // TMP_Text
using Qf.Systems;

public class mInputMappingConfigurator : MonoBehaviour
{
    public enum InputModeType { None, Edit, Play, UI }
    // ++ 增加 Page4
    private enum UIPage { Page1 = 1, Page2 = 2, Page3 = 3, Page4 = 4 }
    private enum ModifierState { None, Ctrl, Shift, CtrlShift }
    public enum ComboPriority { CtrlFirst, ShiftFirst }

    [Header("当前输入响应模式")]
    public InputModeType inputModeType = InputModeType.UI;

    [Header("默认音效")]
    public AudioClip keySoundDefault;
    public AudioClip keySoundShift;
    public AudioClip keySoundCtrl;
    public AudioSource keySoundSource;

    [Header("UI 模式：组合键策略（切页键优先级低于此设置）")]
    [Tooltip("按住 Shift 时是否屏蔽普通页的单键映射（仍会处理 Shift 组合列表）")]
    public bool suppressBaseWhenShift = true;
    [Tooltip("按住 Ctrl 时是否屏蔽普通页的单键映射（仍会处理 Ctrl 组合列表）")]
    public bool suppressBaseWhenCtrl = true;
    [Tooltip("同时按住 Ctrl+Shift 时，是否屏蔽普通页的单键映射（仍会处理优先的组合列表）")]
    public bool suppressBaseWhenCtrlShift = true;

    [Tooltip("同时按住 Ctrl+Shift 时，优先使用哪个组合列表")]
    public ComboPriority comboPriorityWhenBoth = ComboPriority.CtrlFirst;

    [Header("UI 模式：触发当前页功能时朗读 TTS")]
    public bool uiSpeakOnTrigger = true;

    // ---------- 全局快捷键 ----------
    [Header("Global 全局快捷键（不受模式控制）")]
    public List<UIInputMapping> mGlobalInputMappings = new();
    [Tooltip("全局快捷键触发时也朗读 TTS")]
    public bool globalSpeakOnTrigger = true;

    [Header("UI 页面切换（使用 UIInputMapping；优先级：全局 > 页面切换 > 页内）")]
    public UIInputMapping uiSwitchToPage1;   // 建议 groupName="UI.Switch.Page1", key=F1, trigger=Down
    public UIInputMapping uiSwitchToPage2;   // 建议 groupName="UI.Switch.Page2", key=F2
    public UIInputMapping uiSwitchToPage3;   // 建议 groupName="UI.Switch.Page3", key=F3
    // ++ 新增 Page4 的切页映射
    public UIInputMapping uiSwitchToPage4;   // 建议 groupName="UI.Switch.Page4", key=F4

    [Header("（后备）当上述未配置时使用的 KeyCode")]
    public KeyCode uiSwitchToPage1Key = KeyCode.F1;
    public KeyCode uiSwitchToPage2Key = KeyCode.F2;
    public KeyCode uiSwitchToPage3Key = KeyCode.F3;
    // ++ 新增 Page4 的后备 KeyCode
    public KeyCode uiSwitchToPage4Key = KeyCode.F4;

    [Header("页面切换行为")]
    [Tooltip("仅在 UI 模式时允许页面切换")]
    public bool enablePageSwitchInUI = true;

    // 可选提示音的按键状态
    private bool lastShiftPressed = false;
    private bool lastCtrlPressed = false;

    // ===== Edit 模式（Inspector 直配）=====
    [Header("Edit 模式键位设置（每个操作一个键）")]
    public KeyCode mKey_Quit = KeyCode.Escape;
    public KeyCode mKey_Sure = KeyCode.Return;
    public KeyCode mKey_SwipeUp = KeyCode.UpArrow;
    public KeyCode mKey_SwipeDown = KeyCode.DownArrow;
    public KeyCode mKey_SwipeLeft = KeyCode.LeftArrow;
    public KeyCode mKey_SwipeRight = KeyCode.RightArrow;
    public KeyCode mKey_Click = KeyCode.Space;

    // ===== UI 模式（Inspector 直配）=====
    [Header("UI 模式 - Page1（保留原有配置，不改名不清空）")]
    public List<UIInputMapping> mUIInputMappings = new();

    [Header("UI 模式 - Page2")]
    public List<UIInputMapping> mUIInputMappings_Page2 = new();

    [Header("UI 模式 - Page3")]
    public List<UIInputMapping> mUIInputMappings_Page3 = new();

    // ++ 新增 Page4 的列表
    [Header("UI 模式 - Page4")]
    public List<UIInputMapping> mUIInputMappings_Page4 = new();

    // 组合列表：只有当按住修饰键时才会使用
    [Header("UI 模式 - Shift 组合映射列表（按住 Shift 时触发此列表，而非普通页）")]
    public List<UIInputMapping> mUIInputMappings_Shift = new();

    [Header("UI 模式 - Ctrl 组合映射列表（按住 Ctrl 时触发此列表，而非普通页）")]
    public List<UIInputMapping> mUIInputMappings_Ctrl = new();

    // ===== Play 模式（Inspector 直配）=====
    [Header("Play 模式键位设置（每个操作多个键）")]
    public KeyCode[] pKeys_Quit = new KeyCode[] { KeyCode.Escape };
    public KeyCode[] pKeys_Sure = new KeyCode[] { KeyCode.Return, KeyCode.Space };
    public KeyCode[] pKeys_SwipeUp = new KeyCode[] { KeyCode.W, KeyCode.UpArrow };
    public KeyCode[] pKeys_SwipeDown = new KeyCode[] { KeyCode.S, KeyCode.DownArrow };
    public KeyCode[] pKeys_SwipeLeft = new KeyCode[] { KeyCode.A, KeyCode.LeftArrow };
    public KeyCode[] pKeys_SwipeRight = new KeyCode[] { KeyCode.D, KeyCode.RightArrow };
    public KeyCode[] pKeys_Click = new KeyCode[] { KeyCode.Space, KeyCode.Return };

    // 运行期
    private InputModeType mLastAppliedMode = InputModeType.None;
    [SerializeField] private UIPage currentUIPage = UIPage.Page1;

    // —— 每帧同键消费集合（跨全局与模式通用；全局优先） ——
    private readonly HashSet<KeyCode> consumedKeysThisFrame = new HashSet<KeyCode>();

    // —— 为“后备 KeyCode 切页”分配固定组名（仅内部使用） ——
    private const string kPage1GroupFallback = "__UI__SwitchPage1";
    private const string kPage2GroupFallback = "__UI__SwitchPage2";
    private const string kPage3GroupFallback = "__UI__SwitchPage3";
    // ++ 新增 Page4 的后备组名
    private const string kPage4GroupFallback = "__UI__SwitchPage4";

    // ---------- 粘滞修饰键修复 ----------
    [Header("粘滞修饰键修复")]
    [Tooltip("修复弹窗/切出后 Ctrl/Shift 被卡住的问题")]
    public bool fixStickyModifiers = true;

    [Tooltip("在 macOS 将 Command 视为 Ctrl（组合键识别）")]
    public bool treatCommandAsCtrl = true;

    private bool _awaitModifiersRelease = false;  // 焦点恢复后，等待一次“所有修饰键都松开”

    // ---------- 组合键弹窗修复：延迟回调 ----------
    [Header("组合键弹窗修复（延迟回调）")]
    [Tooltip("UI模式下，触发功能后先清键盘状态并短暂不接收输入，延迟再执行回调")]
    public bool deferCallbacksInUIMode = true;

    [Range(0f, 0.5f)]
    public float deferDelaySeconds = 0.1f;

    private bool _deferringInputs = false; // 延迟期间完全不处理输入
    private Coroutine _deferCo;
    // —— 安全朗读（帧末触发）版本号：每次请求自增，帧末只执行“最后一个” —— 
    private int _safeSpeakVersion = 0;


    void Reset()
    {
        // 给新挂载的组件默认切页映射
        uiSwitchToPage1.groupName = "UI.Switch.Page1";
        uiSwitchToPage1.keyCode = KeyCode.F1;
        uiSwitchToPage1.triggerType = InputTriggerType.Down;

        uiSwitchToPage2.groupName = "UI.Switch.Page2";
        uiSwitchToPage2.keyCode = KeyCode.F2;
        uiSwitchToPage2.triggerType = InputTriggerType.Down;

        uiSwitchToPage3.groupName = "UI.Switch.Page3";
        uiSwitchToPage3.keyCode = KeyCode.F3;
        uiSwitchToPage3.triggerType = InputTriggerType.Down;

        // ++ Page4 默认
        uiSwitchToPage4.groupName = "UI.Switch.Page4";
        uiSwitchToPage4.keyCode = KeyCode.F4;
        uiSwitchToPage4.triggerType = InputTriggerType.Down;
    }

    void Awake()
    {
        RegisterGlobalInputs();
        ApplyModeChange(inputModeType);
        mLastAppliedMode = inputModeType;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!fixStickyModifiers) return;

        if (hasFocus)
        {
            Input.ResetInputAxes();
            _awaitModifiersRelease = true;
            lastCtrlPressed = false;
            lastShiftPressed = false;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (!fixStickyModifiers) return;

        if (!paused) // 从暂停 -> 继续，相当于回到前台
        {
            Input.ResetInputAxes();
            _awaitModifiersRelease = true;
            lastCtrlPressed = false;
            lastShiftPressed = false;
        }
    }

    void LateUpdate()
    {
        // 延迟期间：完全不处理输入（全局/切页/页内都阻断）
        if (_deferringInputs) return;

        // —— 1) 全局（最高优先级） ——
        consumedKeysThisFrame.Clear();
        ProcessMappingList(mGlobalInputMappings, globalSpeakOnTrigger);

        // 可选：Shift/Ctrl 声音提示
        bool nowShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (nowShift && !lastShiftPressed) PlaySpecialKeySound(keySoundShift);
        lastShiftPressed = nowShift;

        bool nowCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (nowCtrl && !lastCtrlPressed) PlaySpecialKeySound(keySoundCtrl);
        lastCtrlPressed = nowCtrl;

        // 模式切换检测
        if (inputModeType != mLastAppliedMode)
        {
            ApplyModeChange(inputModeType);
            mLastAppliedMode = inputModeType;
        }

        switch (inputModeType)
        {
            case InputModeType.UI:
                // —— 先判断修饰键状态（供切页 + 页内共同使用）——
                var state = GetModifierState();
                var listToProcess = SelectListByModifier(state);

                bool blockBase = false;
                if (state != ModifierState.None)
                {
                    blockBase = state switch
                    {
                        ModifierState.CtrlShift => suppressBaseWhenCtrlShift,
                        ModifierState.Ctrl => suppressBaseWhenCtrl,
                        ModifierState.Shift => suppressBaseWhenShift,
                        _ => false
                    };
                }

                // —— 页面切换：也应用 suppress 策略（被屏蔽普通页时，同样屏蔽切页）——
                if (enablePageSwitchInUI && !blockBase)
                {
                    if (ProcessPageSwitch(uiSwitchToPage1, UIPage.Page1, uiSwitchToPage1Key)) return;
                    if (ProcessPageSwitch(uiSwitchToPage2, UIPage.Page2, uiSwitchToPage2Key)) return;
                    if (ProcessPageSwitch(uiSwitchToPage3, UIPage.Page3, uiSwitchToPage3Key)) return;
                    // ++ 追加 Page4
                    if (ProcessPageSwitch(uiSwitchToPage4, UIPage.Page4, uiSwitchToPage4Key)) return;
                }

                // —— 页内映射 —— 
                if (!blockBase)
                {
                    // 先处理组合列表（若有修饰键），再处理当前页的普通映射
                    ProcessMappingList(listToProcess, uiSpeakOnTrigger);
                    ProcessMappingList(GetActiveUIPageList(), uiSpeakOnTrigger);
                }
                else
                {
                    // 屏蔽普通页：只处理组合列表
                    ProcessMappingList(listToProcess, uiSpeakOnTrigger);
                }
                break;

            case InputModeType.Edit:
            case InputModeType.Play:
                CheckStandardInputs();
                break;
        }
    }

    // === 公开模式切换方法 ===
    public void SwitchToUI()
    {
        inputModeType = InputModeType.UI;
        ApplyModeChange(inputModeType);
        mLastAppliedMode = inputModeType;
        Debug.Log("[InputMapping] 切换到 UI 模式");
    }

    public void SwitchToEdit()
    {
        inputModeType = InputModeType.Edit;
        ApplyModeChange(inputModeType);
        mLastAppliedMode = inputModeType;
        Debug.Log("[InputMapping] 切换到 Edit 模式");
    }

    public void SwitchToPlay()
    {
        inputModeType = InputModeType.Play;
        ApplyModeChange(inputModeType);
        mLastAppliedMode = inputModeType;
        Debug.Log("[InputMapping] 切换到 Play 模式");
    }

    /// <summary>
    /// 在调用系统文件对话框（保存/打开）后可手动调用，清一次输入并防止修饰键卡住。
    /// </summary>
    public void FlushModifiersNow()
    {
        if (!fixStickyModifiers) return;
        Input.ResetInputAxes();
        _awaitModifiersRelease = true;
        lastCtrlPressed = false;
        lastShiftPressed = false;
    }

    // ======= 页面切换：使用 UIInputMapping（有配置）→ 否则使用后备 KeyCode =======
    private bool ProcessPageSwitch(UIInputMapping mapping, UIPage targetPage, KeyCode fallbackKey)
    {
        // 若全局已消费（同帧同键），直接跳过
        if (!string.IsNullOrEmpty(mapping.groupName) && consumedKeysThisFrame.Contains(mapping.keyCode))
            return false;

        if (!string.IsNullOrEmpty(mapping.groupName))
        {
            if (IsUIInputMappingTriggered(mapping))
            {
                consumedKeysThisFrame.Add(mapping.keyCode);

                // 延迟后再切页 + 触发绑定的 targetItem + 互斥切换
                StartDeferredInvoke(mapping, uiSpeakOnTrigger, invoke: () =>
                {
                    SwitchUIToPage(targetPage); // 切页
                    if (mapping.targetItem != null && mapping.targetItem.isActiveAndEnabled)
                        mapping.targetItem.TriggerClick();
                    ApplyMutualToggle(mapping);
                });
                return true; // 本帧结束
            }
        }
        else
        {
            if (fallbackKey != KeyCode.None && Input.GetKeyDown(fallbackKey))
            {
                consumedKeysThisFrame.Add(fallbackKey);

                // 没有结构体配置时，也走延迟（此时无专属TTS，但会播放默认音效）
                var fake = new UIInputMapping(); // 空结构占位
                StartDeferredInvoke(fake, speakOnTrigger: false, invoke: () =>
                {
                    SwitchUIToPage(targetPage);
                });
                return true;
            }
        }
        return false;
    }

    // ===== 列表处理主函数（全局与模式共用） =====
    private void ProcessMappingList(List<UIInputMapping> list, bool speakOnTrigger)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            var map = list[i];

            // 仅对“可交互对象”触发
            if (!IsInteractable(map)) continue;

            // 同键消费：若本帧已被其他条目消费（含全局/切页），则跳过
            if (consumedKeysThisFrame.Contains(map.keyCode)) continue;

            if (IsUIInputMappingTriggered(map))
            {
                // 消费该键
                consumedKeysThisFrame.Add(map.keyCode);

                // —— 延迟回调：统一封装音效/TTS/清输入/短暂 None/恢复UI/再触发事件 —— 
                StartDeferredInvoke(map, speakOnTrigger, invoke: () =>
                {
                    map.targetItem?.TriggerClick();
                    ApplyMutualToggle(map);
                });
            }
        }
    }

    // ====== 延迟回调内核 ======
    private void StartDeferredInvoke(UIInputMapping map, bool speakOnTrigger, System.Action invoke)
    {
        if (!deferCallbacksInUIMode || inputModeType != InputModeType.UI)
        {
            // 不启用延迟或非UI模式：直接执行（保留原即刻回调）
            PlayKeySound(map);

            bool doSpeak = speakOnTrigger && !map.cancelTTS;
            if (doSpeak)
                SpeakFor(map);

            invoke?.Invoke();
            return;
        }
        if (_deferCo != null) StopCoroutine(_deferCo);
        _deferCo = StartCoroutine(CoDeferredInvoke(map, speakOnTrigger, invoke));
    }


    private System.Collections.IEnumerator CoDeferredInvoke(UIInputMapping map, bool speakOnTrigger, System.Action invoke)
    {
        _deferringInputs = true;

        // 1) 先做：按键音、TTS
        PlayKeySound(map);

        bool doSpeak = speakOnTrigger && !map.cancelTTS;
        if (doSpeak)
            SpeakFor(map);

        // 2) 短暂不接收输入：清一次轴 + 切到 None
        Input.ResetInputAxes();
        ApplyModeChange(InputModeType.None);
        mLastAppliedMode = InputModeType.None;
        inputModeType = InputModeType.None;

        // 3) 等待（真实时间）
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, deferDelaySeconds));

        // 4) 恢复 UI 模式再执行回调
        inputModeType = InputModeType.UI;
        ApplyModeChange(inputModeType);
        mLastAppliedMode = inputModeType;

        // 保险：再清一次粘滞修饰键
        FlushModifiersNow();

        // 真正执行该条目的回调（页面切换/按钮事件/互斥切换等）
        invoke?.Invoke();

        _deferringInputs = false;
    }


    private void SpeakFor(UIInputMapping map)
    {
        string t = null;
        if (map.ttsSourceTMP != null && map.ttsSourceTMP.isActiveAndEnabled)
            t = map.ttsSourceTMP.text;
        else if (map.ttsSourceText != null && map.ttsSourceText.isActiveAndEnabled)
            t = map.ttsSourceText.text;
        else
            t = map.GetTTSText();

        // 选项：保持与你当前一致（立即朗读时也会打断旧的）
        var opt = new mTTS.Options
        {
            interruptMode = mTTS.InterruptMode.Interrupt,
            writeMode = mTTS.WriteMode.Replace,
            simulateDuration = false
        };

        if (string.IsNullOrWhiteSpace(t))
        {
            // 空文本也要“中断当前朗读”
            if (map.ttsSafe)
            {
                // —— 安全朗读：把“中断”也延迟到帧末，只保留最后一次 —— 
                ScheduleSafeSpeak(text: null, emptyMeansStop: true, opt: null);
            }
            else
            {
                // —— 立即中断 —— 
                mTTS.Stop(clearQueue: false, clearTMP: false);
            }
            return;
        }

        // 非空：朗读文本
        if (map.ttsSafe)
        {
            // —— 安全朗读：仅在帧末执行“最后一次”的朗读 —— 
            ScheduleSafeSpeak(text: t, emptyMeansStop: false, opt: opt);
        }
        else
        {
            // —— 立即朗读 —— 
            mTTS.Speak(t, opt);
        }
    }


    // ===== 修饰键判断 & 列表选择（仅 UI 模式用） =====
    private ModifierState GetModifierState()
    {
        // 是否把 Command 当成 Ctrl（macOS 常用）
        bool cmd = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                    || (treatCommandAsCtrl && cmd);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 焦点恢复后，直到所有修饰键松开为止，忽略修饰键（防止“卡住”）
        if (fixStickyModifiers && _awaitModifiersRelease)
        {
            if (!ctrl && !shift) // 确认所有修饰键都已松开
                _awaitModifiersRelease = false;
            return ModifierState.None;
        }

        if (ctrl && shift) return ModifierState.CtrlShift;
        if (ctrl) return ModifierState.Ctrl;
        if (shift) return ModifierState.Shift;
        return ModifierState.None;
    }

    private List<UIInputMapping> SelectListByModifier(ModifierState state)
    {
        switch (state)
        {
            case ModifierState.Ctrl:
                return mUIInputMappings_Ctrl;
            case ModifierState.Shift:
                return mUIInputMappings_Shift;
            case ModifierState.CtrlShift:
                return (comboPriorityWhenBoth == ComboPriority.CtrlFirst)
                    ? (mUIInputMappings_Ctrl?.Count > 0 ? mUIInputMappings_Ctrl : mUIInputMappings_Shift)
                    : (mUIInputMappings_Shift?.Count > 0 ? mUIInputMappings_Shift : mUIInputMappings_Ctrl);
            case ModifierState.None:
            default:
                return GetActiveUIPageList();
        }
    }

    private bool IsInteractable(UIInputMapping map)
    {
        if (map.targetItem == null) return false;

        // 开了这个开关：忽略 isActiveAndEnabled / activeInHierarchy，认为可触发
        if (map.allowInvokeWhenDisabled) return true;

        var go = map.targetItem.gameObject;
        return go.activeInHierarchy && map.targetItem.isActiveAndEnabled;
    }

    // ===== UI 页面逻辑 =====
    private List<UIInputMapping> GetActiveUIPageList()
    {
        return currentUIPage switch
        {
            UIPage.Page1 => mUIInputMappings,
            UIPage.Page2 => mUIInputMappings_Page2,
            UIPage.Page3 => mUIInputMappings_Page3,
            UIPage.Page4 => mUIInputMappings_Page4,
            _ => mUIInputMappings
        };
    }

    public void SwitchUIToPage1() => SwitchUIToPage(UIPage.Page1);
    public void SwitchUIToPage2() => SwitchUIToPage(UIPage.Page2);
    public void SwitchUIToPage3() => SwitchUIToPage(UIPage.Page3);
    // ++ 新增 Page4 切换方法
    public void SwitchUIToPage4() => SwitchUIToPage(UIPage.Page4);

    private void SwitchUIToPage(UIPage page)
    {
        if (currentUIPage == page) return;
        currentUIPage = page;
        RegisterUIPageInputs(); // 只注册当前页 + 组合列表（切页映射在 ApplyModeChange 时已注册或走后备）
        Debug.Log($"[UI] 切换至 {page}");
    }

    private void RegisterUIPageInputs()
    {
        // 先清理所有已知 UI 组名，保证互不干扰（不清全局）
        ClearUIGroupKeys(mUIInputMappings);
        ClearUIGroupKeys(mUIInputMappings_Page2);
        ClearUIGroupKeys(mUIInputMappings_Page3);
        // ++ 清 Page4
        ClearUIGroupKeys(mUIInputMappings_Page4);
        ClearUIGroupKeys(mUIInputMappings_Shift);
        ClearUIGroupKeys(mUIInputMappings_Ctrl);

        // 注册当前页（普通单键）
        foreach (var map in GetActiveUIPageList())
        {
            InputSystems.ClearKey(map.groupName);
            InputSystems.AddKey(map.groupName, map.keyCode);
        }

        // 注册组合列表（始终注册，只有按住修饰键时才会命中流程）
        foreach (var map in mUIInputMappings_Shift)
        {
            InputSystems.ClearKey(map.groupName);
            InputSystems.AddKey(map.groupName, map.keyCode);
        }
        foreach (var map in mUIInputMappings_Ctrl)
        {
            InputSystems.ClearKey(map.groupName);
            InputSystems.AddKey(map.groupName, map.keyCode);
        }
    }

    // —— 注册全局快捷键 —— 
    private void RegisterGlobalInputs()
    {
        ClearUIGroupKeys(mGlobalInputMappings);
        foreach (var map in mGlobalInputMappings)
        {
            InputSystems.ClearKey(map.groupName);
            InputSystems.AddKey(map.groupName, map.keyCode);
        }
    }

    private void ClearUIGroupKeys(List<UIInputMapping> list)
    {
        if (list == null) return;
        foreach (var map in list)
        {
            if (!string.IsNullOrEmpty(map.groupName))
                InputSystems.ClearKey(map.groupName);
        }
    }

    // ===== 触发与音效 =====
    private bool IsUIInputMappingTriggered(UIInputMapping map)
    {
        return map.triggerType switch
        {
            InputTriggerType.Click => InputSystems.InputQuery(map.groupName),
            InputTriggerType.Down => InputSystems.InputQuery(map.groupName),
            InputTriggerType.Up => Input.GetKeyUp(map.keyCode),
            _ => false
        };
    }

    private void PlayKeySound(UIInputMapping map)
    {
        if (keySoundSource == null || map.keySilent) return;
        var clip = map.keySound != null ? map.keySound : keySoundDefault;
        if (clip != null)
        {
            keySoundSource.clip = clip;
            keySoundSource.Play();
        }
    }

    // —— 互斥切换：触发后禁用自己并启用伙伴（可选） ——
    private void ApplyMutualToggle(UIInputMapping map)
    {
        if (!map.toggleEnablePartner && !map.toggleDisableSelf) return;

        // 先处理“自己”
        if (map.toggleDisableSelf && map.targetItem != null)
        {
            if (map.toggleUseGameObjectActive)
            {
                var selfGO = map.targetItem.gameObject;
                if (selfGO.activeSelf) selfGO.SetActive(false);
            }
            else
            {
                if (map.targetItem.enabled) map.targetItem.enabled = false;
                map.targetItem.SetClickOff(); // 确保点击开关也关闭
            }
        }

        // 再处理“伙伴”
        if (map.toggleEnablePartner && map.togglePartner != null)
        {
            if (map.toggleUseGameObjectActive)
            {
                var partnerGO = map.togglePartner.gameObject;
                if (!partnerGO.activeSelf) partnerGO.SetActive(true);
            }
            else
            {
                if (!map.togglePartner.enabled) map.togglePartner.enabled = true;
                map.togglePartner.SetClickOn(); // 确保点击开关也开启
            }
        }
    }

    private void PlaySpecialKeySound(AudioClip clip)
    {
        if (keySoundSource == null) return;
        if (clip == null) clip = keySoundDefault;
        if (clip != null)
        {
            keySoundSource.clip = clip;
            keySoundSource.Play();
        }
    }

    private void ScheduleSafeSpeak(string text, bool emptyMeansStop, mTTS.Options opt)
    {
        int ver = ++_safeSpeakVersion; // 记录这次请求的版本号
        StartCoroutine(CoSafeSpeakAtFrameEnd(ver, text, emptyMeansStop, opt));
    }

    private System.Collections.IEnumerator CoSafeSpeakAtFrameEnd(int ver, string text, bool emptyMeansStop, mTTS.Options opt)
    {
        // 等到“当帧最后”
        yield return new WaitForEndOfFrame();

        // 若期间发生了更新（来了更新的朗读请求），丢弃旧请求
        if (ver != _safeSpeakVersion) yield break;

        if (emptyMeansStop)
            mTTS.Stop(clearQueue: false, clearTMP: false);
        else
            mTTS.Speak(text, opt);
    }

    // ===== Edit/Play 标准输入（Inspector 直配）=====
    private void CheckStandardInputs()
    {
        if (InputSystems.InputQuery("Click")) Debug.Log("[Input] Click 被触发");
        if (InputSystems.InputQuery("Sure")) Debug.Log("[Input] Sure 被触发");
        if (InputSystems.InputQuery("Quit")) Debug.Log("[Input] Quit 被触发");
        if (InputSystems.InputQuery("SwipeUp")) Debug.Log("[Input] SwipeUp 被触发");
        if (InputSystems.InputQuery("SwipeDown")) Debug.Log("[Input] SwipeDown 被触发");
        if (InputSystems.InputQuery("SwipeLeft")) Debug.Log("[Input] SwipeLeft 被触发");
        if (InputSystems.InputQuery("SwipeRight")) Debug.Log("[Input] SwipeRight 被触发");
    }

    private void ApplyModeChange(InputModeType mode)
    {
        // 清理一遍标准组名，避免跨模式残留
        foreach (var key in new[] { "Click", "Sure", "Quit", "SwipeUp", "SwipeDown", "SwipeLeft", "SwipeRight", "PlayClick" })
            InputSystems.ClearKey(key);

        // 全局键不受模式影响，但在切换时也重新注册一次，保证一致性
        RegisterGlobalInputs();

        // 同步注册“页面切换”的映射组（仅当配置了 groupName 时）
        RegisterPageSwitchInputs();

        switch (mode)
        {
            case InputModeType.None:
                break;

            case InputModeType.Edit:
                RegisterEditInputsFromInspector();
                break;

            case InputModeType.Play:
                RegisterPlayInputsFromInspector();
                break;

            case InputModeType.UI:
                RegisterUIPageInputs(); // 注册当前页 + 组合列表
                break;
        }
    }

    // —— 注册“页面切换”的映射组（仅当配置使用结构体时） ——
    private void RegisterPageSwitchInputs()
    {
        // 先清理旧的（包括“后备组名”）
        InputSystems.ClearKey(kPage1GroupFallback);
        InputSystems.ClearKey(kPage2GroupFallback);
        InputSystems.ClearKey(kPage3GroupFallback);
        // ++ 清理 Page4 的后备组名
        InputSystems.ClearKey(kPage4GroupFallback);

        if (!string.IsNullOrEmpty(uiSwitchToPage1.groupName))
        {
            InputSystems.ClearKey(uiSwitchToPage1.groupName);
            InputSystems.AddKey(uiSwitchToPage1.groupName, uiSwitchToPage1.keyCode);
        }
        else if (uiSwitchToPage1Key != KeyCode.None)
        {
            InputSystems.ClearKey(kPage1GroupFallback);
            InputSystems.AddKey(kPage1GroupFallback, uiSwitchToPage1Key);
        }

        if (!string.IsNullOrEmpty(uiSwitchToPage2.groupName))
        {
            InputSystems.ClearKey(uiSwitchToPage2.groupName);
            InputSystems.AddKey(uiSwitchToPage2.groupName, uiSwitchToPage2.keyCode);
        }
        else if (uiSwitchToPage2Key != KeyCode.None)
        {
            InputSystems.ClearKey(kPage2GroupFallback);
            InputSystems.AddKey(kPage2GroupFallback, uiSwitchToPage2Key);
        }

        if (!string.IsNullOrEmpty(uiSwitchToPage3.groupName))
        {
            InputSystems.ClearKey(uiSwitchToPage3.groupName);
            InputSystems.AddKey(uiSwitchToPage3.groupName, uiSwitchToPage3.keyCode);
        }
        else if (uiSwitchToPage3Key != KeyCode.None)
        {
            InputSystems.ClearKey(kPage3GroupFallback);
            InputSystems.AddKey(kPage3GroupFallback, uiSwitchToPage3Key);
        }

        // ++ Page4 同步注册
        if (!string.IsNullOrEmpty(uiSwitchToPage4.groupName))
        {
            InputSystems.ClearKey(uiSwitchToPage4.groupName);
            InputSystems.AddKey(uiSwitchToPage4.groupName, uiSwitchToPage4.keyCode);
        }
        else if (uiSwitchToPage4Key != KeyCode.None)
        {
            InputSystems.ClearKey(kPage4GroupFallback);
            InputSystems.AddKey(kPage4GroupFallback, uiSwitchToPage4Key);
        }
    }

    private void RegisterEditInputsFromInspector()
    {
        InputSystems.ClearKey("Quit"); InputSystems.AddKey("Quit", mKey_Quit);
        InputSystems.ClearKey("Sure"); InputSystems.AddKey("Sure", mKey_Sure);
        InputSystems.ClearKey("SwipeUp"); InputSystems.AddKey("SwipeUp", mKey_SwipeUp);
        InputSystems.ClearKey("SwipeDown"); InputSystems.AddKey("SwipeDown", mKey_SwipeDown);
        InputSystems.ClearKey("SwipeLeft"); InputSystems.AddKey("SwipeLeft", mKey_SwipeLeft);
        InputSystems.ClearKey("SwipeRight"); InputSystems.AddKey("SwipeRight", mKey_SwipeRight);
        InputSystems.ClearKey("Click"); InputSystems.AddKey("Click", mKey_Click);
        Debug.Log("[Edit] 已根据 Inspector 注册按键映射。");
    }

    private void RegisterPlayInputsFromInspector()
    {
        if (pKeys_Quit != null) { InputSystems.ClearKey("Quit"); InputSystems.AddKey("Quit", pKeys_Quit); }
        if (pKeys_Sure != null) { InputSystems.ClearKey("Sure"); InputSystems.AddKey("Sure", pKeys_Sure); }
        if (pKeys_SwipeUp != null) { InputSystems.ClearKey("SwipeUp"); InputSystems.AddKey("SwipeUp", pKeys_SwipeUp); }
        if (pKeys_SwipeDown != null) { InputSystems.ClearKey("SwipeDown"); InputSystems.AddKey("SwipeDown", pKeys_SwipeDown); }
        if (pKeys_SwipeLeft != null) { InputSystems.ClearKey("SwipeLeft"); InputSystems.AddKey("SwipeLeft", pKeys_SwipeLeft); }
        if (pKeys_SwipeRight != null) { InputSystems.ClearKey("SwipeRight"); InputSystems.AddKey("SwipeRight", pKeys_SwipeRight); }
        if (pKeys_Click != null) { InputSystems.ClearKey("Click"); InputSystems.AddKey("Click", pKeys_Click); }
        Debug.Log("[Play] 已根据 Inspector 注册按键映射。");
    }
}

/* ====== 结构保持不变，同时包含 TMP 源 ====== */
[System.Serializable]
public struct UIInputMapping
{
    public string groupName;
    public KeyCode keyCode;
    public InputTriggerType triggerType;
    public UIEventsItem targetItem;
    [Header("禁用状态下也可触发（忽略可交互检查）")]
    public bool allowInvokeWhenDisabled;


    [Header("触发功能时播放音效")]
    public AudioClip keySound;
    public bool keySilent;

    [Header("TTS 朗读来源优先级：TMP → Text → 备用 string")]
    public TMP_Text ttsSourceTMP;        // 优先从 TMP 读取
    public Text ttsSourceText;           // 非TMP的 Text

    [Header("触发功能时朗读文本（后备；保留原字段名）")]
    [TextArea][SerializeField] string ttsText;  // 保留，不改名
    public string GetTTSText() => ttsText;

    [Header("取消该映射的 TTS 朗读（为 true 则不朗读也不打断当前 TTS）")]
    public bool cancelTTS;
    [Header("TTS 安全朗读（帧末触发，仅保留最后一次）")]
    public bool ttsSafe;


    [Header("互斥切换（可选）：触发后禁用自己并启用伙伴")]
    public UIEventsItem togglePartner;      // 伙伴（第二个功能）
    public bool toggleDisableSelf;          // 触发后禁用自己
    public bool toggleEnablePartner;        // 触发后启用伙伴
    [Tooltip("true=启/禁GameObject，false=仅启/禁UIEventsItem组件")]
    public bool toggleUseGameObjectActive;  // 区分禁用对象或仅禁用组件
}


[System.Serializable]
public struct UIInputMappingSerializable
{
    public string groupName;
    public KeyCode keyCode;
    public InputTriggerType triggerType;
}

[System.Serializable]
public class UIInputMapWrapper
{
    public List<UIInputMappingSerializable> entries;
}

public enum InputTriggerType
{
    Click,
    Down,
    Up
}

[System.Serializable]
public class UIMapWrapper
{
    public List<UIInputMapping> uiMappings;
    public List<UIInputMapping> uiMappings_Shift;
    public List<UIInputMapping> uiMappings_Ctrl;
}
