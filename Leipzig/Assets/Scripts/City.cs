using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour
{
    public CityData CityData { get => _CityData; }
    [SerializeField] private CityData _CityData = default;
    [SerializeField] private District _DistrictPrefab = default;
    [SerializeField] private float _ScaleFactor = 0.0095f;
    public float ScaleFactor => _ScaleFactor;

    public Pool<District> Districts = new Pool<District>();

    public void Awake()
    {
        for ( int i = 0; i < _CityData.Districts.Count; i++ )
        {
            CityData.DistrictData districtData = _CityData.Districts[i];
            District district = Instantiate(_DistrictPrefab, transform, true);
            district.Initialize(districtData);
            district.CityDataIndex = i;
            Districts.Add(district);
        }
        transform.localScale = new Vector3(_ScaleFactor, -_ScaleFactor, _ScaleFactor);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ResetCamera();
    }

    private void ResetCamera()
    {
        Vector2 cameraPosition = Vector2.zero;
        for (int i = 0; i < Districts.Count; i++)
        {
            cameraPosition += Districts[i].CenterPoint;
        }
        cameraPosition /= _CityData.Districts.Count;
        cameraPosition *= _ScaleFactor;
        Camera.main.transform.position = new Vector3(cameraPosition.x + 0.2f, -cameraPosition.y, -10);
    }
}
