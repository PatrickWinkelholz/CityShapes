using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Shape
{
    public Vector2 Center;
    public List<Vector2> Points;
}

[System.Serializable]
public struct MapObjectData
{
    public string Name;
    public string Region;
    public Shape Shape;
}

[System.Serializable]
public struct CityData
{
    public string Name;
    public Shape Shape;
    public List<MapObjectData> MapObjects;
    public TileData[,] BackgroundTiles;
}

[CreateAssetMenu(fileName="CityCollection", menuName="CityCollection")]
public class CityCollection : ScriptableObject
{
    [SerializeField] public Dictionary<string, CityData> Cities = new Dictionary<string, CityData>();
}
