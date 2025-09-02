using Assets.Scripts.Querys.AudioEdit;
using Qf.ClassDatas.AudioEdit;
using Qf.Commands.AudioEdit;
using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using UnityEngine;

public class UIMainAttributeSetPanel : MonoBehaviour, IController
{
    // ─────────────────────────────────────────────
    // 关卡音乐设置（BGM）—— 修改即覆盖（不走“应用全局”）
    // ─────────────────────────────────────────────
    [Header("关卡音乐设置")]
    [SerializeField] UIFileAttribute MainAudio;          // 关卡主音频（BGM）
    [SerializeField] UISliderAttribute MainAudioVolume;  // 关卡音乐音量（UI:0..200 ↔ 模型:0..1）

    // // ─────────────────────────────────────────────
    // // 全局设置（不立刻改鼓点；点击“应用全局”才批量覆盖到鼓点）
    // // ─────────────────────────────────────────────
    // [Header("全局设置（需点“应用全局”才覆盖到鼓点）")]
    // [SerializeField] UISliderAttribute TipVolumeGlobal;      // 全局-提示音量（UI:0..200 ↔ 模型:0..1）
    // [SerializeField] UISliderAttribute SucceedVolumeGlobal;  // 全局-成功音量
    // [SerializeField] UISliderAttribute LoseVolumeGlobal;     // 全局-失败音量
    // [SerializeField] UISliderAttribute DefaultVolumeGlobal;  // 全局-默认音量（若有每鼓点字段可写回）
    // [SerializeField] UIValueAttribute TimeOfExistenceGlobal; // 全局-判定时长（鼓点存在时间）
    // [SerializeField] UIValueAttribute TipAdvanceGlobal;      // 全局-预告音提前（原预告音偏移）

    // ─────────────────────────────────────────────
    // 鼓点类型设置（5种）—— 修改即刻批量应用本类型鼓点
    // ─────────────────────────────────────────────
    [System.Serializable]
    public class TypeBlock
    {
        [Header("鼓点类型")]
        public TheTypeOfOperation type;

        [Header("时间")]
        public UIValueAttribute TimeOfExistence;     // 判定时长（存在时间）
        public UIValueAttribute TipAdvance;          // 提示音提前（数据偏移）
        public UIValueAttribute TipPlayOffset;       // 仅播放时偏移（可选，需有字段）

        [Header("音频")]
        public UIFileAttribute TipAudio;             // 提示音频（ComeTip）
        public UIFileAttribute SucceedAudio;         // 回答/成功音频
        public UIFileAttribute LoseAudio;            // 失败音频
        public UIFileAttribute DefaultAudio;         // 默认音频（若你要按类型写回，可选）

        [Header("音量")]
        public UISliderAttribute TipVolume;          // UI:0..200 ↔ 模型:0..1
        public UISliderAttribute SucceedVolume;      // UI:0..200 ↔ 模型:0..1
        public UISliderAttribute LoseVolume;         // UI:0..200 ↔ 模型:0..1
        public UISliderAttribute DefaultVolume;      // UI:0..200 ↔ 模型:0..1（如有每鼓点字段）
    }

    [Header("类型设置：点击/上滑/下滑/左滑/右滑")]
    [SerializeField] TypeBlock ClickBlock;
    [SerializeField] TypeBlock SwipeUpBlock;
    [SerializeField] TypeBlock SwipeDownBlock;
    [SerializeField] TypeBlock SwipeLeftBlock;
    [SerializeField] TypeBlock SwipeRightBlock;

    private AudioEditModel editModel;
    public IArchitecture GetArchitecture() => GameBody.Interface;

    // —— 面板内抑制全量回显（防止把正在输入的值重刷掉）——
    private bool _suppressEcho = false;

    /// <summary>在本面板内“静默执行”一段写模型/发事件的逻辑：期间不触发本面板的 RefreshAllEcho</summary>
    private void DoQuietly(System.Action body)
    {
        bool was = _suppressEcho;
        _suppressEcho = true;
        try { body?.Invoke(); }
        finally { _suppressEcho = was; }
    }

