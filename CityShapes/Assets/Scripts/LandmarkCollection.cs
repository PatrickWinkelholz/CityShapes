using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//currently unused
[CreateAssetMenu(fileName = "LandmarkCollection")]
public class LandmarkCollection : ScriptableObject
{
    [System.Serializable]
    public struct LandmarkData
    {
        public string Name;
        public string LatLongInput;
        public Vector2 Location;
    }

    public List<LandmarkData> Landmarks = default;

    public void CalculateLocations()
    {
        for(int i = 0; i < Landmarks.Count; i++)
        {
            LandmarkData data = Landmarks[i];

            string latLong = data.LatLongInput.Replace(" ", "");
            string[] splitLatLong = latLong.Split(',');
            Vector2 pos = new Vector2(float.Parse(splitLatLong[0]), float .Parse(splitLatLong[1]));
            data.Location = pos;
            Landmarks[i] = data;
        }
    }
}
