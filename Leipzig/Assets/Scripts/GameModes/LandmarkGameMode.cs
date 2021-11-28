using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandmarkGameMode : GameMode
{
    [SerializeField] private LandmarkCollection _Landmarks = null;
    [SerializeField] private LandmarkCollection _NewLandmarks = null;
    public GameObject Marker = null;
    public GameObject NewMarker = null;

    public override void DistrictPressed(District district)
    {
    }

    private void Start()
    {
        foreach(LandmarkCollection.LandmarkData data in _Landmarks.Landmarks)
        {
            Instantiate(Marker, (Vector3)data.Location, Quaternion.identity);
        }
        foreach (LandmarkCollection.LandmarkData data in _NewLandmarks.Landmarks)
        {
            Instantiate(NewMarker, (Vector3)data.Location, Quaternion.identity);
        }
    }
}
