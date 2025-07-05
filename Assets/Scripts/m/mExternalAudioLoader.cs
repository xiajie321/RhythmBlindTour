using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class mExternalAudioLoader : MonoBehaviour
{
    public string externalAudioPath = "UserAudios";
    public List<AudioClip> loadedClips = new();
    public UIResourceSelectList uiList;

    public void LoadExternalAudios()
    {
        StartCoroutine(LoadAudiosCoroutine());
    }

    IEnumerator LoadAudiosCoroutine()
    {
        loadedClips.Clear();

        string fullPath = Path.Combine(Application.dataPath, "..", externalAudioPath);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            yield return null;
            uiList?.ShowExternalResources(loadedClips);
            yield break;
        }

        string[] extensions = { "*.wav", "*.ogg", "*.mp3" };
        List<string> audioFiles = new();
        foreach (var ext in extensions)
            audioFiles.AddRange(Directory.GetFiles(fullPath, ext));

        foreach (string filePath in audioFiles)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            AudioType type = ext switch
            {
                ".wav" => AudioType.WAV,
                ".ogg" => AudioType.OGGVORBIS,
                ".mp3" => AudioType.MPEG,
                _ => AudioType.UNKNOWN
            };
            if (type == AudioType.UNKNOWN) continue;

            string url = "file:///" + filePath.Replace("\\", "/");

            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, type);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                clip.name = Path.GetFileNameWithoutExtension(filePath);
                loadedClips.Add(clip);
            }
        }

        uiList?.ShowExternalResources(loadedClips);
    }
}
