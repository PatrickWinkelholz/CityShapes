using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundTiles", menuName = "BackgroundTiles")]
public class BackgroundTilesAsset : ScriptableObject
{
    //jamming camera data in here for now. not that clean but eh
    public float MaxCameraSize = 6.5f;
    public float MinCameraSize = 0.8f;
    public Vector2 MinCameraPosition = default;
    public Vector2 MaxCameraPosition = default;
    public Vector3 CameraStartPosition = default;

    [System.Serializable]
    public struct SerializableSprite
    {
        public byte[] Data;
        public Vector3 Position;
    }

    public IReadOnlyList<SerializableSprite> Tiles => _Tiles.AsReadOnly();
    [SerializeField] private List<SerializableSprite> _Tiles;

    public void SaveTile(byte[] data, Vector3 position)
    {
        SerializableSprite sprite = new SerializableSprite();
        sprite.Data = data;
        sprite.Position = position;
        _Tiles.Add(sprite);
    }

    public void ClearTiles()
    {
        _Tiles.Clear();
    }
}
