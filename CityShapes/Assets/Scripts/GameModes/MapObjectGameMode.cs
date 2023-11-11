using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapObjectGameMode : GameMode
{
    private MapObject _CurrentMapObject;
    private City _City => GameManager.Instance.City;

    public override void MapObjectPressed(MapObject mapObject)
    {
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
