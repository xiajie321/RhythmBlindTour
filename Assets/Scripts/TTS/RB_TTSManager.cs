using UnityEngine;

[AddComponentMenu("RB/Test/RB_TestManager (DontDestroyOnLoad)")]
public class RB_TestManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}