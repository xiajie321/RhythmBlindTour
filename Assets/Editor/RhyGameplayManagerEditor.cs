using Gameplay;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RhyGameplayManager))]
public class RhyGameplayManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (!Application.isPlaying) return;
        GUILayout.Button(RhyGameplayManager.Instance.ChartTiming.ToString());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Play"))
        {
            RhyGameplayManager.Instance.Play();
        }

        if (GUILayout.Button("Pause"))
        {
            RhyGameplayManager.Instance.Pause();
        }

        if (GUILayout.Button("Reset"))
        {
            RhyGameplayManager.Instance.Reset();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("LoadChart"))
        {
            RhyGameplayManager.Instance.LoadChart();
        }
    }
}