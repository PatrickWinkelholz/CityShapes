using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum ObjectType
{
    District = 0,
    Road = 1
}

public class MapObject : MonoBehaviour
{
    public int CityDataIndex = 0;

    public Vector2 CenterPoint { get; private set; }
    public string MapObjectName { get; private set; }
    public bool Locked { get; set; }
    public ObjectType Type => _Type;
    [SerializeField] private ObjectType _Type;
    [SerializeField] private MeshFilter _MeshFilter = default;
    [SerializeField] private LineRenderer _OutlineRenderer = default;

    [SerializeField] private Color _InitialColor = default;
    [SerializeField] private Color _InitialOutlineColor = default;

    [SerializeField] private Color _PressedColor = default;
    [SerializeField] private Color _PressedOutlineColor = default;

    [SerializeField] private Color _CorrectColor = default;
    [SerializeField] private Color _CorrectOutlineColor = default;

    [SerializeField] private Color _WrongColor = default;
    [SerializeField] private Color _WrongOutlineColor = default;

    [Header("Road specific components")]
    [SerializeField] private LineRenderer _LineRenderer = default;
    [SerializeField] private MeshCollider _MeshCollider = default;

    [Header("District specific components")]
    [SerializeField] private MeshRenderer _MeshRenderer = default;
    [SerializeField] private PolygonCollider2D _PolygonCollider = default;

    private bool _ClickStarted = false;
    private Vector2 _ClickedPosition = Vector2.zero;

    public void Initialize(MapObjectData mapObjectData)
    {
        MapObjectName = mapObjectData.Name;
        gameObject.name = MapObjectName;

        List<Vector3> shapePoints = new List<Vector3>();
        foreach (Vector2 point in mapObjectData.Shape.Points)
        {
            shapePoints.Add(point);
        }
        CenterPoint = mapObjectData.Shape.Center;
        _OutlineRenderer.positionCount = shapePoints.Count;
        _OutlineRenderer.SetPositions(shapePoints.ToArray());

        if (Type == ObjectType.Road)
        {
            _LineRenderer.positionCount = shapePoints.Count;
            _LineRenderer.SetPositions(shapePoints.ToArray());

            _MeshFilter.mesh = new Mesh();
            _OutlineRenderer.BakeMesh(_MeshFilter.mesh);
            _MeshCollider.sharedMesh = _MeshFilter.mesh;
        }
        else
        {
            _PolygonCollider.points = mapObjectData.Shape.Points.ToArray();
            _MeshFilter.mesh = _PolygonCollider.CreateMesh(false, false);
        }

        SetColors(_InitialColor, _InitialOutlineColor);
    }

    private void OnMouseUpAsButton()
    {
        if (!_ClickStarted || Locked)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(0)) 
        {
            return;
        }
#else
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
#endif
        CancelClick();
        GameManager.Instance.MapObjectPressed(this);
    }

    private void OnMouseDown()
    {
        if (Locked || PausePanel.Instance.Paused || GameManager.Instance.GameOver)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount != 1 || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(0)) 
        {
            return;
        }
        _ClickedPosition = Input.GetTouch(0).position; 
#else
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        _ClickedPosition = Input.mousePosition;
#endif
        _ClickStarted = true;
        SetColors(_PressedColor, _PressedOutlineColor);
    }

    private void Update()
    {
        if (_ClickStarted && !Locked)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Input.touchCount != 1 || (Input.touches[0].position - _ClickedPosition).sqrMagnitude > 30f)
            {
                CancelClick();
            }         
#else
            if (((Vector2)Input.mousePosition - _ClickedPosition).sqrMagnitude > 30f)
            {
                CancelClick();
            }
#endif
        }
    }

    private void CancelClick()
    {
        _ClickStarted = false;
        SetColors(_InitialColor, _InitialOutlineColor);
    }

    public void Lock(bool correct)
    {
        Locked = true;
        if (correct)
        {
            SetColors(_CorrectColor, _CorrectOutlineColor);
            transform.position += new Vector3(0, 0, -0.6f);
        }
        else
        {
            SetColors(_WrongColor, _WrongOutlineColor);
            transform.position += new Vector3(0, 0, -0.3f);
        }
    }

    public void Reset()
    {
        Locked = false;
        Vector3 pos = transform.position;
        pos.z = 0;
        transform.position = pos;
        SetColors(_InitialColor, _InitialOutlineColor);
    }

    private void SetColors(Color color, Color outlineColor)
    {
        if (_MeshRenderer)
        {
            _MeshRenderer.material.color = color;
        }
        else if (_LineRenderer)
        {
            _LineRenderer.startColor = color;
            _LineRenderer.endColor = color;
        }
        if (_OutlineRenderer)
        {
            _OutlineRenderer.startColor = outlineColor;
            _OutlineRenderer.endColor = outlineColor;
        }
    }
}
