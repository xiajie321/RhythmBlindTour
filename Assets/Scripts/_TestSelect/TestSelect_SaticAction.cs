using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

namespace TestRevolver
{
    /// <summary>
    /// 静态工具方法类，用于封装选择系统的通用逻辑，
    /// 以及存储系统流程所需的静态变量。
    /// </summary>
    public static class TestSelect_SaticAction
    {
        public static class StaticData
        {
            // —— 新增：当前活跃的 Revolver 实例
            public static TestSelect_Revolver s_currentRevolver = null;

            public static int s_iCurrentUIPanelNumber = 0;
            public static int s_iCurrentSelectedOption = 0;
            public static string s_strLayer = "";
            public static int s_iGameLevelNumber = 0;
            public static string s_strUniqueLevelID
            {
                get { return s_strLayer + "_" + s_iGameLevelNumber.ToString("D2"); }
            }
            public static void SetLayer(string newLayer) { s_strLayer = newLayer; }
            public static void SetGameLevelNumber(int newGameLevelNumber) { s_iGameLevelNumber = newGameLevelNumber; }
            public static int GetNumber_SelectedOption() { return s_iCurrentSelectedOption; }

            public static float s_minWaitTime = 0f;
            public static UnityEvent s_preSceneLoading;
            public static UnityEvent<float> s_duringSceneLoading;
            public static UnityEvent s_postSceneLoading;
            public static void SetSceneSwitchSettings(float minWaitTime, UnityEvent preSceneLoading, UnityEvent<float> duringSceneLoading, UnityEvent postSceneLoading)
            {
                s_minWaitTime = minWaitTime;
                s_preSceneLoading = preSceneLoading;
                s_duringSceneLoading = duringSceneLoading;
                s_postSceneLoading = postSceneLoading;
            }

            public static string[] s_sceneNameEnum = new string[0];
            public static void SetSceneNameEnum(string[] sceneNames) { s_sceneNameEnum = sceneNames; }
            public static string[] GetSceneNameEnum() { return s_sceneNameEnum; }

            // 【新增】用于覆盖场景切换设置的静态变量：
            public static bool s_bOverrideSceneSwitch = false;
            public static string s_overrideSceneName = "";
        }

        public static int CalculateNewIndex(bool isRight, int currentIndex, int length)
        {
            return isRight ? (currentIndex + 1) % length : (currentIndex - 1 + length) % length;
        }

        public static void ConfirmAction(UnityEvent commonConfirm)
        {
            commonConfirm?.Invoke();
        }

        public static void CancelAction(UnityEvent commonCancel)
        {
            commonCancel?.Invoke();
        }

        public static IEnumerator AsyncLoadSceneByName(string sceneName, float minWaitTime = 0f, UnityEvent preSceneLoading = null, UnityEvent<float> duringSceneLoading = null, UnityEvent postSceneLoading = null)
        {
            preSceneLoading?.Invoke();
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;
            float elapsedTime = 0f;
            while (!asyncOp.isDone)
            {
                if (asyncOp.progress >= 0.9f && elapsedTime >= minWaitTime)
                    break;
                duringSceneLoading?.Invoke(elapsedTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            postSceneLoading?.Invoke();
            asyncOp.allowSceneActivation = true;
        }

        public static IEnumerator AsyncLoadSceneByIndex(int sceneIndex, float minWaitTime = 0f, UnityEvent preSceneLoading = null, UnityEvent<float> duringSceneLoading = null, UnityEvent postSceneLoading = null)
        {
            preSceneLoading?.Invoke();
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneIndex);
            asyncOp.allowSceneActivation = false;
            float elapsedTime = 0f;
            while (!asyncOp.isDone)
            {
                if (asyncOp.progress >= 0.9f && elapsedTime >= minWaitTime)
                    break;
                duringSceneLoading?.Invoke(elapsedTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            postSceneLoading?.Invoke();
            asyncOp.allowSceneActivation = true;
        }

        public static class InputEvents
        {
            public static UnityEvent s_evNextOption = new UnityEvent();
            public static UnityEvent s_evPrevOption = new UnityEvent();
            public static UnityEvent s_evConfirm = new UnityEvent();
            public static UnityEvent s_evBack = new UnityEvent();
            public static UnityEvent s_evExit = new UnityEvent();
        }
    }
}
