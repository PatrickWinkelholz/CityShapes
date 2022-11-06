using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pin : MonoBehaviour
{
    [SerializeField] private float _RotationSpeed = 10f;
    [SerializeField] private float _LerpSpeed = 10f;
    [SerializeField] private Transform _MeshRotationHandle = default;
    [SerializeField] private Transform _MeshTiltHandle = default;
    [SerializeField] private Transform _MeshTiltAnchor = default;
    [SerializeField] private Transform _MeshNoTiltAnchor = default;
    [SerializeField] private Transform _Mesh = default;
    [SerializeField] private Transform _DragAnchor = default;
    [SerializeField] private Transform _NoDragAnchor = default;

    private bool _Dragged = false;
    private Vector3 _MouseOffset = default;
    private Camera _Camera = default;

    private void Start()
    {
        _Camera = Camera.main;
    }

    private void OnEnable()
    {
        GameManager.Instance.Camera.ZoomChanged += OnZoomChanged;
    }

    private void OnDisable()
    {
        GameManager.Instance.Camera.ZoomChanged -= OnZoomChanged;
    }

    private void OnZoomChanged(float zoom)
    {
        transform.parent.localScale = transform.parent.localScale - new Vector3(zoom, zoom, zoom) * 0.2f;
    }

    private void Update()
    {
        if (_Dragged)
        {
            Vector3 pos = _Camera.ScreenToWorldPoint(Input.mousePosition) - _MouseOffset;
            pos.z = transform.position.z;
            transform.position = pos;
        }
        else
        {
            _MeshRotationHandle.Rotate(Vector3.up, Time.deltaTime * _RotationSpeed, Space.Self);
        }
        LerpTransform(_MeshTiltHandle, _Dragged ? _MeshNoTiltAnchor : _MeshTiltAnchor, _LerpSpeed);
        LerpTransform(_Mesh, _Dragged ? _DragAnchor : _NoDragAnchor, _LerpSpeed);
    }

    private void OnMouseDown()
    {
        GameManager.Instance.Camera.Blocked = true;
        _MouseOffset = _Camera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        _Dragged = true;
    }

    private void OnMouseUp()
    {
        GameManager.Instance.Camera.Blocked = false;
        _Dragged = false;
    }

    private void LerpTransform(Transform a, Transform b, float speed)
    {
        a.position = Vector3.Lerp(a.position, b.position, speed * Time.deltaTime);
        a.rotation = Quaternion.Lerp(a.rotation, b.rotation, speed * Time.deltaTime);
    }
}
