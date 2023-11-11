using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//currently unused
public class LandmarkGameMode : GameMode
{
    [SerializeField] private LandmarkCollection _Landmarks = null;
    public GameObject Marker = null;
    public GameObject NewMarker = null;

    public override void MapObjectPressed(MapObject mapObject)
    {
    }

    private void Start()
    {
        foreach(LandmarkCollection.LandmarkData data in _Landmarks.Landmarks)
        {
            Instantiate(Marker, (Vector3)data.Location, Quaternion.identity);
        }
    }
}
