using Qf.Models;
using Qf.Models.AudioEdit;
using Qf.Systems;
using QFramework;
using UnityEngine;
public class GameBody : Architecture<GameBody>
{
    protected override void Init()
    {
        //在这里对数据以及系统进行注册
        Debug.Log("初始化加载中...");
        Models();
        Systems();
    }
    void Models()
    {
        this.RegisterModel(new AudioEditModel());
        Debug.Log("Model加载完毕");
    }
    void Systems()
    {
        RegisterSystem(new InputSystems());
        Debug.Log("System加载完毕");
    }
}
