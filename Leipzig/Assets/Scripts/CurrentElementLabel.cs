using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentElementLabel : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI _Text = default;
    [SerializeField] private UnityEngine.UI.Image _Image = default;

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
        _Image.enabled = element != "";
    }

    private void OnGameOver()
    {
        _Text.text = "You solved " + GameManager.Instance.Score.ToString() + " out of " + GameManager.Instance.City.Districts.Count;
    }
}
