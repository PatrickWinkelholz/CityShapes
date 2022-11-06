using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PausePanel : MonoBehaviour
{
    public bool Paused => _Paused;
    private bool _Paused = true;

    public static PausePanel Instance;

    [SerializeField] private float _lerpSpeed = default;
    [SerializeField] private TMPro.TextMeshProUGUI _ScoreDisplay = null;
    [SerializeField] private TMPro.TextMeshProUGUI _HighScoreDisplay = null;
    [SerializeField] private TMPro.TextMeshProUGUI _ScoreTimeDisplay = null;
    [SerializeField] private TMPro.TextMeshProUGUI _HighScoreTimeDisplay = null;
    [SerializeField] private Button _DistrictsButton = null;
    [SerializeField] private Button _LandmarksButton = null;
    [SerializeField] private TMPro.TMP_InputField _SearchInputField = null;
    [SerializeField] private TMPro.TMP_InputField _UserNameInputField = null;
    [SerializeField] private TMPro.TMP_InputField _PasswordInputField = null;
    [SerializeField] private TMPro.TextMeshProUGUI _ErrorText = null;
    [SerializeField] private GameObject _CitySearchPanel = null;
    [SerializeField] private GameObject _MenuContentPanel = null;
    [SerializeField] private GameObject _LeaderboardPanel = null;
    [SerializeField] private GameObject _LoginPanel = null;
    [SerializeField] private SearchResultEntry _SearchResultEntryPrefab = default;
    [SerializeField] private GameObject _LeaderboardEntryPrefab = default;
    [SerializeField] private Transform _SearchResultViewportContent = default;
    [SerializeField] private Transform _LeaderboardViewportContent = default;
    [SerializeField] private Transform _LoadingPanel = default;
    [SerializeField] private TMPro.TextMeshProUGUI _LoadingText = default;
    [SerializeField] private Button _BackButton = default;
    [SerializeField] private TMPro.TextMeshProUGUI _TimeIndicator = default;
    [SerializeField] private GameObject _PauseButton = default;

    private Vector3 _NotPausedPosition = default;
    private Vector3 _PausedPosition = Vector3.zero;
    private Vector3 _TargetPosition = default;
    private GameObject _CurrentPanel = default;
    private int _HighScore = 0;
    private float _HighScoreTime = 0.0f;

    private void Awake()
    {
        Instance = this;
        _CurrentPanel = _LoginPanel;
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
        GameManager.Instance.OsmProcessor.StatusChangedEvent += OnOsmProcessorStatusChanged;
    }

    private void OnDisable()
    {
        GameManager.Instance.GameOverEvent -= OnGameOver;
        GameManager.Instance.GameRestartingEvent -= OnRestarting;
        GameManager.Instance.OsmProcessor.StatusChangedEvent -= OnOsmProcessorStatusChanged;
    }

    private void OnOsmProcessorStatusChanged(string status)
    {
        _LoadingText.text = status;
    }

    private void OnGameOver()
    {
        TrySetNewHighscore();
        TogglePause();
    }

    public void TogglePause()
    {
        _Paused = !_Paused;
        _PauseButton.SetActive(!_Paused);
        _TimeIndicator.gameObject.SetActive(!_Paused);
        UpdateScoreDisplay();
        GameManager.Instance.Camera.Blocked = _Paused;
        _TargetPosition = _Paused ? _PausedPosition : _NotPausedPosition;
    }

    private void ChangePanel(GameObject panel)
    {
        _CurrentPanel.SetActive(false);
        _CurrentPanel = panel;
        panel.SetActive(true);
        _ErrorText.text = "";
    }

    private void OnRestarting()
    {
        ChangePanel(_MenuContentPanel);
        _BackButton.gameObject.SetActive(true);
        TogglePause();
    }

    private void UpdateScoreDisplay()
    {
        _ScoreDisplay.text = GameManager.Instance.Score.ToString();
        _ScoreTimeDisplay.text = "(" + Utils.FormatTime(GameManager.Instance.Timer) + ")";
        if (_HighScore == 0)
        {
            _HighScoreDisplay.text = "-";
            _HighScoreTimeDisplay.text = "";
        }
        else
        {
            _HighScoreDisplay.text = _HighScore.ToString();
            _HighScoreTimeDisplay.text = "(" + Utils.FormatTime(_HighScoreTime) + ")";
        }
    }

    private IEnumerator SubmitScoreRoutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("submitScore", GameManager.Instance.Score);
        form.AddField("time", GameManager.Instance.Timer.ToString());
        form.AddField("cityName", GameManager.Instance.City.gameObject.name);
        form.AddField("userName", _UserNameInputField.text);
        form.AddField("password", _PasswordInputField.text);
        UnityWebRequest webRequest = UnityWebRequest.Post("https://patrickwinkelholz.com/leaderboard.php", form);

        yield return webRequest.SendWebRequest();

        if (!webRequest.isDone)
        {
            Debug.LogError("Not Done!");
            yield break;
        }

        if (webRequest.isNetworkError)
        {
            _ErrorText.text = "Network Error!";
            yield break;
        }

        if (webRequest.downloadHandler.text == "success")
        {
            yield return ResetUserData();
        }
        else
        {
            _ErrorText.text = webRequest.downloadHandler.text;
        }
    }

    private IEnumerator ResetUserData()
    {
        _HighScore = 0;
        _HighScoreTime = 0.0f;
        if (GameManager.Instance.City != null)
        {
            yield return ObtainLeaderboard();
        }
        UpdateScoreDisplay();
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

    public void OnLeaderboardPressed()
    {
        ChangePanel(_LeaderboardPanel);
    }

    public void OnLogoutPressed()
    {
        _UserNameInputField.text = "";
        _PasswordInputField.text = "";
        ChangePanel(_LoginPanel);
    }

    public void OnLoginPressed()
    {
        StartCoroutine(LoginRoutine(false));
    }

    public void OnRegisterPressed()
    {
        StartCoroutine(LoginRoutine(true));
    }

    private IEnumerator LoginRoutine(bool register)
    {
        if (_UserNameInputField.text == "")
        {
            _ErrorText.text = "please enter username!";
            yield break;
        }
        if (_PasswordInputField.text == "")
        {
            _ErrorText.text = "please enter password!";
            yield break;
        }
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[A-Za-z0-9]+");

        if (!regex.IsMatch(_UserNameInputField.text))
        {
            _ErrorText.text = "username contains invalid characters!";
            yield break;
        }

        if (!regex.IsMatch(_PasswordInputField.text))
        {
            _ErrorText.text = "password contains invalid characters!";
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField( register ? "register" : "login", 1);
        form.AddField("userName", _UserNameInputField.text);
        form.AddField("password", _PasswordInputField.text);
        UnityWebRequest webRequest = UnityWebRequest.Post("https://patrickwinkelholz.com/leaderboard.php", form);

        _LoadingPanel.gameObject.SetActive(true);
        _LoadingText.text = "contacting server...";
        yield return webRequest.SendWebRequest();
        yield return ResetUserData();

        _LoadingPanel.gameObject.SetActive(false);

        if (!webRequest.isDone)
        {
            Debug.LogError("Not Done!");
            yield break;
        }

        if (webRequest.isNetworkError)
        {
            _ErrorText.text = "Network Error!";
            yield break;
        }

        if (webRequest.downloadHandler.text == "success")
        {
            ChangePanel(_CitySearchPanel);
        }
        else
        {
            _ErrorText.text = webRequest.downloadHandler.text;
        }
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
        if (GameManager.Instance.Score > _HighScore ||
            (GameManager.Instance.Score == _HighScore && GameManager.Instance.Timer < _HighScoreTime))
        {
            _HighScore = GameManager.Instance.Score;
            _HighScoreTime = GameManager.Instance.Timer;
            StartCoroutine(SubmitScoreRoutine());
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
        yield return GameManager.Instance.ChangeCity(cityName, boundingBox, (cityChanged)=>
        { 
            if (!cityChanged)
            {
                _ErrorText.text = "Can't generate cityData for " + cityName;
            }
        });
        _LoadingPanel.gameObject.SetActive(false);
        yield return ResetUserData();
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

    private IEnumerator ObtainLeaderboard()
    {
        for (int i = 0; i < _LeaderboardViewportContent.childCount; i++)
        {
            Destroy(_LeaderboardViewportContent.GetChild(i).gameObject);
        }

        WWWForm form = new WWWForm();
        form.AddField("readLeaderboard", 1);
        form.AddField("cityName", GameManager.Instance.City.name);
        UnityWebRequest webRequest = UnityWebRequest.Post("https://patrickwinkelholz.com/leaderboard.php", form);

        yield return webRequest.SendWebRequest();

        if (!webRequest.isDone)
        {
            Debug.LogError("Not Done!");
            yield break;
        }

        if (webRequest.isNetworkError)
        {
            _ErrorText.text = "Network Error!";
            yield break;
        }

        string result = webRequest.downloadHandler.text;
        string[] entries = result.Split('\n');
        
        if (entries.Length > 0 && entries[0] == "results:")
        {
            if (entries.Length == 2)
            {
                GameObject entry = Instantiate(_LeaderboardEntryPrefab, _LeaderboardViewportContent);
                TMPro.TextMeshProUGUI text = entry.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                text.text = "no entries!";
            }
            foreach (string s in entries)
            {
                string[] values = s.Split('\t');
                if (values.Length == 3)
                {
                    GameObject entry = Instantiate(_LeaderboardEntryPrefab, _LeaderboardViewportContent);
                    TMPro.TextMeshProUGUI[] textElements = entry.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                    textElements[0].text = values[0];
                    textElements[1].text = values[1];
                    textElements[2].text = "(" + Utils.FormatTime(values[2]) + ")";

                    if (values[0] == _UserNameInputField.text)
                    {
                        _HighScore = int.Parse(values[1]);
                        _HighScoreTime = float.Parse(values[2]);
                    }
                }
            }
        }
        else
        {
            _ErrorText.text = result;
        }
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _TargetPosition, Time.deltaTime * _lerpSpeed);
        _TimeIndicator.text = Utils.FormatTime(GameManager.Instance.Timer);
        if (_Paused && !GameManager.Instance.GameOver)
        {
            UpdateScoreDisplay();
        }
    }
}
