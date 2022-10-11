using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DistrictData
{
    public string Name;
    public string Region;
    public List<Vector2> Shape;
}

[System.Serializable]
public struct CityData
{
    public string Name;
    public Vector2 Center;
    public List<DistrictData> Districts;
}

[CreateAssetMenu(fileName="CityCollection", menuName="CityCollection")]
public class CityCollection : ScriptableObject
{
    [SerializeField] public Dictionary<string, CityData> Cities = new Dictionary<string, CityData>();
}
