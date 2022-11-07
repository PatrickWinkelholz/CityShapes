using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Utils
{
    public static string FormatTime(float seconds)
    {
        return System.TimeSpan.FromSeconds(seconds).ToString("mm\\:ss\\:ff");
    }

    public static string FormatTime(string seconds)
    {
        if (float.TryParse(seconds, out float secondsf))
        {
            return FormatTime(secondsf);
        }
        return seconds;
    }

    public static IEnumerator SendWebRequest(string url, WWWForm form, System.Action<string> callback)
    {
        using (UnityWebRequest webRequest = form == default ? UnityWebRequest.Get(url) : UnityWebRequest.Post(url, form)) 
        {
            yield return webRequest.SendWebRequest();
            callback?.Invoke(webRequest.result != UnityWebRequest.Result.Success ?
                ("WebRequest Error: " + webRequest.error) :
                webRequest.downloadHandler.text);
        };
    }

    public static IEnumerator SendWebRequest(string url, System.Action<string> callback)
    {
        yield return SendWebRequest(url, default, callback);
    }
}
