using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    public bool Paused => _Paused;
    private bool _Paused = false;

    public static PausePanel Instance;

    [SerializeField] private float _lerpSpeed = default;
    [SerializeField] private TMPro.TextMeshProUGUI _ScoreDisplay = null;
    [SerializeField] private TMPro.TextMeshProUGUI _HighScoreDisplay = null;
    [SerializeField] private Button _DistrictsButton = null;
    [SerializeField] private Button _LandmarksButton = null;

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
        UpdateHighscore();
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
        _ScoreDisplay.text = GameManager.Instance.Score.ToString();
        _Paused = !_Paused;
        GameManager.Instance.Camera.Blocked = _Paused;
        _TargetPosition = _Paused ? _PausedPosition : _NotPausedPosition;
    }

    public void OnDistrictsPressed()
    {
        _LandmarksButton.interactable = true;
        _DistrictsButton.interactable = false;
    }

    public void OnLandmarksPressed()
    {
        _LandmarksButton.interactable = false;
        _DistrictsButton.interactable = true;
    }

    public void UpdateHighscore()
    {
        string key = GameManager.Instance.GameMode.GameModeName;
        if (PlayerPrefs.HasKey(key))
        {
            int highscore = PlayerPrefs.GetInt(key);
            if (GameManager.Instance.Score > highscore)
            {
                PlayerPrefs.SetInt(key, GameManager.Instance.Score);
            }
        }
        else
        {
            PlayerPrefs.SetInt(key, GameManager.Instance.Score);
        }

        if (PlayerPrefs.HasKey(key))
        {
            _HighScoreDisplay.text = PlayerPrefs.GetInt(key).ToString();
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
        TogglePause();
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _TargetPosition, Time.deltaTime * _lerpSpeed);
    }
}
