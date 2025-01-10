using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using Qf.Commands.AudioEdit;
public class THGTest : MonoBehaviour,IController
{
    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }

    public void Test()
    {
        AudioClip clip = FileLoader.LoadAudioClip(FileLoader.OpenFolderPanel());
        GetComponent<AudioSource>().clip = clip;
        //GetComponent<AudioSource>().Play();
        this.SendCommand(new SetAudioEditAudioCommand(clip));
    }
}
