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
        Debug.Log("[GameBody] 初始化加载中...");
        Utilities();
        Models();
        Systems();
    }

    void Utilities()
    {
        this.RegisterUtility(new FileLoader());
    }
    void Models()
    {
        this.RegisterModel(new AudioEditModel());
        Debug.Log("[GameBody] Model加载完毕");
    }
    void Systems()
    {
        RegisterSystem(new InputSystems());
        Debug.Log("[GameBody] System加载完毕");
    }
}
