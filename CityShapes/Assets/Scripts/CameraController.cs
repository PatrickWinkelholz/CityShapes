using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public delegate void FloatDelegate(float value);
    public event FloatDelegate ZoomChanged;

    public bool Blocked = true;

    [SerializeField] private Camera _Camera = null;
    [SerializeField] private float _ZoomSpeed = 10.0f;
    [SerializeField] private float _MaxCameraSize = 6.5f;
    [SerializeField] private float _MinCameraSize = 0.8f;
    [SerializeField] private Vector2 _MinCameraPosition = default;
    [SerializeField] private Vector2 _MaxCameraPosition = default;

    private Vector3 _MousePosDelta = Vector3.zero;
    private Vector3 _LastMousePos = Vector3.zero;

    private Vector3 _StartPosition = default;
    private float _StartSize = default;

    private void Start()
    {
        _LastMousePos = Input.mousePosition;
        _StartPosition = transform.position;
        _StartSize = _Camera.orthographicSize;
    }

    public void Reset(CityData cityData)
    {
        _MinCameraPosition = cityData.BackgroundTiles[0, 0].Pos;
        _MaxCameraPosition = cityData.BackgroundTiles[cityData.BackgroundTiles.GetLength(0) - 1, cityData.BackgroundTiles.GetLength(1) - 1].Pos;
        _StartPosition = new Vector3(cityData.Shape.Center.x, cityData.Shape.Center.y, -10);

        _MaxCameraSize = (cityData.BackgroundTiles.GetLength(1) - GameManager.Instance.OsmDataProcessor.NrExtraBackgroundTiles.x * 2) * 1.9f; //using random value to avoid maxSize getting too large
        _StartSize = _MaxCameraSize;
        _Camera.orthographicSize = _MaxCameraSize;

        _Camera.backgroundColor = new Color(0.9647059f, 0.8784314f, 0.8235294f);

        transform.position = _StartPosition;
    }

    private void Update()
    {
        _MousePosDelta = Input.mousePosition - _LastMousePos;

        if (!Blocked)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Input.touchCount == 1)
            {
                MoveCamera(Input.touches[0].deltaPosition);
            }
            else if (Input.touchCount == 2)
            {
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

                float difference = currentMagnitude - prevMagnitude;
                Zoom(difference);
            }
#else
            if (Input.GetMouseButton(0))
            {
                //TODO don't scale by artificial value here, find out what causes mouse to behave differently on webGL.
                MoveCamera(_MousePosDelta * 1.65f);
            }
            float zoom = Input.GetAxis("Mouse ScrollWheel");
            if (zoom != 0f)
            {
                Zoom(zoom * 100f);
            }
#endif
        }
        _LastMousePos = Input.mousePosition;
    }

    private void Zoom(float difference)
    {
        float diff = difference * _ZoomSpeed * (_Camera.orthographicSize / 1000);
        _Camera.orthographicSize -= diff;
        if (_Camera.orthographicSize < _MinCameraSize)
        {
            diff -= _MinCameraSize - _Camera.orthographicSize;
            _Camera.orthographicSize = _MinCameraSize;
        }
        if (_Camera.orthographicSize > _MaxCameraSize)
        {
            diff -= _MaxCameraSize - _Camera.orthographicSize;
            _Camera.orthographicSize = _MaxCameraSize;
        }
        MoveCamera(Vector3.zero);
        ZoomChanged?.Invoke(diff);
    }

    private void MoveCamera(Vector3 difference)
    {
        Vector3 newPosition = transform.position;
        newPosition -= difference * (_Camera.orthographicSize / 1000);
        float lowerXBound = _MinCameraPosition.x + _Camera.orthographicSize / 2.0f + 1.5f; //no idea why + 1.5f is necessary here but without it theres always a slim line on the left
        float upperXBound = _MaxCameraPosition.x - _Camera.orthographicSize / 2.0f;
        float lowerYBound = _MaxCameraPosition.y + _Camera.orthographicSize;
        float upperYBound = _MinCameraPosition.y - _Camera.orthographicSize;
        if (lowerXBound < upperXBound)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, lowerXBound, upperXBound);
        }
        else
        {
            newPosition.x = transform.position.x;
        }
        if (lowerYBound < upperYBound)
        {
            newPosition.y = Mathf.Clamp(newPosition.y, lowerYBound, upperYBound);
        }
        else
        {
            newPosition.y = transform.position.y;
        }

        transform.position = newPosition;
    }
}
