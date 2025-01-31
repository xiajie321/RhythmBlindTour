using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMonoUAP : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        "请说，speak".UAPSpeak();        
    }

    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space))
        {
            //
            "测试一下 test".UAPSpeak();
        }
    }
}
