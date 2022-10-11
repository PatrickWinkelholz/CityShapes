using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    private bool _BonnSelected = true;

    public bool Paused => _Paused;
    private bool _Paused = false;

    public static PausePanel Instance;

    [SerializeField] private float _lerpSpeed = default;
    [SerializeField] private TMPro.TextMeshProUGUI _ScoreDisplay = null;
    [SerializeField] private TMPro.TextMeshProUGUI _HighScoreDisplay = null;
    [SerializeField] private Button _DistrictsButton = null;
    [SerializeField] private Button _LandmarksButton = null;
    [SerializeField] private TMPro.TMP_InputField _SearchInputField = null;
    [SerializeField] private GameObject _CitySearchPanel = null;
    [SerializeField] private GameObject _MenuContentPanel = null;

    private Vector3 _NotPausedPosition = default;
    private Vector3 _PausedPosition = Vector3.zero;
    private Vector3 _TargetPosition = default;

    private void Awake()
    {
        Instance = this;
        _NotPausedPosition = new Vector3(0, -Screen.height, 0);
        _TargetPosition = _NotPausedPosition;
        transform.position = _NotPausedPosition;
    }

    private void Start()
    {
        UpdateScoreDisplay();
    }

    private void OnEnable()
    {
        GameManager.Instance.GameOverEvent += OnGameOver;
    }

    private void OnDisable()
    {
        GameManager.Instance.GameOverEvent -= OnGameOver;
    }

    private void OnGameOver()
    {
        UpdateHighscore();
        TogglePause();
    }

    public void TogglePause()
    {
        UpdateScoreDisplay();
        _Paused = !_Paused;
        GameManager.Instance.Camera.Blocked = _Paused;
        _TargetPosition = _Paused ? _PausedPosition : _NotPausedPosition;
    }

    private void UpdateScoreDisplay()
    {
        //_ScoreDisplay.text = _BonnSelected == GameManager.Instance.PlayingBonn ? GameManager.Instance.Score.ToString() : "-";

        string key = _BonnSelected ? "Bonn" : "Leipzig";
        if (PlayerPrefs.HasKey(key))
        {
            _HighScoreDisplay.text = PlayerPrefs.GetInt(key).ToString();
        }
        else
        {
            _HighScoreDisplay.text = "-";
        }
    }

    public void OnDistrictsPressed()
    {
        _BonnSelected = true;

        _LandmarksButton.interactable = true;
        _DistrictsButton.interactable = false;

        UpdateScoreDisplay();
    }

    public void OnLandmarksPressed()
    {
        _BonnSelected = false;

        _LandmarksButton.interactable = false;
        _DistrictsButton.interactable = true;

        UpdateScoreDisplay();
    }

    public void OnBackPressed()
    {
        _MenuContentPanel.SetActive(true);
        _CitySearchPanel.SetActive(false);
    }

    public void OnChangeCityPressed()
    {
        _MenuContentPanel.SetActive(false);
        _CitySearchPanel.SetActive(true);
    }

    public void UpdateHighscore()
    {
        //string key = GameManager.Instance.PlayingBonn ? "Bonn" : "Leipzig"; //GameManager.Instance.GameMode.GameModeName;
        //if (PlayerPrefs.HasKey(key))
        //{
        //    int highscore = PlayerPrefs.GetInt(key);
        //    if (GameManager.Instance.Score > highscore)
        //    {
        //        PlayerPrefs.SetInt(key, GameManager.Instance.Score);
        //    }
        //}
        //else
        //{
        //    PlayerPrefs.SetInt(key, GameManager.Instance.Score);
        //}
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
        //GameManager.Instance.PlayingBonn = _BonnSelected;
        GameManager.Instance.RestartGame();
        TogglePause();
    }

    public void OnSearchPressed()
    {
        GameManager.Instance.SearchCity(_SearchInputField.text);
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _TargetPosition, Time.deltaTime * _lerpSpeed);
    }
}
