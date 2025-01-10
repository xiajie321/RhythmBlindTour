using System.Windows.Forms;
using QFramework;
using UnityEngine;
using UnityEngine.Networking;

public class FileLoader : IUtility
{
    public static string OpenFolderPanel()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Audio Files|*.wav;*.mp3;*.ogg",
            Title = "Select an audio file"
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            return openFileDialog.FileName;
        }
        return null;
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
