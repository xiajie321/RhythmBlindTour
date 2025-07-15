using System.Linq;
using UnityEngine;
using Qf.Systems;
using Qf.Models.AudioEdit;
using Qf.ClassDatas.AudioEdit;
using Qf.Managers;

public class mInputJudgeController : MonoBehaviour
{
    private CreateDrumsManager drumsManager;
    private AudioEditModel editModel;
    private bool inputConsumedThisFrame = false;


    private void Start()
    {
        drumsManager = FindObjectOfType<CreateDrumsManager>();
        editModel = GameBody.Interface.GetModel<AudioEditModel>();
    }

    private void Update()
    {
        if (drumsManager == null || editModel == null || !editModel.Mode.Equals(SystemModeData.PlayMode))
            return;

        inputConsumedThisFrame = false;

        TryHandle(TheTypeOfOperation.Click, InputSystems.Click);
        TryHandle(TheTypeOfOperation.SwipeUp, InputSystems.SwipeUp);
        TryHandle(TheTypeOfOperation.SwipeDown, InputSystems.SwipeDown);
        TryHandle(TheTypeOfOperation.SwipeLeft, InputSystems.SwipeLeft);
        TryHandle(TheTypeOfOperation.SwipeRight, InputSystems.SwipeRight);

        // 区间外输入播放默认音效
        if (!inputConsumedThisFrame)
        {
            if (InputSystems.Click || InputSystems.SwipeUp || InputSystems.SwipeDown || InputSystems.SwipeLeft || InputSystems.SwipeRight)
            {
                PlayDefaultAudio();
                inputConsumedThisFrame = true;
            }
        }
    }

    public void PauseAllInputModeAutoFail(bool pause)
    {
        if (drumsManager == null || drumsManager.ActiveInputModes == null)
            return;

        foreach (var mode in drumsManager.ActiveInputModes)
        {
            if (mode != null)
                mode.PauseAutoFail = pause;
        }

        Debug.Log($"[mInputJudgeController] 所有 InputMode 自动失败判断已{(pause ? "暂停" : "恢复")}");
    }

    void TryHandle(TheTypeOfOperation inputType, bool inputTriggered)
    {
        if (!inputTriggered || inputConsumedThisFrame)
            return;

        var activeModes = drumsManager.ActiveInputModes
            .Where(x => x != null && x.IsActive)
            .ToList();

        float now = editModel.ThisTime;

        // 1. 正确优先：先按时间，再按 DrumCode 排序
        var matching = activeModes
            .Where(x => x.GetOperation() == inputType)
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.DrwmsData.DrwmsData.DrumCode)
            .ToList();

        foreach (var mode in matching)
        {
            if (mode.ReceiveInput(inputType))
            {
                mode.IsActive = false;
                inputConsumedThisFrame = true;
                return;
            }
            // 不再因为 HasJudged == true 就直接消耗，继续尝试下一个
        }

        // 2. 错误输入：保持不变
        var earliest = activeModes
            .OrderBy(x => x.StartTime)
            .FirstOrDefault();
        if (earliest != null && now >= earliest.StartTime)
        {
            bool result = earliest.ReceiveInput(inputType);
            if (result || earliest.HasJudged)
            {
                earliest.IsActive = false;
                inputConsumedThisFrame = true;
            }
        }
    }

    void HandleFirstLose()
    {
        var fallback = drumsManager.ActiveInputModes
            .Where(x => x != null && x.IsActive)
            .OrderBy(x => x.StartTime)
            .ToList();

        float now = editModel.ThisTime;

        foreach (var mode in fallback)
        {
            if (now < mode.StartTime)
                continue; // 尚未进入判定时间，不能处理

            if (now > mode.EndTime)
                continue; // 超时由 InputMode 自动处理

            // 已进入 StartTime 区间，但未被触发 → 判定为失败
            mode.LoseByManager();
            mode.IsActive = false;
            inputConsumedThisFrame = true;
            break;
        }
    }

    void PlayDefaultAudio()
    {
        if (editModel != null && editModel.DefaultAudioClip != null)
        {
            float volume = editModel.DefaultAudioVolume != null ? editModel.DefaultAudioVolume.Value : 1f;
            AudioSource vfxSource = AudioEditManager.Instance.GetVFXSource((TheTypeOfOperation)5); // 轨道编号或按需要
            vfxSource.PlayOneShot(editModel.DefaultAudioClip, volume);

        }
    }

}
