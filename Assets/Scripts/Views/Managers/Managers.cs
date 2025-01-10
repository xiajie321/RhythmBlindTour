using Qf.Commands.AudioEdit;
using QFramework;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Qf.Managers {
    public class Managers : MonoSingleton<Managers>, IController
    {
        [SerializeField]
        List<ManagerBase> ManagerOrderOfExecution = new();//¹ÜÀíÆ÷Ö´ÐÐË³Ðò
        //[SerializeField]
        //AudioClip clip;
        Action _UpDate;
        Action _FixedUpDate;
        Action _LateUpDate;

        protected override void OnAwake()
        {
            GetArchitecture();

            foreach (var manager in ManagerOrderOfExecution)
            {
                manager.Init();
            }
            //this.SendCommand(new SetAudioEditAudioCommand(clip));
        }
        void Update() => _UpDate?.Invoke();

        void FixedUpdate() => _FixedUpDate?.Invoke();

        void LateUpdate() => _LateUpDate?.Invoke();

        public void AddUpdate(Action action) => _UpDate += action;
        public void AddFixedUpDate(Action action) => _FixedUpDate += action;
        public void AddLateUpDate(Action action) => _LateUpDate += action;

        public void RemoveManager(ManagerBase managerBase) => ManagerOrderOfExecution.Remove(managerBase);
        public void AddManager(ManagerBase managerBase) => ManagerOrderOfExecution.Add(managerBase);

        public IArchitecture GetArchitecture()
        {
            return GameBody.Interface;
        }
    }
}
