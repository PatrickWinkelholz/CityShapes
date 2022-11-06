using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LandmarkCollection))]
public class LandmarkCollectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        LandmarkCollection collection = (LandmarkCollection)target;
        if (GUILayout.Button("Calculate Locations"))
        {
            collection.CalculateLocations();
        }
    }
}
