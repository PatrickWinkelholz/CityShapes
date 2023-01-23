using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Utils
{
    public readonly static System.Globalization.NumberStyles NumberStyle = System.Globalization.NumberStyles.Number;
    public readonly static System.Globalization.CultureInfo CultureInfo = System.Globalization.CultureInfo.InvariantCulture;

    public static bool TryParse(string s, out int i)
    {
        return int.TryParse(s, NumberStyle, CultureInfo, out i);
    }

    public static bool TryParse(string s, out float f)
    {
        return float.TryParse(s, NumberStyle, CultureInfo, out f);
    }

    public static bool TryParse(string s, out long l)
    {
        return long.TryParse(s, NumberStyle, CultureInfo, out l);
    }

    public static string FormatTime(float seconds)
    {
        return System.TimeSpan.FromSeconds(seconds).ToString("mm\\:ss\\:ff");
    }

    public static string FormatTime(string seconds)
    {
        if (TryParse(seconds, out float secondsf))
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
