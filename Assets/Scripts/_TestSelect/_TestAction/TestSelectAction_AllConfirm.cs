using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace TestRevolver
{
    /// <summary>
    /// 配置多组 Confirm Events：
    /// 每个 Stage 对应一组 Bullet Confirm 回调，
    /// 并根据本脚本中命名为 _Action_{stage}_{index} 的私有方法自动构建 eventGroups。
    /// </summary>
    [ExecuteInEditMode]
    public class TestSelectAction_AllConfirm : MonoBehaviour
    {
        /// <summary>
        /// 每个 Stage 下的一组 Confirm 事件
        /// </summary>
        [Serializable]
        public class ConfirmEventsGroup
        {
            [Tooltip("对应 TestSelectAction_StageManager.StageMode 枚举")]
            public TestSelectAction_StageManager.StageMode stageMode;
            [Tooltip("该模式下每个 Bullet 的 Confirm 回调（按索引顺序）")]
            public List<UnityEvent> confirmEvents = new List<UnityEvent>();
        }

        [Header("自动生成：每个 Stage 的 Confirm 事件组")]
        [Tooltip("本脚本会在编辑器/运行时扫描所有名为 _Action_{stage}_{index} 的方法来填充")]
        public List<ConfirmEventsGroup> eventGroups = new List<ConfirmEventsGroup>();

        private void Awake()
        {
            // 运行时也要生成一次
            PopulateEventGroups();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器模式下，Inspector 修改或脚本热重载后自动生成
            PopulateEventGroups();
        }
#endif

        /// <summary>
        /// 扫描本类型中所有名为 _Action_{stage}_{index} 的方法，
        /// 按 stage 分组并按 index 排序，将它们封装为 UnityEvent 填入 eventGroups。
        /// </summary>
        public void PopulateEventGroups()
        {
            // 找到所有符合命名规范的方法
            var methods = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(m => m.Name.StartsWith("_Action_"))
                .ToList();

            // 临时 map：StageMode -> List<(index, MethodInfo)>
            var map = new Dictionary<TestSelectAction_StageManager.StageMode, List<(int idx, MethodInfo mi)>>();

            foreach (var mi in methods)
            {
                var parts = mi.Name.Split('_');
                // 期望格式：_Action_{stageIndex}_{bulletIndex}
                if (parts.Length == 4
                    && int.TryParse(parts[2], out int stageIdx)
                    && int.TryParse(parts[3], out int bulletIdx)
                    && Enum.IsDefined(typeof(TestSelectAction_StageManager.StageMode), stageIdx))
                {
                    var mode = (TestSelectAction_StageManager.StageMode)stageIdx;
                    if (!map.TryGetValue(mode, out var list))
                    {
                        list = new List<(int, MethodInfo)>();
                        map[mode] = list;
                    }
                    list.Add((bulletIdx, mi));
                }
            }

            // 清理或更新 eventGroups，使之与 map 保持同步
            // 1. 移除那些不在 map 中的组
            eventGroups.RemoveAll(g => !map.ContainsKey(g.stageMode));

            // 2. 对于 map 中每个组，创建或更新
            foreach (var kv in map)
            {
                var mode = kv.Key;
                var list = kv.Value.OrderBy(e => e.idx).ToList();

                // 找到或创建对应的 ConfirmEventsGroup
                var group = eventGroups.Find(g => g.stageMode == mode);
                if (group == null)
                {
                    group = new ConfirmEventsGroup { stageMode = mode };
                    eventGroups.Add(group);
                }

                // 重建 confirmEvents 列表
                group.confirmEvents = new List<UnityEvent>();
                foreach (var (idx, method) in list)
                {
                    var ue = new UnityEvent();
                    // 捕获当前 method 引用
                    ue.AddListener(() => method.Invoke(this, null));
                    group.confirmEvents.Add(ue);
                }
            }
        }


        #region — UI-Stage0 —
        /// <summary>
        /// Mode-Stage切换指引：
        ///     Mode0 - 进入游戏Stage0
        ///     Mode1 - 开始界面Stage1
        ///     Mode2 - 退出游戏Stage2
        /// </summary>
        private void _Action_0_0()
        {
            //快查：切换到主界面Stage1
            this.GetComponent<TestSelectAction_StageManager>()._SA_SwitchToMode(1);
        }
        #endregion

        #region — UI-Stage1 —
        /// <summary>
        /// Mode-Stage切换指引：
        ///     Mode0 - 进入游戏Stage0
        ///     Mode1 - 开始界面Stage1
        ///     Mode2 - 退出游戏Stage2
        /// </summary>
        private void _Action_1_0()
        {
            //TODO 快查：切换游戏场景
            this.GetComponent<TestSelectAction_StageManager>()._SA_SwitchToMode(0);        
        }
        private void _Action_1_1()
        {
            Debug.Log("Stage1: Action 1 执行");
        }
        private void _Action_1_2()
        {
            //快查：切换到退出游戏确认Stage2
            this.GetComponent<TestSelectAction_StageManager>()._SA_SwitchToMode(2);
        }
        private void _Action_1_3()
        {
            Debug.Log("Stage1: Action 3 执行");
        }
        private void _Action_1_4()
        {
            Debug.Log("Stage1: Action 4 执行");
        }
        #endregion

        #region — UI-Stage2 —
        /// <summary>
        /// Mode-Stage切换指引：
        ///     Mode0 - 进入游戏Stage0
        ///     Mode1 - 开始界面Stage1
        ///     Mode2 - 退出游戏Stage2
        /// </summary>
        private void _Action_2_0()
        {
            //快查：切换到主界面Stage1
            this.GetComponent<TestSelectAction_StageManager>()._SA_SwitchToMode(1);
        }
        private void _Action_2_1()
        {
            // 快查：退出游戏/运行模式
        #if UNITY_EDITOR
            // 编辑器下停止播放
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 真机/打包后退出应用
            Application.Quit();
        #endif
        }

            #endregion
            // 以后再添加 Stage2、Stage3... 的 Action 方法时，只需按命名规范添加即可，
            // 不需手动修改 confirmEvents 或 eventGroups。
        }
}
