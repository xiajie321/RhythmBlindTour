using Qf.Models;
using QFramework;
using System.Collections.Generic;
using UnityEngine;

public class UIResourceSelectList : MonoBehaviour, IController
{
    public enum AudioSourceType { Internal, External }

    [SerializeField] GameObject UIItemProfab;
    [SerializeField] GameObject ObjectParent;

    List<UIResourceItem> _UIItems = new();
    List<AudioClip> currentClips = new();
    AudioSourceType currentSource = AudioSourceType.Internal;

    /// <summary>
    /// 统一入口，清空 UI 并重建
    /// </summary>
    void RebuildUI(List<AudioClip> clips, AudioSourceType source)
    {
        currentSource = source;
        currentClips.Clear();
        currentClips.AddRange(clips);

        //  彻底清空 ObjectParent 下的所有子物体（不依赖 _UIItems 列表）
        for (int i = ObjectParent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(ObjectParent.transform.GetChild(i).gameObject);
        }

        _UIItems.Clear();

        if (currentClips == null || currentClips.Count == 0)
            return;

        foreach (var clip in currentClips)
        {
            var newItem = Instantiate(UIItemProfab).GetComponent<UIResourceItem>();
            newItem.transform.SetParent(ObjectParent.transform, false);
            newItem.SetAudioClip(clip);
            newItem.SetName(clip.name);
            _UIItems.Add(newItem);
        }
    }


    /// <summary>
    /// 加载内部音频资源并刷新 UI（彻底清空旧列表）
    /// </summary>
    public void ShowInternalResources()
    {
        var modelClips = this.GetModel<DataCachingModel>().GetListAudioClips();
        RebuildUI(modelClips, AudioSourceType.Internal);
    }

    /// <summary>
    /// 显示外部音频资源（由外部 Loader 提供）
    /// </summary>
    public void ShowExternalResources(List<AudioClip> externalClips)
    {
        RebuildUI(externalClips, AudioSourceType.External);
    }

    public IArchitecture GetArchitecture()
    {
        return GameBody.Interface;
    }
}
