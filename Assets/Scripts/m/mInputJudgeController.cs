using System.Linq;
using UnityEngine;
using Qf.Systems;
using Qf.Models.AudioEdit;
using Qf.ClassDatas.AudioEdit;

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

        if (!inputConsumedThisFrame && InputSystems.PlayClick)
        {
            HandleFirstLose(); // 输入无响应时自动处理失败鼓点
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

        // 1. 正确优先：类型匹配的鼓点（先尝试匹配）
        var matching = activeModes
            .Where(x => x.GetOperation() == inputType)
            .OrderBy(x => x.StartTime)
            .ToList();

        foreach (var mode in matching)
        {
            if (mode.ReceiveInput(inputType))
            {
                mode.IsActive = false;
                inputConsumedThisFrame = true;
                return;
            }

            if (mode.HasJudged)
            {
                mode.IsActive = false;
                inputConsumedThisFrame = true;
                return;
            }
        }

        // 2. 错误输入：只处理 StartTime 最早的鼓点，但仅当 now >= StartTime
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

}
