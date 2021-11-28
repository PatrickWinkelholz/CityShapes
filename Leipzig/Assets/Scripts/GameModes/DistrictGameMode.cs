using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistrictGameMode : GameMode
{
    [SerializeField] private City _City = default;

    private District _CurrentDistrict;

    public override void DistrictPressed(District district)
    {
        if (_CurrentDistrict == district)
        {
            district.SetColor(Color.green);
            district.Locked = true;
            GameManager.Instance.Score++;
        }
        else
        {
            _CurrentDistrict.SetColor(Color.red);
            _CurrentDistrict.Locked = true;
        }
        NextDistrict();
    }

    private void OnEnable()
    {
        GameManager.Instance.GameRestartingEvent += OnGameRestarting;
    }

    private void OnDisable()
    {
        GameManager.Instance.GameRestartingEvent -= OnGameRestarting;
    }

    private void OnGameRestarting()
    {
        _City.Districts.Reset();
        foreach (District district in _City.Districts)
        {
            district.Reset();
        }

        NextDistrict();
    }

    private void NextDistrict()
    {
        if (_City.Districts.Empty())
        {
            GameManager.Instance.EndGame();
        }
        else
        {
            _CurrentDistrict = _City.Districts.NextRandom();
            InvokeElementChangedEvent(_CurrentDistrict.DistrictName);
        }
    }
}
