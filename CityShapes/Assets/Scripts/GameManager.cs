using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int Score;
    public float Timer { get; private set; }
    public bool GameOver { get; private set; }
    public event System.Action GameOverEvent;
    public event System.Action GameRestartingEvent;

    [SerializeField] private CityCollection _CityCollection = null;
    public GameMode GameMode { get => _GameMode; }
    [SerializeField] private GameMode _GameMode = null;
    public CameraController Camera { get => _Camera; }
    [SerializeField] private CameraController _Camera = null;

    [SerializeField] private City _CityPrefab = default;
    [SerializeField] private SpriteRenderer _BackgroundSpritePrefab = default;

    private List<SpriteRenderer> _BackgroundSprites = null;
    public City City => _City;
    private City _City = default;
    public OsmDataProcessor OsmProcessor = new OsmDataProcessor();

    private void Awake()
    {
        Instance = this;
        GameOver = true;
    }

    private void Start()
    {
        //RestartGame();
    }

    private void Update()
    {
        if (!GameOver)
        {
            Timer += Time.deltaTime;
        }
    }

    public void DistrictPressed(District district)
    {
        _GameMode.DistrictPressed(district);
    }

    public void RestartGame()
    {
        Score = 0;
        Timer = 0.0f;
        GameOver = false;        
        GameRestartingEvent?.Invoke();
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

            if (!_CityCollection.Cities.ContainsKey(cityName))
            {
                _CityCollection.Cities.Add(cityName, cityData);
            }

            if (cityData.Districts != null && cityData.Districts.Count > 0)
            {
                if (_City)
                {
                    Destroy(_City.gameObject);
                }
                if (_BackgroundSprites != null)
                {
                    foreach (SpriteRenderer sprite in _BackgroundSprites)
                    {
                        Destroy(sprite.gameObject);
                    }
                    _BackgroundSprites.Clear();
                }

                Debug.Log("creating city with " + cityData.Districts.Count + " districts and center " + cityData.Shape.Center);

                _City = Instantiate(_CityPrefab);
                _City.Initialize(cityData);

                //_BackgroundSprites
                _BackgroundSprites = new List<SpriteRenderer>();
                foreach (TileData tileData in cityData.BackgroundTiles)
                {
                    SpriteRenderer sprite = Instantiate(_BackgroundSpritePrefab);
                    sprite.sprite = tileData.Sprite;
                    sprite.transform.position = tileData.Pos;
                    _BackgroundSprites.Add(sprite);
                }

                RestartGame();
                cityChangedCallback?.Invoke("success");
            }
            else
            {
                cityChangedCallback?.Invoke("Can't generate cityData for " + cityName);
            }
        };

        if (_CityCollection.Cities.TryGetValue(cityName, out CityData outCityData))
        {
            callback.Invoke("success", outCityData);
        }
        else
        {
            yield return OsmProcessor.GenerateCityData(cityName, boundingBox, callback);
        }
    }

    public void EndGame()
    {
        GameOverEvent?.Invoke();
        GameOver = true;
    }
}