    /// <summary>对外广播 UI 刷新事件，但本面板不做全量回显</summary>
    private void FireModelUiUpdated()
    {
        DoQuietly(() =>
        {
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
        });
    }

    private void OnEnable()
    {
        if (editModel == null) editModel = this.GetModel<AudioEditModel>();

        WireLevelMusic();  // 关卡音乐设置：立刻覆盖
        WireGlobal();      // 全局设置：只存值，不立刻覆盖鼓点
        WireType(ClickBlock);
        WireType(SwipeUpBlock);
        WireType(SwipeDownBlock);
        WireType(SwipeLeftBlock);
        WireType(SwipeRightBlock);

        // 任一处变更（如 SetPanel 批量写回）后，这里回显同步
        // 原：this.RegisterEvent<OnUpdateAudioEditDrumsUI>(_ => RefreshAllEcho()).UnRegisterWhenDisabled(gameObject);
        this.RegisterEvent<OnUpdateAudioEditDrumsUI>(_ =>
        {
            if (!_suppressEcho) RefreshAllEcho();
        }).UnRegisterWhenDisabled(gameObject);
        this.RegisterEvent<AudioEditModelLoad>(_ => RefreshAllEcho()).UnRegisterWhenDisabled(gameObject);

        RefreshAllEcho();
    }

    // ─────────────────────────────────────────────
    // 关卡音乐设置（改了就覆盖 editModel / 音量）
    // ─────────────────────────────────────────────
    void WireLevelMusic()
    {
        if (MainAudio) MainAudio.SetAction(v =>
        {
            var clip = (AudioClip)v;
            // 原来会直接 SendCommand + SendEvent，这里用静默包起来，避免本面板立刻全量回显
            DoQuietly(() =>
            {
                this.SendCommand(new SetAudioEditAudioCommand(clip));
                // 如果需要同时让其它 UI（如时间线）刷新，发事件也包在静默里
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
            });

            MainAudio.SetShowFileName(clip ? clip.name : null); // 局部回显自己
        });

        if (MainAudioVolume) MainAudioVolume.SetAction(v =>
        {
            if (editModel == null) return;
            DoQuietly(() =>
            {
                editModel.EditAudioClipVolume.Value = UiToModel01(v); // 只写模型，不全量回显
            });
            MainAudioVolume.SetValueShow(Model01ToUi(editModel.EditAudioClipVolume.Value));
        });
    }


    // ─────────────────────────────────────────────
    // 全局设置（只更新 editModel 的全局值；不立刻覆盖到鼓点）
    // 通过按钮调用 ApplyGlobalToAllDrums() 才批量写回到所有鼓点
    // ─────────────────────────────────────────────
    void WireGlobal()
    {
        // if (TipVolumeGlobal) TipVolumeGlobal.SetAction(v =>
        // {
        //     if (editModel == null) return;
        //     DoQuietly(() => editModel.PreAdventVolume.Value = UiToModel01(v));
        //     TipVolumeGlobal.SetValueShow(Model01ToUi(editModel.PreAdventVolume.Value));
        // });

        // if (SucceedVolumeGlobal) SucceedVolumeGlobal.SetAction(v =>
        // {
        //     if (editModel == null) return;
        //     DoQuietly(() => editModel.SucceedAudioVolume.Value = UiToModel01(v));
        //     SucceedVolumeGlobal.SetValueShow(Model01ToUi(editModel.SucceedAudioVolume.Value));
        // });

        // if (LoseVolumeGlobal) LoseVolumeGlobal.SetAction(v =>
        // {
        //     if (editModel == null) return;
        //     DoQuietly(() => editModel.LoseAudioVolume.Value = UiToModel01(v));
        //     LoseVolumeGlobal.SetValueShow(Model01ToUi(editModel.LoseAudioVolume.Value));
        // });

        // if (DefaultVolumeGlobal) DefaultVolumeGlobal.SetAction(v =>
        // {
        //     if (editModel == null) return;
        //     DoQuietly(() => editModel.DefaultAudioVolume.Value = UiToModel01(v));
        //     DefaultVolumeGlobal.SetValueShow(Model01ToUi(editModel.DefaultAudioVolume.Value));
        // });

        // if (TimeOfExistenceGlobal) TimeOfExistenceGlobal.SetAction(v =>
        // {
        //     if (editModel == null) return;
        //     float f = ParseToFloat(v);
        //     DoQuietly(() => editModel.TimeOfExistence.Value = f);
        //     TimeOfExistenceGlobal.SetValueShow(f.ToString());
        // });

        // if (TipAdvanceGlobal) TipAdvanceGlobal.SetAction(v =>
        // {
        //     if (editModel == null) return;
        //     float f = ParseToFloat(v);
        //     DoQuietly(() => editModel.TipOffset.Value = f);
        //     TipAdvanceGlobal.SetValueShow(f.ToString());
        // });
    }


