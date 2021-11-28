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

    public GameMode GameMode { get => _GameMode; }
    [SerializeField] private GameMode _GameMode = null;
    public CameraController Camera { get => _Camera; }
    [SerializeField] private CameraController _Camera = null;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RestartGame();
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

    public void EndGame()
    {
        GameOverEvent?.Invoke();
    }
}
