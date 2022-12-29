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
        if (float.TryParse(seconds, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out float secondsf))
        {
            return FormatTime(secondsf);
        }
        return seconds;
    }

    public static IEnumerator SendWebRequest(string url, System.Action<byte[]> callback)
    {
        yield return ExecuteWebRequest(url, default, (request) => callback?.Invoke(request.downloadHandler.data));
    }

    public static IEnumerator SendWebRequest(string url, WWWForm form, System.Action<string> callback)
    {
        yield return ExecuteWebRequest(url, form, (request) => callback?.Invoke(request.result != UnityWebRequest.Result.Success ?
            ("WebRequest Error: " + request.downloadHandler.error) : request.downloadHandler.text));
    }

    public static IEnumerator SendWebRequest(string url, System.Action<string> callback)
    {
        yield return SendWebRequest(url, default, callback);
    }

    private static IEnumerator ExecuteWebRequest(string url, WWWForm form, System.Action<UnityWebRequest> callback)
    {
        using (UnityWebRequest webRequest = form == default ? UnityWebRequest.Get(url) : UnityWebRequest.Post(url, form))
        {
            webRequest.SetRequestHeader("accept-language", "en"); //"de-DE,de;q=0.9,en-GB;q=0.8,en;q=0.7,en-US;q=0.6");
            yield return webRequest.SendWebRequest();

            callback?.Invoke(webRequest);
        }
    }
}