    // ─────────────────────────────────────────────
    // “应用全局” —— 公开方法，绑定到按钮
    // 把关卡音乐以外的全局内容写回所有鼓点
    // ─────────────────────────────────────────────
    public void ApplyGlobalToAllDrums()
    {
        if (editModel == null) return;

        float gExist = editModel.TimeOfExistence.Value;
        float gAdvance = editModel.TipOffset.Value;
        float gTipVol = editModel.PreAdventVolume.Value;
        float gSucVol = editModel.SucceedAudioVolume.Value;
        float gLoseVol = editModel.LoseAudioVolume.Value;
        float gDefVol = editModel.DefaultAudioVolume.Value;

        ApplyAll(d =>
        {
            d.DrwmsData.VTimeOfExistence = gExist;
            d.DrwmsData.VPreAdventAudioClipOffsetTime = gAdvance;
            d.MusicData.SPreAdventVolume = gTipVol;
            d.MusicData.SSucceedVolume = gSucVol;
            d.MusicData.SLoseVolume = gLoseVol;
        });

        // 原来这里直接 SendEvent + RefreshAllEcho(); 我们改成静默广播，然后手动回显一次
        FireModelUiUpdated();
        RefreshAllEcho();
    }


    // ─────────────────────────────────────────────
    // 鼓点类型设置（改了就立刻批量作用到该类型鼓点）
    // —— 带 PrefabType 过滤规则（与 UIAttributeSetPanel 保持一致）
    // ─────────────────────────────────────────────
    // 绑定“类型设置块”的所有控件：写入现有鼓点 + 同步到模型的 TypeSettings（用于持久化与回显）
    void WireType(TypeBlock b)
    {
        if (b == null || editModel == null) return;

        // —— 时间类 ——
        if (b.TimeOfExistence) b.TimeOfExistence.SetAction(v =>
        {
            float f = ParseToFloat(v);
            DoQuietly(() =>
            {
                // 批量应用到现有同类型鼓点
                ApplyType(b.type, d =>
                {
                    if (!IsSkipExistence(d.DrwmsData.PrefabType))
                        d.DrwmsData.VTimeOfExistence = f;
                });
                // 同步类型默认
                editModel.EnsureTypeSetting(b.type).TimeOfExistence = f;
            });
            b.TimeOfExistence.SetValueShow(f.ToString());
        });

        if (b.TipAdvance) b.TipAdvance.SetAction(v =>
        {
            float f = ParseToFloat(v);
            DoQuietly(() =>
            {
                ApplyType(b.type, d =>
                {
                    if (!IsSkipTipOffset(d.DrwmsData.PrefabType))
                        d.DrwmsData.VPreAdventAudioClipOffsetTime = f;
                });
                editModel.EnsureTypeSetting(b.type).TipAdvance = f;
            });
            b.TipAdvance.SetValueShow(f.ToString());
        });

        if (b.TipPlayOffset) b.TipPlayOffset.SetAction(v =>
        {
            float f = ParseToFloat(v);
            DoQuietly(() =>
            {
                // 直接应用到现有同类型鼓点（你已把 VTipPlayOffset 改为可序列化字段）
                ApplyType(b.type, d => d.DrwmsData.VTipPlayOffset = f);
                // 同步类型默认
                editModel.EnsureTypeSetting(b.type).TipPlayOffset = f;
            });
            b.TipPlayOffset.SetValueShow(f.ToString());
            // 若需要让预览立即响应播放位置，可在此额外通知其它系统
            // FireModelUiUpdated();  // 如不需要可注释
        });

        // —— 音频（路径）——
        if (b.TipAudio) b.TipAudio.SetAction(v =>
        {
            var clip = (AudioClip)v; string name = clip ? clip.name : null;
            DoQuietly(() =>
            {
                ApplyType(b.type, d => d.DrwmsData.FPreAdventAudioClipPath = name);
                editModel.EnsureTypeSetting(b.type).TipAudio = name;
                this.SendCommand(new SetAudioEditAudioComeTipsCommand(b.type, clip));
            });
            b.TipAudio.SetShowFileName(name);
        });

        if (b.SucceedAudio) b.SucceedAudio.SetAction(v =>
        {
            var clip = (AudioClip)v; string name = clip ? clip.name : null;
            DoQuietly(() =>
            {
                ApplyType(b.type, d => d.DrwmsData.FSucceedAudioClipPath = name);
                editModel.EnsureTypeSetting(b.type).SucceedAudio = name;
                this.SendCommand(new SetAudioEditSucceedAudioCommand(b.type, clip));
            });
            b.SucceedAudio.SetShowFileName(name);
        });

        if (b.LoseAudio) b.LoseAudio.SetAction(v =>
        {
            var clip = (AudioClip)v; string name = clip ? clip.name : null;
            DoQuietly(() =>
            {
                ApplyType(b.type, d => d.DrwmsData.FLoseAudioClipPath = name);
                editModel.EnsureTypeSetting(b.type).LoseAudio = name;
                // 全局失败音频的命令（若你的项目需要全局兜底）
                this.SendCommand(new SetAudioEditAudioLoseAudioCommand(clip));
            });
            b.LoseAudio.SetShowFileName(name);
        });

        if (b.DefaultAudio) b.DefaultAudio.SetAction(v =>
        {
            var clip = (AudioClip)v; string name = clip ? clip.name : null;
            DoQuietly(() =>
            {
                // 类型默认音频：既写入类型默认，又可选地批量写回现有鼓点
                editModel.EnsureTypeSetting(b.type).DefaultAudio = name;
                ApplyType(b.type, d => d.DrwmsData.FDefaultAudioClipPath = name);
            });
            b.DefaultAudio.SetShowFileName(name);
        });

        // —— 音量（UI:0..200 ↔ 模型:0..1）——
        if (b.TipVolume) b.TipVolume.SetAction(v =>
        {
            float f = UiToModel01(v);
            DoQuietly(() =>
            {
                ApplyType(b.type, d =>
                {
                    if (!IsSkipTipVolume(d.DrwmsData.PrefabType))
                        d.MusicData.SPreAdventVolume = f;
                });
                editModel.EnsureTypeSetting(b.type).TipVolume = f;
            });
            b.TipVolume.SetValueShow(Model01ToUi(f));
        });

        if (b.SucceedVolume) b.SucceedVolume.SetAction(v =>
        {
            float f = UiToModel01(v);
            DoQuietly(() =>
            {
                ApplyType(b.type, d =>
                {
                    if (!IsSkipSuccessVolume(d.DrwmsData.PrefabType))
                        d.MusicData.SSucceedVolume = f;
                });
                editModel.EnsureTypeSetting(b.type).SucceedVolume = f;
            });
            b.SucceedVolume.SetValueShow(Model01ToUi(f));
        });

        if (b.LoseVolume) b.LoseVolume.SetAction(v =>
        {
            float f = UiToModel01(v);
            DoQuietly(() =>
            {
                ApplyType(b.type, d => d.MusicData.SLoseVolume = f);
                editModel.EnsureTypeSetting(b.type).LoseVolume = f;
            });
            b.LoseVolume.SetValueShow(Model01ToUi(f));
        });

        if (b.DefaultVolume) b.DefaultVolume.SetAction(v =>
        {
            float f = UiToModel01(v);
            DoQuietly(() =>
            {
                ApplyType(b.type, d => d.MusicData.SDefaultVolume = f);
                editModel.EnsureTypeSetting(b.type).DefaultVolume = f;
            });
            b.DefaultVolume.SetValueShow(Model01ToUi(f));
        });
    }


