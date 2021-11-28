using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="CityData", menuName="CityData")]
public class CityData : ScriptableObject
{
    [System.Serializable]
    public struct DistrictData
    {
        public string Name;
        public string Region;
        public List<Vector2> Shape;
    }

    public List<DistrictData> Districts;
}
