﻿using QFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public class UIAudioEditShowAllTimePlane : MonoBehaviour, IController
{

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}