    // ─────────────────────────────────────────────
    // 回显：全局 + 类型（从鼓点抽样/默认）
    // ─────────────────────────────────────────────
    void RefreshAllEcho()
    {
        if (editModel == null) return;

        // 关卡音乐
        if (MainAudio) MainAudio.SetShowFileName(editModel.EditAudioClip ? editModel.EditAudioClip.name : null);
        if (MainAudioVolume) MainAudioVolume.SetValueShow(Model01ToUi(editModel.EditAudioClipVolume.Value));

        // // 全局设置
        // if (TipVolumeGlobal) TipVolumeGlobal.SetValueShow(Model01ToUi(editModel.PreAdventVolume.Value));
        // if (SucceedVolumeGlobal) SucceedVolumeGlobal.SetValueShow(Model01ToUi(editModel.SucceedAudioVolume.Value));
        // if (LoseVolumeGlobal) LoseVolumeGlobal.SetValueShow(Model01ToUi(editModel.LoseAudioVolume.Value));
        // if (DefaultVolumeGlobal) DefaultVolumeGlobal.SetValueShow(Model01ToUi(editModel.DefaultAudioVolume.Value));
        // if (TimeOfExistenceGlobal) TimeOfExistenceGlobal.SetValueShow(editModel.TimeOfExistence.Value.ToString("0.##"));
        // if (TipAdvanceGlobal) TipAdvanceGlobal.SetValueShow(editModel.TipOffset.Value.ToString("0.##"));

        // 类型块
        EchoType(ClickBlock);
        EchoType(SwipeUpBlock);
        EchoType(SwipeDownBlock);
        EchoType(SwipeLeftBlock);
        EchoType(SwipeRightBlock);
    }

