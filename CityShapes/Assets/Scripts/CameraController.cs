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

    public void ResetPosition(Vector3 startPosition)
    {
        _StartPosition = startPosition;
        transform.position = startPosition;
    }

    private void Update()
    {
        _MousePosDelta = Input.mousePosition - _LastMousePos;

        if (!Blocked)
        {
#if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                MoveCamera(_MousePosDelta);
            }
            float zoom = Input.GetAxis("Mouse ScrollWheel");
            if (zoom != 0f)
            {
                Zoom(zoom * 100f);
            }
#else
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
        ZoomChanged?.Invoke(diff);
    }

    private void MoveCamera(Vector3 difference)
    {
        Vector3 newPosition = transform.position;
        newPosition -= difference * (_Camera.orthographicSize / 1000);
        newPosition.x = Mathf.Clamp(newPosition.x, _StartPosition.x + _MinCameraPosition.x, _StartPosition.x + _MaxCameraPosition.x);
        newPosition.y = Mathf.Clamp(newPosition.y, _StartPosition.y + _MinCameraPosition.y, _StartPosition.y + _MaxCameraPosition.y);

        transform.position = newPosition;
    }
}
