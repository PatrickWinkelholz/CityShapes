using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int Score;
    public bool GameOver { get; private set; }
    public event System.Action GameOverEvent;
    public event System.Action GameRestartingEvent;

    [SerializeField] private CityCollection _CityCollection = null;
    public GameMode GameMode { get => _GameMode; }
    [SerializeField] private GameMode _GameMode = null;
    public CameraController Camera { get => _Camera; }
    [SerializeField] private CameraController _Camera = null;

    [SerializeField] private City _CityPrefab = default;

    public City City => _City;
    private City _City = default;
    public OsmDataProcessor OsmProcessor = new OsmDataProcessor();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        //RestartGame();
    }

    public void DistrictPressed(District district)
    {
        _GameMode.DistrictPressed(district);
    }

    public void RestartGame()
    {
        Score = 0;
        GameOver = false;        
        GameRestartingEvent?.Invoke();
    }

    public IEnumerator ChangeCity(string cityName, string boundingBox)
    {
        System.Action<CityData> callback = (cityData) =>
        {
            if (!_CityCollection.Cities.ContainsKey(cityName))
            {
                _CityCollection.Cities.Add(cityName, cityData);
            }

            if (cityData.Districts.Count > 0)
            {
                if (_City)
                {
                    Destroy(_City.gameObject);
                }
                _City = Instantiate(_CityPrefab);
                _City.Initialize(cityData);
                RestartGame();
            }
        };
        if (_CityCollection.Cities.TryGetValue(cityName, out CityData outCityData))
        {
            callback.Invoke(outCityData);
        }
        else
        {
            yield return OsmProcessor.GenerateCityData(cityName, boundingBox, callback);
        }
    }

    public void EndGame()
    {
        GameOverEvent?.Invoke();
    }
}