    // 按优先级回显“类型设置”到 UI：TypeSettings（持久化）→ 时间轴首个该类型鼓点 → 全局兜底
    void EchoType(TypeBlock b)
    {
        if (b == null || editModel == null) return;

        // 1) 先读“类型默认集”
        editModel.TypeSettings.TryGetValue(b.type, out var ts);

        string tipName = ts?.TipAudio;
        string succName = ts?.SucceedAudio;
        string loseName = ts?.LoseAudio;
        string defName = ts?.DefaultAudio;

        float exist = ts?.TimeOfExistence ?? float.NaN; // 判定时长
        float tipAdv = ts?.TipAdvance ?? float.NaN; // 提前量（数据偏移）
        float tipPlayOff = ts?.TipPlayOffset ?? float.NaN; // 播放偏移

        float tipVol = ts?.TipVolume ?? float.NaN;
        float succVol = ts?.SucceedVolume ?? float.NaN;
        float loseVol = ts?.LoseVolume ?? float.NaN;
        float defVol = ts?.DefaultVolume ?? float.NaN;

        // 2) 若类型默认缺失，再从时间轴“首个该类型鼓点”补齐
        if (TryFindFirstDrumOfType(b.type, out var d))
        {
            tipName ??= d.DrwmsData.FPreAdventAudioClipPath;
            succName ??= d.DrwmsData.FSucceedAudioClipPath;
            loseName ??= d.DrwmsData.FLoseAudioClipPath;
            defName ??= d.DrwmsData.FDefaultAudioClipPath;

            if (float.IsNaN(exist)) exist = d.DrwmsData.VTimeOfExistence;
            if (float.IsNaN(tipAdv)) tipAdv = d.DrwmsData.VPreAdventAudioClipOffsetTime;
            if (float.IsNaN(tipPlayOff)) tipPlayOff = d.DrwmsData.VTipPlayOffset;

            if (float.IsNaN(tipVol)) tipVol = d.MusicData.SPreAdventVolume;
            if (float.IsNaN(succVol)) succVol = d.MusicData.SSucceedVolume;
            if (float.IsNaN(loseVol)) loseVol = d.MusicData.SLoseVolume;
            if (float.IsNaN(defVol)) defVol = d.MusicData.SDefaultVolume;
        }

        // 3) 仍缺则最后兜底“全局默认”
        // —— 音频兜底（按类型取各自的 Tips/Succeed，Lose/Default 使用全局）
        switch (b.type)
        {
            case TheTypeOfOperation.SwipeUp:
                tipName ??= editModel.UpTipsAudioClip ? editModel.UpTipsAudioClip.name : null;
                succName ??= editModel.UpSucceedAudioClip ? editModel.UpSucceedAudioClip.name : null;
                break;
            case TheTypeOfOperation.SwipeDown:
                tipName ??= editModel.DownTipsAudioClip ? editModel.DownTipsAudioClip.name : null;
                succName ??= editModel.DownSucceedAudioClip ? editModel.DownSucceedAudioClip.name : null;
                break;
            case TheTypeOfOperation.SwipeLeft:
                tipName ??= editModel.LeftTipsAudioClip ? editModel.LeftTipsAudioClip.name : null;
                succName ??= editModel.LeftSucceedAudioClip ? editModel.LeftSucceedAudioClip.name : null;
                break;
            case TheTypeOfOperation.SwipeRight:
                tipName ??= editModel.RightTipsAudioClip ? editModel.RightTipsAudioClip.name : null;
                succName ??= editModel.RightSucceedAudioClip ? editModel.RightSucceedAudioClip.name : null;
                break;
            case TheTypeOfOperation.Click:
            default:
                tipName ??= editModel.ClickTipsAudioClip ? editModel.ClickTipsAudioClip.name : null;
                succName ??= editModel.ClickSucceedAudioClip ? editModel.ClickSucceedAudioClip.name : null;
                break;
        }
        loseName ??= editModel.LoseAudioClip ? editModel.LoseAudioClip.name : null;
        defName ??= editModel.DefaultAudioClip ? editModel.DefaultAudioClip.name : null;

        // —— 数值兜底（全局）
        if (float.IsNaN(exist)) exist = editModel.TimeOfExistence.Value;
        if (float.IsNaN(tipAdv)) tipAdv = editModel.TipOffset.Value;
        if (float.IsNaN(tipPlayOff)) tipPlayOff = 0f; // 全局无统一“播放偏移”则置 0

        // —— 音量兜底（全局）
        if (float.IsNaN(tipVol)) tipVol = editModel.PreAdventVolume.Value;
        if (float.IsNaN(succVol)) succVol = editModel.SucceedAudioVolume.Value;
        if (float.IsNaN(loseVol)) loseVol = editModel.LoseAudioVolume.Value;
        if (float.IsNaN(defVol)) defVol = editModel.DefaultAudioVolume.Value;

        // 4) 回显到 UI
        if (b.TipAudio) b.TipAudio.SetShowFileName(tipName);
        if (b.SucceedAudio) b.SucceedAudio.SetShowFileName(succName);
        if (b.LoseAudio) b.LoseAudio.SetShowFileName(loseName);
        if (b.DefaultAudio) b.DefaultAudio.SetShowFileName(defName);

        if (b.TimeOfExistence) b.TimeOfExistence.SetValueShow(exist.ToString("0.##"));
        if (b.TipAdvance) b.TipAdvance.SetValueShow(tipAdv.ToString("0.##"));
        if (b.TipPlayOffset) b.TipPlayOffset.SetValueShow(tipPlayOff.ToString("0.##"));

        if (b.TipVolume) b.TipVolume.SetValueShow(Model01ToUi(tipVol));
        if (b.SucceedVolume) b.SucceedVolume.SetValueShow(Model01ToUi(succVol));
        if (b.LoseVolume) b.LoseVolume.SetValueShow(Model01ToUi(loseVol));
        if (b.DefaultVolume) b.DefaultVolume.SetValueShow(Model01ToUi(defVol));
    }



