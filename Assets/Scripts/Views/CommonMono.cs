using QFramework;
using System;
using UnityEngine;
using System.Collections;

public class CommonMono : MonoSingleton<CommonMono>, IController
{

    /// <summary>
    /// �ڷ�mono�ű��п���Э�̣�����Э�̵Ĺر�������Ҫ�������
    /// </summary>
    /// <param name="ienum"></param>
    /// <returns></returns>
    public new Coroutine StartCoroutine(IEnumerator ienum)
    {
        return StartCoroutine(ienum);
    }

    public new void StopCoroutine(Coroutine cor)
    {
        StopCoroutine(cor);
    }

    public new void StopAllCoroutines()
    {
        StopAllCoroutines();
    }

    protected override void OnAwake()
    {
        this.GetArchitecture();
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
