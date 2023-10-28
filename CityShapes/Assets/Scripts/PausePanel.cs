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
    [SerializeField] private UnityEngine.EventSystems.EventSystem _EventSystem = null;
    [SerializeField] private TMPro.TextMeshProUGUI _ScoreDisplay = null;
    [SerializeField] private TMPro.TextMeshProUGUI _ScoreDisplayLabel = null;
    [SerializeField] private TMPro.TextMeshProUGUI _ScoreTimeDisplay = null;
    [SerializeField] private TMPro.TextMeshProUGUI _HighScoreDisplay = null;
    [SerializeField] private TMPro.TextMeshProUGUI _HighScoreTimeDisplay = null;
    //[SerializeField] private Button _MapObjectsButton = null;
    //[SerializeField] private Button _LandmarksButton = null;
    [SerializeField] private TMPro.TMP_InputField _SearchInputField = null;
    [SerializeField] private TMPro.TMP_InputField _UserNameInputField = null;
    [SerializeField] private TMPro.TMP_InputField _PasswordInputField = null;
    [SerializeField] private TMPro.TextMeshProUGUI _ErrorText = null;
    [SerializeField] private GameObject _LogoImage = null;
    [SerializeField] private GameObject _CitySearchPanel = null;
    [SerializeField] private GameObject _MenuContentPanel = null;
    [SerializeField] private GameObject _LeaderboardPanel = null;
    [SerializeField] private GameObject _GameModePanel = null;
    [SerializeField] private GameObject _LoginPanel = null;
    [SerializeField] private GameObject _CreditsPanel = null;
    [SerializeField] private GameObject _SearchResultEntryPrefab = default;
    [SerializeField] private GameObject _LeaderboardEntryPrefab = default;
    [SerializeField] private Transform _SearchResultViewportContent = default;
    [SerializeField] private Transform _LeaderboardViewportContent = default;
    [SerializeField] private Transform _LoadingPanel = default;
    [SerializeField] private TMPro.TextMeshProUGUI _LoadingText = default;
    [SerializeField] private Button _CitySearchBackButton = default;
    [SerializeField] private Button _GameModeBackButton = default;
    [SerializeField] private TMPro.TextMeshProUGUI _TimeIndicator = default;
    [SerializeField] private GameObject _PauseButton = default;
    [SerializeField] private GameObject _CurrentElementLabel = default;
    [SerializeField] private TMPro.TextMeshProUGUI _LeaderboardLabel = default;
    [SerializeField] private ParticleSystem _ConfettiParticle = default;

    private Vector3 _NotPausedPosition = default;
    private Vector3 _PausedPosition = Vector3.zero;
    private Vector3 _TargetPosition = default;
    private GameObject _CurrentPanel = default;
    private int _PersonalBestScore = 0;
    private float _PersonalBestTime = 0.0f;   
    private Dictionary<ObjectType, string> _ModeAbbreviations = new Dictionary<ObjectType, string>()
    { 
        { ObjectType.District, "Dst" }, 
        { ObjectType.Road, "Rod" } 
    };
    private Dictionary<ObjectType, string> _ModeNames = new Dictionary<ObjectType, string>()
    {
        { ObjectType.District, "Districts" },
        { ObjectType.Road, "Roads" }
    };
    private bool _NewTopscore = false;
    private bool _Loading = false;

    private void Awake()
    {
        Instance = this;
        _CurrentPanel = _LoginPanel;
        _NotPausedPosition = new Vector3(0, -Screen.height, 0);

        _UserNameInputField.onSubmit.AddListener(str => 
        { 
            _PasswordInputField.Select(); 
        });
        _PasswordInputField.onSubmit.AddListener(str => 
        { 
            OnLoginPressed(); 
        });
        _SearchInputField.onSubmit.AddListener(str =>
        {
            OnSearchPressed();
        });
    }

    private void Start()
    {
        UpdateScoreDisplay();
    }

    private void OnEnable()
    {
        GameManager.Instance.GameOverEvent += OnGameOver;
        GameManager.Instance.GameRestartingEvent += OnRestarting;
        GameManager.Instance.OsmDataProcessor.StatusChangedEvent += OnOsmProcessorStatusChanged;
    }

    private void OnDisable()
    {
        GameManager.Instance.GameOverEvent -= OnGameOver;
        GameManager.Instance.GameRestartingEvent -= OnRestarting;
        GameManager.Instance.OsmDataProcessor.StatusChangedEvent -= OnOsmProcessorStatusChanged;
    }

    private void OnOsmProcessorStatusChanged(string status)
    {
        _LoadingText.text = status;
    }

    private void OnGameOver(bool submitScore)
    {
        if (submitScore)
        {
            TrySetNewHighscore();
        }
    }

    public void TogglePause()
    {
        if (GameManager.Instance.City == null)
        {
            return;
        }

        _Paused = !_Paused;
        if (_EventSystem.TryGetComponent(out TabNavigation tabNav))
        {
            tabNav.enabled = _Paused && !_Loading;
        }
        _PauseButton.SetActive(!_Paused);
        _CurrentElementLabel.SetActive(!_Paused);
        _TimeIndicator.gameObject.SetActive(!_Paused);
        if (_CurrentPanel != _LoginPanel)
        {
            ChangePanel(_MenuContentPanel);
        }
        GameManager.Instance.Camera.Blocked = _Paused;
        _TargetPosition = _Paused ? _PausedPosition : _NotPausedPosition;
    }

    private void ChangePanel(GameObject panel)
    {
        _CurrentPanel.SetActive(false);
        _CurrentPanel = panel;
        panel.SetActive(true);
        _ErrorText.text = "";
        _EventSystem.SetSelectedGameObject(null);
    }

    private void OnRestarting()
    {
        ChangePanel(_MenuContentPanel);
        _CitySearchBackButton.onClick.RemoveAllListeners();
        _CitySearchBackButton.onClick.AddListener(()=> 
        {
            OnBackPressed();
        });
        _GameModeBackButton.gameObject.SetActive(true);
        if (_NewTopscore)
        {
            ToggleNewTopscore(false, false);
        }
        TogglePause();
    }

    private void UpdateScoreDisplay()
    {
        if (_NewTopscore)
        {
            Color.RGBToHSV(_ScoreDisplayLabel.colorGradient.topLeft, out float h, out float s, out float v);
            h += Time.deltaTime * 0.4f;
            if (h > 1.0f) h -= 1.0f;
            float h2 = h + 0.25f;
            if (h2 > 1.0f) h2 -= 1.0f;
            float h3 = h + 0.5f;
            if (h3 > 1.0f) h3 -= 1.0f;
            float h4 = h + 0.75f;
            if (h4 > 1.0f) h4 -= 1.0f;

            _ScoreDisplayLabel.colorGradient =
            new TMPro.VertexGradient(
                Color.HSVToRGB(h, s, v),
                Color.HSVToRGB(h2, s, v),
                Color.HSVToRGB(h4, s, v),
                Color.HSVToRGB(h3, s, v)
                );
        }

        if (_Paused)
        {
            _ScoreDisplay.text = GameManager.Instance.Score.ToString();
            _ScoreTimeDisplay.text = "(" + Utils.FormatTime(GameManager.Instance.Timer) + ")";

            if (_PersonalBestScore == 0)
            {
                _HighScoreDisplay.text = "-";
                _HighScoreTimeDisplay.text = "";
            }
            else
            {
                _HighScoreDisplay.text = _PersonalBestScore.ToString();
                _HighScoreTimeDisplay.text = "(" + Utils.FormatTime(_PersonalBestTime) + ")";
            }
        }
    }

    private void ToggleNewTopscore(bool newPersonalBest, bool newHighscore)
    {
        if (newPersonalBest || newHighscore)
        {
            _NewTopscore = true;
            _ConfettiParticle.Play();
            _HighScoreDisplay.transform.parent.parent.gameObject.SetActive(false);
            _ScoreDisplayLabel.text = newHighscore ? "NEW HIGHSCORE!" : "NEW PERSONAL BEST!";
            _ScoreDisplayLabel.fontSize = 14;
            _ScoreDisplayLabel.fontStyle = TMPro.FontStyles.Bold;
            _ScoreDisplayLabel.enableVertexGradient = true;
        }
        else
        {
            _NewTopscore = false;
            _ConfettiParticle.Stop();
            _HighScoreDisplay.transform.parent.parent.gameObject.SetActive(true);
            _ScoreDisplayLabel.text = "your score:";
            _ScoreDisplayLabel.fontSize = 10;
            _ScoreDisplayLabel.fontStyle = TMPro.FontStyles.Normal;
            _ScoreDisplayLabel.enableVertexGradient = false;
            _ScoreDisplayLabel.color = Color.white;
        }
    }

    private IEnumerator SubmitScoreRoutine()
    {
        SetLoading(true);
        _LoadingText.text = "submitting score...";

        WWWForm form = new WWWForm();
        form.AddField("submitScore", GameManager.Instance.Score);
        form.AddField("time", GameManager.Instance.Timer.ToString());
        form.AddField("cityName", GameManager.Instance.City.gameObject.name);
        form.AddField("mode", _ModeAbbreviations[GameManager.Instance.MapObjectType]);
        form.AddField("userName", _UserNameInputField.text);
        form.AddField("password", _PasswordInputField.text);

        yield return Utils.SendWebRequest(WebDependancies.Leaderboard, form, result => 
        {
            if (result == "success")
            {
                StartCoroutine(ObtainLeaderboard(true, ()=> { if (!_Paused) TogglePause(); }));
            }
            else
            {
                _ErrorText.text = result;
                SetLoading(false);
            }
        });
    }

    //public void OnMapObjectsPressed()
    //{
    //    _LandmarksButton.interactable = true;
    //    _MapObjectsButton.interactable = false;

    //    UpdateScoreDisplay();
    //}

    //public void OnLandmarksPressed()
    //{
    //    _LandmarksButton.interactable = false;
    //    _MapObjectsButton.interactable = true;

    //    UpdateScoreDisplay();
    //}

    public void OnLeaderboardPressed()
    {
        ChangePanel(_LeaderboardPanel);
    }

    public void OnLogoutPressed()
    {
        _UserNameInputField.text = "";
        _PasswordInputField.text = "";
        _PersonalBestScore = 0;
        _PersonalBestTime = 0;
        if (_NewTopscore)
        {
            ToggleNewTopscore(false, false);
        }
        GameManager.Instance.EndGame(false);
        ChangePanel(_LoginPanel);
        _LogoImage.SetActive(true);
    }

    public void OnLoginPressed(bool guest = false)
    {
        _ErrorText.text = "";
        StartCoroutine(LoginRoutine(false, guest));
    }

    public void OnRegisterPressed()
    {
        _ErrorText.text = "";
        StartCoroutine(LoginRoutine(true));
    }

    private IEnumerator LoginRoutine(bool register, bool guest = false)
    {
        string username = guest ? "Guest" : _UserNameInputField.text;
        string password = guest ? "28394650394760" : _PasswordInputField.text;

        if (username == "")
        {
            _ErrorText.text = "please enter username!";
            yield break;
        }
        if (password == "")
        {
            _ErrorText.text = "please enter password!";
            yield break;
        }
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[A-Za-z0-9]+");

        if (!regex.IsMatch(username))
        {
            _ErrorText.text = "username contains invalid characters!";
            yield break;
        }

        if (!regex.IsMatch(password))
        {
            _ErrorText.text = "password contains invalid characters!";
            yield break;
        }

        SetLoading(true);
        _LoadingText.text = "contacting server...";

        WWWForm form = new WWWForm();
        form.AddField( register ? "register" : "login", 1);
        form.AddField("userName", username);
        form.AddField("password", password);

        yield return Utils.SendWebRequest(WebDependancies.Leaderboard, form, result => 
        {
            SetLoading(false);

            //ChangePanel(_CitySearchPanel);
            if (result == "success")
            {
                _LogoImage.SetActive(false);
                if (GameManager.Instance.City != null)
                {
                    ChangePanel(_MenuContentPanel);
                    StartCoroutine(ObtainLeaderboard());
                }
                else
                {
                    ChangePanel(_GameModePanel);
                }
            }
            else
            {
                _ErrorText.text = result;
            }
        });
    }

    public void OnBackPressed()
    {
        ChangePanel(_MenuContentPanel);
    }

    public void OnChangeCityPressed()
    {
        ChangePanel(_CitySearchPanel);
    }

    public void OnCreditsPressed()
    {
        ChangePanel(_CreditsPanel);
    }

    public void TrySetNewHighscore()
    {
        int score = GameManager.Instance.Score;
        float time = GameManager.Instance.Timer;
        if (score > _PersonalBestScore || (score == _PersonalBestScore && time < _PersonalBestTime))
        {
            StartCoroutine(SubmitScoreRoutine());
        }
        else
        {
            if (!_Paused)
            {
                TogglePause();
            }
        }
    }

    public void OnWebsitePressed()
    {
        Application.OpenURL(WebDependancies.Website);
    }

    public void OnContactPressed()
    {
        Application.OpenURL(WebDependancies.MailContact);
    }

    public void OnRestartPressed()
    {
        GameManager.Instance.RestartGame();
    }

    public void OnSearchPressed()
    {
        _ErrorText.text = "";
        StartCoroutine(SearchCityRoutine());
    }

    private IEnumerator GenerateCityRoutine(string cityName, string boundingBox)
    {
        SetLoading(true);
        yield return GameManager.Instance.ChangeCity(cityName, boundingBox, result=>
        { 
            if (result != "success")
            {
                _ErrorText.text = result;
            }
        });
        yield return ObtainLeaderboard();
        SetLoading(false);
    }

    private void StartGenerateCityRoutine(string cityName, string boundingBox)
    {
        StartCoroutine(GenerateCityRoutine(cityName, boundingBox));
    }

    public void OnGameModePressed(int mode)
    {
        SetLoading(true);
        StartCoroutine(GameManager.Instance.ChangeMode((ObjectType)mode, result =>
        {
            if (result != "success")
            {
                _ErrorText.text = result;
                return;
            }
            SetLoading(false);
            
            if (GameManager.Instance.City != null)
            {
                StartCoroutine(ObtainLeaderboard());
            }
            else
            {
                ChangePanel(_CitySearchPanel);
            }
        }));
    }

    public void OnChangeGameModePressed()
    {
        ChangePanel(_GameModePanel);
    }

    private IEnumerator SearchCityRoutine()
    {
        SetLoading(true);
        string cityName = _SearchInputField.text.ToLower();
        yield return GameManager.Instance.OsmDataProcessor.SearchCities(cityName, (callbackResult, searchResults) =>
        {
            if (callbackResult != "success")
            {
                _ErrorText.text = callbackResult;
                return;
            }

            for (int i = 0; i < _SearchResultViewportContent.childCount; i++)
            {
                Destroy(_SearchResultViewportContent.GetChild(i).gameObject);
            }

            foreach (NominatimResult result in searchResults)
            {
                GameObject entry = Instantiate(_SearchResultEntryPrefab, _SearchResultViewportContent);
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
        SetLoading(false);
    }

    private IEnumerator ObtainLeaderboard(bool newPersonalBest = false, System.Action callback = default)
    {
        for (int i = 0; i < _LeaderboardViewportContent.childCount; i++)
        {
            Destroy(_LeaderboardViewportContent.GetChild(i).gameObject);
        }

        if (GameManager.Instance.City == null)
        {
            callback?.Invoke();
            yield break;
        }

        SetLoading(true);
        _LoadingText.text = "obtaining leaderboard...";
        _LeaderboardLabel.text = GameManager.Instance.City.name.Split(',')[0] + " - " + _ModeNames[GameManager.Instance.MapObjectType];
        WWWForm form = new WWWForm();
        form.AddField("readLeaderboard", 1);
        form.AddField("cityName", GameManager.Instance.City.name);
        form.AddField("mode", _ModeAbbreviations[GameManager.Instance.MapObjectType]);
        yield return Utils.SendWebRequest(WebDependancies.Leaderboard, form, result =>
        {
            string[] entries = result.Split('\n');
            bool newHighscore = false;
            if (entries.Length > 0 && entries[0] == "results:")
            {
                if (!newPersonalBest)
                {
                    _PersonalBestScore = 0;
                    _PersonalBestTime = 0;
                }

                if (entries.Length == 2)
                {
                    GameObject entry = Instantiate(_LeaderboardEntryPrefab, _LeaderboardViewportContent);
                    TMPro.TextMeshProUGUI text = entry.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    text.text = "no entries!";
                }
                bool processedHighscore = false;
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

                        if ( Utils.TryParse(values[1], out int score)
                            && Utils.TryParse(values[2], out float time)
                            && values[0] == _UserNameInputField.text)
                        {
                            if (newPersonalBest && !processedHighscore)
                            {
                                newHighscore = true;
                            }
                            _PersonalBestScore = score;
                            _PersonalBestTime = time;
                        }
                        processedHighscore = true;
                    }
                }
            }
            else
            {
                _ErrorText.text = result;
            }
            ToggleNewTopscore(newPersonalBest, newHighscore);
            SetLoading(false);
            callback?.Invoke();
        });
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _TargetPosition, Time.deltaTime * _lerpSpeed);
        _TimeIndicator.text = Utils.FormatTime(GameManager.Instance.Timer);
        UpdateScoreDisplay();
    }

    private void SetLoading(bool value)
    {
        _Loading = value;
        _LoadingPanel.gameObject.SetActive(value);
        if (_EventSystem.TryGetComponent(out TabNavigation tabNav))
        {
            tabNav.enabled = !value && _Paused;
        }
    }
}
