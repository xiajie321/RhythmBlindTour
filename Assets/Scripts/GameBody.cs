using Qf.Models;
using Qf.Models.AudioEdit;
using Qf.Systems;
using QFramework;
using UnityEngine;
public class GameBody : Architecture<GameBody>
{
    protected override void Init()
    {
        //������������Լ�ϵͳ����ע��
        Debug.Log("[GameBody] ��ʼ��������...");
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
        Debug.Log("[GameBody] Model�������");
    }
    void Systems()
    {
        RegisterSystem(new InputSystems());
        Debug.Log("[GameBody] System�������");
    }
}
