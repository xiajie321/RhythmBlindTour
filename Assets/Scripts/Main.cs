using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Views.UIManager.UIPanels;

public class Main : MonoBehaviour
{
    public AudioSource TTS;
    //入口
    void Start()
    {
        Views.UIManager.UIManager.Instance.ShowPanel<MainPanel>();
        SceneManager.LoadSceneAsync("Level One",LoadSceneMode.Additive);
    }

    private void Update()
    {
        TTS.volume = PlayerPrefs.GetFloat("ReaderVolume", 40f) / 100f;
    }
}
