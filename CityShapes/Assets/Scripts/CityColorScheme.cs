using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="CityColorScheme", menuName="CityColorScheme")]
public class CityColorScheme : ScriptableObject
{
    [System.Serializable]
    public struct ColorEntry
    {
        public string Region;
        public Color Color;
    }

    public List<ColorEntry> RegionColors;
}
