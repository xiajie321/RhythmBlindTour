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
        Debug.Log("��ʼ��������...");
        Models();
        Systems();
    }
    void Models()
    {
        this.RegisterModel(new AudioEditModel());
        Debug.Log("Model�������");
    }
    void Systems()
    {
        RegisterSystem(new InputSystems());
        Debug.Log("System�������");
    }
}
