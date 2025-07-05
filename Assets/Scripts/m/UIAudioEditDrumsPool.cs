///
/// [功能]
/// 为每个轨道管理独立的鼓点 UI 池，池内鼓点对象始终挂在对应轨道下，避免 SetParent。
///
/// [目的]
/// 优化性能，简化结构，提升 UI 更新效率，兼容按轨道独立更新与展示。 -- mixyao/25/07/04
///

using System.Collections.Generic;
using UnityEngine;

public class UIAudioEditDrumsPool
{
    private GameObject prefab;
    private RectTransform[] trackParents;
    private List<List<GameObject>> poolPerTrack = new();
    private List<int> activeCountPerTrack = new();

    public UIAudioEditDrumsPool(GameObject prefabRef, RectTransform[] trackRoots)
    {
        prefab = prefabRef;
        trackParents = trackRoots;

        for (int i = 0; i < trackRoots.Length; i++)
        {
            poolPerTrack.Add(new List<GameObject>());
            activeCountPerTrack.Add(0);
        }
    }

    public GameObject Get(int trackIndex, float drumX)
    {
        if (trackIndex < 0 || trackIndex >= trackParents.Length) return null;

        var pool = poolPerTrack[trackIndex];
        int active = activeCountPerTrack[trackIndex];

        GameObject drum;

        if (active < pool.Count)
        {
            drum = pool[active];
        }
        else
        {
            drum = GameObject.Instantiate(prefab, trackParents[trackIndex]);
            pool.Add(drum);
        }

        activeCountPerTrack[trackIndex]++;
        drum.SetActive(true);
        var rect = drum.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(drumX, 0);

        return drum;
    }

    public void RecycleAll()
    {
        for (int i = 0; i < poolPerTrack.Count; i++)
        {
            var pool = poolPerTrack[i];
            int active = activeCountPerTrack[i];

            for (int j = active; j < pool.Count; j++)
            {
                if (pool[j] != null)
                    pool[j].SetActive(false);
            }

            activeCountPerTrack[i] = 0;
        }
    }

    public void ClearUnused()
    {
        for (int i = 0; i < poolPerTrack.Count; i++)
        {
            var pool = poolPerTrack[i];
            int active = activeCountPerTrack[i];

            for (int j = pool.Count - 1; j >= active; j--)
            {
                GameObject.Destroy(pool[j]);
                pool.RemoveAt(j);
            }
        }
    }
}
