using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class GameEnter : MonoBehaviour,IController
{
    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }

    private void Awake()
    {
        GetArchitecture();
    }

    public void EnterGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("TestScene");
    }
}
