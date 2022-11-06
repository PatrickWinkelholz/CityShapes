using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
