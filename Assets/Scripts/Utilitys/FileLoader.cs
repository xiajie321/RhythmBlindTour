using QFramework;
using UnityEngine;
using UnityEngine.Networking;
using SFB;

public class FileLoader : IUtility
{
    public static string OpenFolderPanel()
    {
        var extensions = new[] { new ExtensionFilter("Sound Files", "mp3", "wav", "ogg") };
        var path = StandaloneFileBrowser.OpenFilePanel("Select an audio file", "", extensions, false);
        return path[0];
    }

    public static AudioClip LoadAudioClip(string path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path))
            {
                return LoadAudio(path);
            }
            Debug.LogError("音频文件路径无效");
            return null;

        }
        catch (System.Exception ex)
        {
            Debug.LogError("音频加载时发生异常: " + ex.Message);
            return null;
        }
    }

    private static AudioClip LoadAudio(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN))
        {
            var asyncOperation = www.SendWebRequest();

            while (!asyncOperation.isDone)
            {
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("音频加载失败: " + www.error);
                return null;
            }

            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            return audioClip;
        }
    }
}
