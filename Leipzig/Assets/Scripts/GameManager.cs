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
    private OsmDataProcessor _OsmProcessor = new OsmDataProcessor();

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

    public void SearchCity(string cityName)
    {
        cityName = cityName.ToLower();

        if (_CityCollection.Cities.TryGetValue(cityName, out CityData cityData))
        {
            if (_City)
            {
                Destroy(_City.gameObject);
            }
            _City = Instantiate(_CityPrefab);
            _City.Initialize(cityData);
            RestartGame();
        }
        else
        {
            StartCoroutine(_OsmProcessor.GenerateCityData(cityName,(outCityData)=>
            {
                if (_City)
                {
                    Destroy(_City.gameObject);
                }
                _CityCollection.Cities.Add(cityName, outCityData);
                _City = Instantiate(_CityPrefab);
                _City.Initialize(outCityData);
                RestartGame();
            }));
        }
    }

    public void EndGame()
    {
        GameOverEvent?.Invoke();
    }
}