    // ─────────────────────────────────────────────
    // 批量应用 / 遍历
    // ─────────────────────────────────────────────
    void ApplyAll(System.Action<DrumsLoadData> action)
    {
        if (editModel == null) return;
        foreach (var kv in editModel.TimeLineData)
            foreach (var d in kv.Value)
                action(d);

        FireModelUiUpdated(); // 替代 this.SendEvent<OnUpdateAudioEditDrumsUI>();
    }

    void ApplyType(TheTypeOfOperation type, System.Action<DrumsLoadData> action)
    {
        if (editModel == null) return;
        foreach (var kv in editModel.TimeLineData)
            foreach (var d in kv.Value)
                if (d.DrwmsData.DtheTypeOfOperation == type)
                    action(d);

        FireModelUiUpdated(); // 替代 this.SendEvent<OnUpdateAudioEditDrumsUI>();
    }


    bool TryFindFirstDrumOfType(TheTypeOfOperation type, out DrumsLoadData found)
    {
        if (editModel != null)
        {
            foreach (var kv in editModel.TimeLineData)
            {
                foreach (var d in kv.Value)
                {
                    if (d.DrwmsData.DtheTypeOfOperation == type)
                    {
                        found = d;
                        return true;
                    }
                }
            }
        }
        found = null;
        return false;
    }

