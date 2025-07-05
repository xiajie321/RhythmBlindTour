using Qf.Models;
using Qf.Models.AudioEdit;
using Qf.Systems;
using QFramework;
using System;
using UnityEngine;
public class GameBody : Architecture<GameBody>
{
    protected override void Init()
    {
        //在这里对数据以及系统进行注册
        Debug.Log("[GameBody] 初始化加载中...");
        Models();
        Systems();
        Utilitys();
    }

    private void Utilitys()
    {
        RegisterUtility(new Storage());
        Debug.Log("[GameBody] Utility加载完毕");
    }

    void Models()
    {
        RegisterModel(new DataCachingModel());
        RegisterModel(new AudioEditModel());
        Debug.Log("[GameBody] Model加载完毕");
    }
    void Systems()
    {
        RegisterSystem(new InputSystems());
        Debug.Log("[GameBody] System加载完毕");
    }
}
