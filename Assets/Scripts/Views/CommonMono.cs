using QFramework;
using System;
using UnityEngine;
using System.Collections;

public class CommonMono : MonoSingleton<CommonMono>, IController
{

    /// <summary>
    /// 在非mono脚本中开启协程，所以协程的关闭条件需要额外控制
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
