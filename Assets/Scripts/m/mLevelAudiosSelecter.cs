using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mLevelAudiosSelecter : MonoBehaviour
{
    private mDefaultAudioInitializer mDefaultAudioInitializer;

    private void Awake()
    {
        mDefaultAudioInitializer = this.GetComponent<mDefaultAudioInitializer>();
    }

    public void SetLevelIndex(int index)
    {
        if (mDefaultAudioInitializer != null)
        {
            mDefaultAudioInitializer.selectedIndex = index;
        }
        else
        {
            Debug.LogWarning("mDefaultAudioInitializer is not assigned!");
        }
    }
}
