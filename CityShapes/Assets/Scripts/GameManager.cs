using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using UnityEngine;
using static BackgroundTilesAsset;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int Score;
    public float Timer { get; private set; }
    public bool GameOver { get; private set; }
    public event System.Action<bool> GameOverEvent;
    public event System.Action GameRestartingEvent;

    public OsmDataProcessor OsmDataProcessor => _OsmDataProcessor;
    [SerializeField] private OsmDataProcessor _OsmDataProcessor = default;
    public GameMode GameMode => _GameMode;
    [SerializeField] private GameMode _GameMode = null;
    public CameraController Camera => _Camera;
    [SerializeField] private CameraController _Camera = null;

    [SerializeField] private City _CityPrefab = default;
    [SerializeField] private SpriteRenderer _BackgroundSpritePrefab = default;
    [SerializeField] private BackgroundTilesAsset _BackgroundTilesAsset = default;

    private List<SpriteRenderer> _BackgroundSprites = null;
    public City City => _City;
    private City _City = default;

    public ObjectType MapObjectType = ObjectType.District;

    private void Awake()
    {
        CultureInfo.CurrentCulture = Utils.CultureInfo;
        Instance = this;
        GameOver = true;
    }

    private void Start()
    {
        if (_BackgroundTilesAsset.Tiles != null && _BackgroundTilesAsset.Tiles.Count > 0)
        {
            GenerateBackgroundTiles(_BackgroundTilesAsset.Tiles);
            _Camera.ActivateMenuMode();
        }        
    }

    private void Update()
    {
        if (!GameOver)
        {
            Timer += Time.deltaTime;
        }
    }

    public void MapObjectPressed(MapObject mapObject)
    {
        _GameMode.MapObjectPressed(mapObject);
    }

    public void RestartGame()
    {
        Score = 0;
        Timer = 0.0f;
        GameOver = false;        
        GameRestartingEvent?.Invoke();
    }

    public IEnumerator ChangeMode(ObjectType mode, System.Action<string> callback)
    {
        MapObjectType = mode;
        if (_City != null)
        {
            yield return OsmDataProcessor.GenerateMapObjects((result, mapObjects) =>
            {
                if (result != "success")
                {
                    callback?.Invoke(result);
                    return;
                }
                CityData cityData = _City.CityData;
                cityData.MapObjects = mapObjects;
                _City.Initialize(cityData);
                RestartGame();
            });
        }
        callback?.Invoke("success");
    }

    public IEnumerator ChangeCity(string cityName, string boundingBox, System.Action<string> cityChangedCallback)
    {
        System.Action<string, CityData> callback = (result, cityData) =>
        {
            if (result != "success")
            {
                cityChangedCallback?.Invoke(result);
                return;
            }

            if (_City)
            {
                Destroy(_City.gameObject);
            }


            Debug.Log("creating city with " + cityData.MapObjects.Count + " mapObjects and center " + cityData.Shape.Center);

            _City = Instantiate(_CityPrefab);
            _City.Initialize(cityData);

            GenerateBackgroundTiles(cityData.BackgroundTiles);

            RestartGame();
            cityChangedCallback?.Invoke("success");
        };
        yield return _OsmDataProcessor.GenerateCityData(cityName, boundingBox, callback);

        //TODO: caching!!!!
    }

    public void GenerateBackgroundTiles(IReadOnlyList<SerializableSprite> tiles)
    {
        TileData[,] tileData = new TileData[tiles.Count, 1];
        for (int i = 0; i < tiles.Count; i++)
        {
            SerializableSprite tile = tiles[i];
            tileData[i, 0].Sprite = Utils.LoadSprite(_OsmDataProcessor.TileResolution, tile.Data);
            tileData[i, 0].Pos = tile.Position;
        }
        GenerateBackgroundTiles(tileData);
    }

    public void GenerateBackgroundTiles(TileData[,] backgroundTiles)
    {
        if (_BackgroundSprites != null)
        {
            foreach (SpriteRenderer sprite in _BackgroundSprites)
            {
                Destroy(sprite.gameObject);
            }
            _BackgroundSprites.Clear();
        }

        _BackgroundSprites = new List<SpriteRenderer>();
        foreach (TileData tileData in backgroundTiles)
        {
            SpriteRenderer sprite = Instantiate(_BackgroundSpritePrefab);
            sprite.sprite = tileData.Sprite;
            sprite.transform.position = tileData.Pos;
            _BackgroundSprites.Add(sprite);
        }
    }

    public void EndGame(bool submitScore)
    {
        GameOverEvent?.Invoke(submitScore);
        GameOver = true;
    }
}
