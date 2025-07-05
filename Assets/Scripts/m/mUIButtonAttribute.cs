using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class mUIButtonAttribute : MonoBehaviour, IController
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    Action<object> action;
    public void SetAction(Action<object> action)
    {
        this.action = action;
    }
    public void RunAction(object value)
    {
        action?.Invoke(value);
    }

    public IArchitecture GetArchitecture()
    {
        throw new NotImplementedException();
    }
}
