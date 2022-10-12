using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    public bool Paused => _Paused;
    private bool _Paused = true;

    public static PausePanel Instance;

    [SerializeField] private float _lerpSpeed = default;
    [SerializeField] private TMPro.TextMeshProUGUI _ScoreDisplay = null;
    [SerializeField] private TMPro.TextMeshProUGUI _HighScoreDisplay = null;
    [SerializeField] private Button _DistrictsButton = null;
    [SerializeField] private Button _LandmarksButton = null;
    [SerializeField] private TMPro.TMP_InputField _SearchInputField = null;
    [SerializeField] private GameObject _CitySearchPanel = null;
    [SerializeField] private GameObject _MenuContentPanel = null;
    [SerializeField] private SearchResultEntry _SearchResultEntryPrefab = default;
    [SerializeField] private Transform _SearchResultViewportContent = default;
    [SerializeField] private Transform _LoadingPanel = default;
    [SerializeField] private Button _BackButton = default;

    private Vector3 _NotPausedPosition = default;
    private Vector3 _PausedPosition = Vector3.zero;
    private Vector3 _TargetPosition = default;
    private GameObject _CurrentPanel = default;
    private int _HighScore = 0;

    private void Awake()
    {
        Instance = this;
        _CurrentPanel = _CitySearchPanel;
        _NotPausedPosition = new Vector3(0, -Screen.height, 0);
    }

    private void Start()
    {
        UpdateScoreDisplay();
    }

    private void OnEnable()
    {
        GameManager.Instance.GameOverEvent += OnGameOver;
        GameManager.Instance.GameRestartingEvent += OnRestarting;
    }

    private void OnDisable()
    {
        GameManager.Instance.GameOverEvent -= OnGameOver;
        GameManager.Instance.GameRestartingEvent -= OnRestarting;
    }

    private void OnGameOver()
    {
        TogglePause();
    }

    public void TogglePause()
    {
        _Paused = !_Paused;
        if (_Paused)
        {
            TrySetNewHighscore();
        }
        UpdateScoreDisplay();
        GameManager.Instance.Camera.Blocked = _Paused;
        _TargetPosition = _Paused ? _PausedPosition : _NotPausedPosition;
    }

    private void ChangePanel(GameObject panel)
    {
        _CurrentPanel.SetActive(false);
        _CurrentPanel = panel;
        panel.SetActive(true);
    }

    private void OnRestarting()
    {
        FetchHighScore();
        ChangePanel(_MenuContentPanel);
        _BackButton.gameObject.SetActive(true);
        TogglePause();
    }

    private void UpdateScoreDisplay()
    {
        _ScoreDisplay.text = GameManager.Instance.Score.ToString();
        _HighScoreDisplay.text = _HighScore == 0 ? "-" : _HighScore.ToString();
    }

    private void FetchHighScore()
    {
        string key = GameManager.Instance.City.gameObject.name;
        if (PlayerPrefs.HasKey(key))
        {
            _HighScore = PlayerPrefs.GetInt(key);
        }
        else
        {
            _HighScore = 0;
        }
    }

    public void OnDistrictsPressed()
    {
        _LandmarksButton.interactable = true;
        _DistrictsButton.interactable = false;

        UpdateScoreDisplay();
    }

    public void OnLandmarksPressed()
    {
        _LandmarksButton.interactable = false;
        _DistrictsButton.interactable = true;

        UpdateScoreDisplay();
    }

    public void OnBackPressed()
    {
        ChangePanel(_MenuContentPanel);
    }

    public void OnChangeCityPressed()
    {
        ChangePanel(_CitySearchPanel);
    }

    public void TrySetNewHighscore()
    {
        if (GameManager.Instance.Score > _HighScore)
        {
            _HighScore = GameManager.Instance.Score;
            string key = GameManager.Instance.City.gameObject.name;
            PlayerPrefs.SetInt(key, _HighScore);
        }
    }

    public void OnWebsitePressed()
    {
        Application.OpenURL("https://patrickwinkelholz.com/");
    }

    public void OnContactPressed()
    {
        Application.OpenURL("mailto:patrick.winkelholz@gmail.com?subject=Leipzig%20App");
    }

    public void OnRestartPressed()
    {
        GameManager.Instance.RestartGame();
    }

    public void OnSearchPressed()
    {
        StartCoroutine(SearchCityRoutine());
    }

    private IEnumerator GenerateCityRoutine(string cityName, string boundingBox)
    {
        _LoadingPanel.gameObject.SetActive(true);
        yield return GameManager.Instance.ChangeCity(cityName, boundingBox);
        _LoadingPanel.gameObject.SetActive(false);
    }

    private void StartGenerateCityRoutine(string cityName, string boundingBox)
    {
        StartCoroutine(GenerateCityRoutine(cityName, boundingBox));
    }

    private IEnumerator SearchCityRoutine()
    {
        _LoadingPanel.gameObject.SetActive(true);
        string cityName = _SearchInputField.text.ToLower();
        yield return GameManager.Instance.OsmProcessor.SearchCities(cityName, (searchResults) =>
        {
            for (int i = 0; i < _SearchResultViewportContent.childCount; i++)
            {
                Destroy(_SearchResultViewportContent.GetChild(i).gameObject);
            }

            foreach (NominatimResult result in searchResults)
            {
                SearchResultEntry entry = Instantiate(_SearchResultEntryPrefab, _SearchResultViewportContent);
                TMPro.TextMeshProUGUI text = entry.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text)
                {
                    text.text = result.DisplayName;
                }
                if (entry.TryGetComponent(out Button button))
                {
                    button.onClick.AddListener(() => { StartGenerateCityRoutine(result.DisplayName, result.BoundingBox); });
                }
            }
        });
        _LoadingPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _TargetPosition, Time.deltaTime * _lerpSpeed);
    }
}
