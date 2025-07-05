using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mScreenFullScreen : MonoBehaviour
{
    // Start is called before the first frame update
    bool fullScreenTrigger = false;
    public void ScreenModeChange()
    {
        if (fullScreenTrigger)
        {
            Screen.fullScreen = false;
            fullScreenTrigger = false;
        }
        else
        {
            Screen.fullScreen = true;
            fullScreenTrigger = true;
        }
    }
}
