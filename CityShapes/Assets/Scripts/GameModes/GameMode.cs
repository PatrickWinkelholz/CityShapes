using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameMode : MonoBehaviour
{
    public event System.EventHandler<string> ElementChangedEvent;
    public string GameModeName { get => _GameModeName; }
    [SerializeField] private string _GameModeName = default;

    public abstract void MapObjectPressed(MapObject mapObject);

    protected void InvokeElementChangedEvent(string element)
    {
        ElementChangedEvent?.Invoke(this, element);
    }
}