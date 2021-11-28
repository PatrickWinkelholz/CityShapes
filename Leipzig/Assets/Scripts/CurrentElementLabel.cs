using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentElementLabel : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI _Text = default;
    [SerializeField] private City _City = default;

    private void OnEnable()
    {
        GameManager.Instance.GameMode.ElementChangedEvent += OnElementChanged;
        GameManager.Instance.GameOverEvent += OnGameOver;
    }

    private void OnDisable()
    {
        GameManager.Instance.GameMode.ElementChangedEvent -= OnElementChanged;
        GameManager.Instance.GameOverEvent -= OnGameOver;
    }

    private void OnElementChanged(object sender, string element)
    {
        _Text.text = element;
    }

    private void OnGameOver()
    {
        _Text.text = "You solved " + GameManager.Instance.Score.ToString() + " out of " + _City.Districts.Count;
    }
}
