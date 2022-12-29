using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour
{
    [SerializeField] private District _DistrictPrefab = default;
    public Pool<District> Districts = new Pool<District>();

    public void Initialize(CityData cityData)
    {
        gameObject.name = cityData.Name;
        for ( int i = 0; i < cityData.Districts.Count; i++ )
        {
            DistrictData districtData = cityData.Districts[i];
            District district = Instantiate(_DistrictPrefab, transform, true);
            district.Initialize(districtData);
            district.CityDataIndex = i;
            Districts.Add(district);
        }

        Camera.main.GetComponent<CameraController>().Reset(cityData);
    }
}
