using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistrictGameMode : GameMode
{
    //[SerializeField] private UnityEngine.UI.InputField _NameInputField = default;
    //[SerializeField] private UnityEngine.UI.InputField _RegionInputField = default;

    private District _CurrentDistrict;

    //private bool EnterDistrictNamesMode = false;
    private City _City => GameManager.Instance.City;

    public override void DistrictPressed(District district)
    {
        //if (EnterDistrictNamesMode)
        //{
        //    CityData.DistrictData districtData = _City.CityData.Districts[district.CityDataIndex];
        //    districtData.Name = _NameInputField.text;
        //    districtData.Region = _RegionInputField.text;
        //    _City.CityData.Districts[district.CityDataIndex] = districtData;
        //    district.SetColor(Color.green);
        //    district.Locked = true;
        //    return;
        //}

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
