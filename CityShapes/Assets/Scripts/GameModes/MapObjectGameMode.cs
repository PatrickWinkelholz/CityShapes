using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapObjectGameMode : GameMode
{
    //[SerializeField] private UnityEngine.UI.InputField _NameInputField = default;
    //[SerializeField] private UnityEngine.UI.InputField _RegionInputField = default;

    private MapObject _CurrentMapObject;

    //private bool EnterMapObjectNamesMode = false;
    private City _City => GameManager.Instance.City;

    public override void MapObjectPressed(MapObject mapObject)
    {
        //if (EnterMapObjectNamesMode)
        //{
        //    CityData.MapObjectData MapObjectData = _City.CityData.MapObjects[mapObject.CityDataIndex];
        //    MapObjectData.Name = _NameInputField.text;
        //    MapObjectData.Region = _RegionInputField.text;
        //    _City.CityData.MapObjects[mapObject.CityDataIndex] = MapObjectData;
        //    mapObject.SetColor(Color.green);
        //    mapObject.Locked = true;
        //    return;
        //}

        if (_CurrentMapObject == mapObject)
        {
            mapObject.Lock(true);
            GameManager.Instance.Score++;
        }
        else
        {
            _CurrentMapObject.Lock(false);
        }
        NextMapObject();
    }

    private void OnEnable()
    {
        GameManager.Instance.GameRestartingEvent += OnGameRestarting;
    }

    private void OnDisable()
    {
        GameManager.Instance.GameRestartingEvent -= OnGameRestarting;
    }

    private void OnGameRestarting()
    {
        _City.MapObjects.Reset();
        foreach (MapObject mapObject in _City.MapObjects)
        {
            mapObject.Reset();
        }

        NextMapObject();
    }

    private void NextMapObject()
    {
        if (_City.MapObjects.Empty())
        {
            GameManager.Instance.EndGame(true);
        }
        else
        {
            _CurrentMapObject = _City.MapObjects.NextRandom();
            InvokeElementChangedEvent(_CurrentMapObject.MapObjectName);
        }
    }
}