    // ─────────────────────────────────────────────
    // PrefabType 过滤（与 UIAttributeSetPanel 保持一致）
    // a: 提示音独立 → 不同步 成功音量、存在时间
    // b: 回答音独立 → 不同步 提示音量/偏移、存在时间
    // d: 判定区间独立 → 不同步 提示音量/偏移
    // ─────────────────────────────────────────────
    private static bool IsSkipSuccessVolume(int prefabType)
        => prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.TipOnly;

    private static bool IsSkipTipVolume(int prefabType)
        => prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.AnswerOnly
        || prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.JudgeOnly;

    private static bool IsSkipTipOffset(int prefabType)
        => prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.AnswerOnly
        || prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.JudgeOnly;

    private static bool IsSkipExistence(int prefabType)
        => prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.TipOnly
        || prefabType == (int)UIAudioEditDrumsOrbit.PrefabDrumType.AnswerOnly;

    // ─────────────────────────────────────────────
    // 小工具
    // ─────────────────────────────────────────────
    float ParseToFloat(object v)
    {
        if (v == null) return 0f;
        var s = v.ToString();
        return float.TryParse(s, out var f) ? f : 0f;
    }

    // // UI(0..200) -> 模型(0..1)
    // float UiToModel01(object v)
    // {
    //     float f = ParseToFloat(v);
    //     f = f / 200f;
    //     return Clamp01(f);
    // }

    // // 模型(0..1) -> UI(0..200)
    // float Model01ToUi(float m)
    // {
    //     return Clamp01(m) * 200f;
    // }

    // UI(0..1) -> 模型(0..1) 直接夹紧
    float UiToModel01(object v)
    {
        float f = ParseToFloat(v);
        return Clamp01(f);
    }

    // 模型(0..1) -> UI(0..1) 直接夹紧
    float Model01ToUi(float m)
    {
        return Clamp01(m);
    }

    float Clamp01(float x) => x < 0 ? 0 : (x > 1 ? 1 : x);
}
