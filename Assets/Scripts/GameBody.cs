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
        //������������Լ�ϵͳ����ע��
        Debug.Log("[GameBody] ��ʼ��������...");
        Models();
        Systems();
        Utilitys();
    }

    private void Utilitys()
    {
        RegisterUtility(new Storage());
        Debug.Log("[GameBody] Utility�������");
    }

    void Models()
    {
        RegisterModel(new DataCachingModel());
        RegisterModel(new AudioEditModel());
        Debug.Log("[GameBody] Model�������");
    }
    void Systems()
    {
        RegisterSystem(new InputSystems());
        Debug.Log("[GameBody] System�������");
    }
}
