using System.Collections.Generic;
using Qf.Models;
using QFramework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Views.UIManager
{
    public class UIArchitecture : Architecture<UIArchitecture>
    {
        protected override void Init()
        {
            this.RegisterModel(new GameSettingModel());
        }
    }

    public class UIManager
    {
        public static UIManager Instance { get; } = new UIManager();

        private Transform canvasTrans;

        //显示时存入，隐去时删除
        public Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();

        private UIManager()
        {
            GameObject canvas = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Canvas"));
            canvasTrans = canvas.transform;
            GameObject.DontDestroyOnLoad(canvasTrans);
        }

        public T ShowPanel<T>() where T : BasePanel
        {
            string name = typeof(T).Name;
            if (panelDic.ContainsKey(name))
            {
                return panelDic[name] as T;
            }

            GameObject panelObj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/UIPanel/" + name),
                canvasTrans, false);

            T panel = panelObj.GetComponent<T>();
            panelDic.Add(name, panel);
            panel.ShowMe();

            return panel;
        }

        public void HidePanel<T>(bool isFade = false) where T : BasePanel
        {
            string name = typeof(T).Name;
            if (!panelDic.TryGetValue(name, out BasePanel panel)) return;

            if (isFade)
            {
                panel.HideMe(() =>
                {
                    GameObject.Destroy(panel.gameObject);
                    panelDic.Remove(name);
                });
            }
            else
            {
                GameObject.Destroy(panel.gameObject);
                panelDic.Remove(name);
            }
        }

        public T GetPanel<T>() where T : BasePanel
        {
            string name = typeof(T).Name;

            if (panelDic.TryGetValue(name, out var panel))
            {
                return panel as T;
            }

            return null;
        }
    }

    public static class EventTriggerExtensions
    {
        public static void AddListener(this EventTrigger trigger, UnityAction<BaseEventData> callback,
            EventTriggerType type = EventTriggerType.PointerClick)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = type
            };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        public static void AddListener(this EventTrigger trigger, UnityAction callback,
            EventTriggerType type = EventTriggerType.PointerClick)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = type
            };

            entry.callback.AddListener((data) => callback());
            trigger.triggers.Add(entry);
        }
    }
}