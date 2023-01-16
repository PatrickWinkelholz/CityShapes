using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour
{
    [SerializeField] private List<MapObject> _MapObjectPrefabs = default;
    public Pool<MapObject> MapObjects = new Pool<MapObject>();
    public CityData CityData => _CityData;
    private CityData _CityData = default;

    public void Initialize(CityData cityData)
    {
        _CityData = cityData;
        gameObject.name = cityData.Name;
        MapObjects.Clear();
        for(int i = transform.childCount - 1; i >= 0; i-- )
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        for ( int i = 0; i < cityData.MapObjects.Count; i++ )
        {
            MapObjectData mapObjectData = cityData.MapObjects[i];

            MapObject mapObject = Instantiate(_MapObjectPrefabs.Find( x => x.Type == GameManager.Instance.MapObjectType), transform, true);
            mapObject.Initialize(mapObjectData);
            mapObject.CityDataIndex = i;
            MapObjects.Add(mapObject);
        }

        Camera.main.GetComponent<CameraController>().Reset(cityData);
    }
}
