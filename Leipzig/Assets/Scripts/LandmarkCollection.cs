using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        // maps top seehausen: 51.448069628554904, 12.441200862517055
        // maps bottom right liebertwolkwitz 51.25741908927664, 12.488002070774924
        // maps Hauptbahnhof 51.34441176033315, 12.380807364201587

        // game top seehanuse: 341.121, 171.415 
        // game bottom right liebertwolkwitz 478.289, 487.334

        for(int i = 0; i < Landmarks.Count; i++)
        {
            LandmarkData data = Landmarks[i];

            string latLong = data.LatLongInput.Replace(" ", "");
            string[] splitLatLong = latLong.Split(',');
            //Vector2 pos = new Vector2((float)( -16.43 * double.Parse(splitLatLong[0]) + 846.1), (float)(-18.45 * double.Parse(splitLatLong[1]) + 225.0));
            Vector2 pos = new Vector2(float.Parse(splitLatLong[0]), float .Parse(splitLatLong[1]));
            data.Location = pos;
            Landmarks[i] = data;
        }
    }
}
