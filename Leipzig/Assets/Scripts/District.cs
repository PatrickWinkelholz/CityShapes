using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class District : MonoBehaviour
{
    public int CityDataIndex = 0;

    public Vector2 CenterPoint { get; private set; }
    public string DistrictName { get; private set; }
    public bool Locked { get; set; }

    [SerializeField] private CityColorScheme _CityColorScheme = default;
    [SerializeField] private PolygonCollider2D _Collider = default;
    [SerializeField] private MeshFilter _MeshFilter = default;
    [SerializeField] private MeshRenderer _MeshRenderer = default;
    [SerializeField] private LineRenderer _LineRenderer = default;

    private Color _InitialColor = default;
    private bool _ClickStarted = false;
    private Vector2 _ClickedPosition = Vector2.zero;

    public void TestInit(Vector2[] shape)
    {
        _Collider.points = shape;
        _MeshFilter.mesh = _Collider.CreateMesh(false, false);

        //CityColorScheme.ColorEntry colorEntry = _CityColorScheme.RegionColors.Find(x => x.Region.Equals(districtData.Region));
        _InitialColor = Color.blue;//colorEntry.Color;
        _MeshRenderer.material.color = _InitialColor;

        //outline + calculate center
        List<Vector3> linePositions = new List<Vector3>();
        foreach (Vector2 point in shape)
        {
            linePositions.Add(point);
        }
        _LineRenderer.positionCount = linePositions.Count;
        _LineRenderer.SetPositions(linePositions.ToArray());
        _LineRenderer.startWidth = 0.01f;
        _LineRenderer.endWidth = 0.001f;
        _LineRenderer.startColor = Color.white;
        _LineRenderer.endColor = Color.black;
    }


    public void Initialize(CityData.DistrictData districtData)
    {
        DistrictName = districtData.Name;
        Vector2[] shapeArray = districtData.Shape.ToArray();
        _Collider.points = shapeArray;
        _MeshFilter.mesh = _Collider.CreateMesh(false, false);

        CityColorScheme.ColorEntry colorEntry = _CityColorScheme.RegionColors.Find(x => x.Region.Equals(districtData.Region));
        _InitialColor = colorEntry.Color;
        _MeshRenderer.material.color = _InitialColor;

        //outline + calculate center
        List<Vector3> linePositions = new List<Vector3>();
        CenterPoint = Vector2.zero;
        foreach (Vector2 point in districtData.Shape)
        {
            linePositions.Add(point);
            CenterPoint += point;
        }
        CenterPoint /= districtData.Shape.Count;
        _LineRenderer.positionCount = linePositions.Count;
        _LineRenderer.SetPositions(linePositions.ToArray());
    }

    private void OnMouseUpAsButton()
    {
        if (!_ClickStarted || Locked)
        {
            return;
        }

#if UNITY_EDITOR
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
#else
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(0)) 
        {
            return;
        }
#endif
        CancelClick();
        GameManager.Instance.DistrictPressed(this);
    }

    private void OnMouseDown()
    {
        if (Locked || PausePanel.Instance.Paused)
        {
            return;
        }

#if UNITY_EDITOR
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        _ClickedPosition = Input.mousePosition;
#else
        if (Input.touchCount != 1 || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(0)) 
        {
            return;
        }
        _ClickedPosition = Input.GetTouch(0).position;
#endif
        _ClickStarted = true;
        _MeshRenderer.material.color = Color.white;        
    }

    private void Update()
    {
        if (_ClickStarted && !Locked)
        {
#if UNITY_EDITOR
            if (((Vector2)Input.mousePosition - _ClickedPosition).sqrMagnitude > 30f)
            {
                CancelClick();
            }
#else
            if (Input.touchCount != 1 || (Input.touches[0].position - _ClickedPosition).sqrMagnitude > 30f)
            {
                CancelClick();
            }
#endif
        }
    }

    private void CancelClick()
    {
        _ClickStarted = false;
        _MeshRenderer.material.color = _InitialColor;
    }

    public void SetColor(Color color) 
    {
        _MeshRenderer.material.color = color;
    }

    public void Reset()
    {
        Locked = false;
        SetColor(_InitialColor);
    }
}
