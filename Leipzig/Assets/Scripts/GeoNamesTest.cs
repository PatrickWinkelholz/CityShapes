using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Xml;

struct RelationData
{
    public string Name;
    public Vector2 Center;
    public List<List<Vector2>> Ways;
}

struct RelationReference
{
    public long CenterId;
    public long NameId;
    public List<long> WayIds;
}

//struct Way
//{
//    public ulong Id;
//    public List<Node> Nodes;
//}

//struct Node
//{
//    public ulong Id;
//    public Vector2 Point;
//}

public class GeoNamesTest : MonoBehaviour
{
    public District DistrictPrefab = null;

    public const float ShapeScaler = 100.0f;
    public const float SqrMagnitudeDelta = 0.00000000001f;
    public const float SqrMagnitudeLargestWayGap = 0.0001f;

    private List<Vector2> _CityShape = new List<Vector2>();
    private string _CityBoundingBoxString = "";
    private bool _Verified = false;

    private void Awake()
    {
        StartCoroutine(GenerateCity("Berlin"));
    }

    private IEnumerator GenerateCity(string cityName)
    {
        yield return VerifyCity(cityName);

        if (_Verified)
        {
            yield return ObtainCityShape(cityName);
            yield return GenerateDistricts(cityName);
        }
    }

    IEnumerator VerifyCity(string cityName)
    {
        UnityWebRequest nominatimRequest = UnityWebRequest.Get("https://nominatim.openstreetmap.org/search?q=" + cityName + "&format=xml&addressdetails=1&extratags=1");
        yield return nominatimRequest.SendWebRequest();
        if (!nominatimRequest.isDone)
        {
            Debug.LogError("Not Done!");
            yield break;
        }

        if (nominatimRequest.isNetworkError)
        {
            Debug.LogError("Network Error!");
            yield break;
        }

        string nominatimResult = nominatimRequest.downloadHandler.text;
        XmlDocument nominatimDoc = new XmlDocument();
        nominatimDoc.LoadXml(nominatimResult);

        XmlNode searchResults = nominatimDoc["searchresults"];
        if (searchResults == null)
        {
            Debug.LogError("search result was null!");
            yield break;
        }

        foreach (XmlNode searchResult in searchResults.ChildNodes)
        {
            XmlAttribute addressRank = searchResult.Attributes["address_rank"];
            if (addressRank != null && int.TryParse(addressRank.Value, out int rank)
                && rank >= 13 && rank <= 16)
            {
                XmlAttribute displayName = searchResult.Attributes["display_name"];
                if (displayName == null)
                {
                    Debug.LogWarning("displayName was null!");
                    continue;
                }
                string[] nameSplit = displayName.Value.Split(',');
                if (nameSplit.Length < 1)
                {
                    Debug.LogWarning("nameSplit was empty!");
                    continue;
                }

                if (nameSplit[0].ToLower() != cityName.ToLower())
                {
                    continue;
                }

                XmlAttribute boundingBox = searchResult.Attributes["boundingbox"];
                if (boundingBox == null)
                {
                    Debug.LogWarning("bounding box was null!");
                    continue;
                }

                _Verified = true;
                _CityBoundingBoxString = boundingBox.Value;
                yield break;
            }
        }
    }

    private IEnumerator ObtainCityShape(string cityName)
    {
        string[] bbox = _CityBoundingBoxString.Split(',');
        string overpassQuery = "relation[boundary=administrative][name=\"" + cityName + "\"][\"admin_level\"~\"4|6\"](" + bbox[0] + "," + bbox[2] + "," + bbox[1] + "," + bbox[3] + ");(._; >;);out qt;";
        System.Action<string> callback = overpassData => 
        {
            List<RelationData> relations = ProcessOverpassData(overpassData);
            if (relations != null && relations.Count > 0)
            {
                _CityShape = GenerateShape(relations[0]);
            }
            else
            {
                Debug.LogError("can't generate CityShape, recieved unexpected amount of relations!");
            }
        };
        yield return ObtainOverpassData(overpassQuery, callback);
    }

    private IEnumerator GenerateDistricts(string cityName)
    {
        string[] bbox = _CityBoundingBoxString.Split(',');
        string overpassQuery = "area[name=" + cityName + "](" + bbox[0] + "," + bbox[2] + "," + bbox[1] + "," + bbox[3] + ")->.b;rel[boundary=administrative][admin_level=10](area.b);(._; >;);out qt;";
        System.Action<string> callback = overpassData =>
        {
            List<RelationData> relations = ProcessOverpassData(overpassData);
            Debug.Log("Generated " + relations.Count + " districts");

            foreach (RelationData relation in relations)
            {
                List<Vector2> shape = GenerateShape(relation);

                District district = Instantiate(DistrictPrefab);
                district.TestInit(shape.ToArray());
            }
        };
        yield return ObtainOverpassData(overpassQuery, callback);
    }

    private IEnumerator ObtainOverpassData( string overpassQuery, System.Action<string> callback)
    {        
        UnityWebRequest overpassRequest = UnityWebRequest.Get("https://overpass-api.de/api/interpreter?data=" + overpassQuery);
        yield return overpassRequest.SendWebRequest();

        if (!overpassRequest.isDone)
        {
            Debug.LogError("Not Done!");
            yield break;
        }

        if (overpassRequest.isNetworkError)
        {
            Debug.LogError("Network Error!");
            yield break;
        }

        callback.Invoke(overpassRequest.downloadHandler.text);
    }

    private List<RelationData> ProcessOverpassData(string overpassResult)
    {
        XmlDocument overpassDoc = new XmlDocument();
        overpassDoc.LoadXml(overpassResult);

        XmlNode osmNode = overpassDoc["osm"];
        if (osmNode == null)
        {
            Debug.LogWarning("osmNode was null!");
            return null;
        }

        Dictionary<long, string> names = new Dictionary<long, string>();
        Dictionary<long, Vector2> nodes = new Dictionary<long, Vector2>();
        Dictionary<long, List<long>> ways = new Dictionary<long, List<long>>();
        List<RelationReference> relations = new List<RelationReference>();

        foreach (XmlNode child in osmNode.ChildNodes)
        {
            if (child.Name == "node")
            {
                XmlAttribute lat = child.Attributes["lat"];
                XmlAttribute lon = child.Attributes["lon"];
                XmlAttribute id = child.Attributes["id"];

                if (lat != null && lon != null && id != null
                    && float.TryParse(lat.Value, out float parsedLat)
                    && float.TryParse(lon.Value, out float parsedLon)
                    && long.TryParse(id.Value, out long parsedId))
                {
                    //TODO: use proper mapping for lat/long to avoid distortion!!!

                    nodes.Add(parsedId, new Vector2(parsedLon, parsedLat));
                    foreach (XmlNode nodeChild in child.ChildNodes)
                    {
                        XmlAttribute key = nodeChild.Attributes["k"];
                        if (key != null && key.Value.ToLower() == "name")
                        {
                            XmlAttribute value = nodeChild.Attributes["v"];
                            if (value != null)
                            {
                                names.Add(parsedId, value.Value);
                            }
                        }
                    }
                }
            }
            if (child.Name == "way")
            {
                XmlAttribute wayId = child.Attributes["id"];
                if (long.TryParse(wayId.Value, out long parsedWayId))
                {
                    List<long> way = new List<long>();
                    foreach (XmlNode wayChild in child.ChildNodes)
                    {
                        XmlAttribute nodeId = wayChild.Attributes["ref"];
                        if (nodeId != null && long.TryParse(nodeId.Value, out long parsedNodeId))
                        {
                            way.Add(parsedNodeId);
                        }
                    }
                    ways.Add(parsedWayId, way);
                }
            }
            if (child.Name == "relation")
            {
                RelationReference relationReference = new RelationReference();
                relationReference.WayIds = new List<long>();
                foreach (XmlNode relationChild in child.ChildNodes)
                {
                    XmlAttribute type = relationChild.Attributes["type"];
                    XmlAttribute id = relationChild.Attributes["ref"];
                    if (id != null && type != null
                        && long.TryParse(id.Value, out long parsedId))
                    {
                        if (type.Value == "node")
                        {
                            XmlAttribute role = relationChild.Attributes["role"];
                            if (role != null && role.Value == "admin_centre" 
                                || role.Value == "admin_center"
                                || role.Value == "label")
                            {
                                relationReference.CenterId = parsedId;
                                relationReference.NameId = parsedId;
                            }
                        }
                        else if (type.Value == "way")
                        {
                            relationReference.WayIds.Add(parsedId);
                        }
                    }
                    else if (relationReference.NameId == 0)
                    {
                        //save label name as fallback name in case there is no admin_center/label node available
                        XmlAttribute key = relationChild.Attributes["k"];
                        if (key != null && key.Value == "name")
                        {
                            XmlAttribute relationId = child.Attributes["id"];
                            XmlAttribute value = relationChild.Attributes["v"];
                            if (value != null && relationId != null
                                && long.TryParse(relationId.Value, out long parsedRelationId))
                            {
                                names.Add(parsedRelationId, value.Value);
                                relationReference.NameId = parsedRelationId;
                            }
                        }
                    }
                }
                relations.Add(relationReference);
            }
        }

        List<RelationData> relationDataList = new List<RelationData>();
        foreach (RelationReference relationReference in relations)
        {
            RelationData relationData = new RelationData();
            if (nodes.TryGetValue(relationReference.CenterId, out Vector2 center))
            {
                relationData.Center = center;
            }
            else
            {
                //TODO: calculate center
            }
            if (names.TryGetValue(relationReference.NameId, out string name))
            {
                relationData.Name = name;
            }
            relationData.Ways = new List<List<Vector2>>();
            foreach (long wayId in relationReference.WayIds)
            {
                List<Vector2> way = new List<Vector2>();
                foreach (long nodeId in ways[wayId])
                {
                    way.Add(nodes[nodeId]);
                }
                relationData.Ways.Add(way);
            }
            relationDataList.Add(relationData);
        }
        return relationDataList;
    }

    private List<Vector2> GenerateShape(RelationData relation)
    {
        List<Vector2> shape = new List<Vector2>();
        List<Vector2> closestWay = relation.Ways[0];
        bool reverse = false;
        Vector2 lastAddedPoint = Vector2.zero;

        do
        {
            for (int i = 0; i < closestWay.Count; i++)
            {
                Vector2 point = closestWay[reverse ? closestWay.Count - 1 - i : i];
                Vector2 shapePoint = point * ShapeScaler;

                if (shape.Count == 0 || (shapePoint - shape[shape.Count - 1]).sqrMagnitude > SqrMagnitudeDelta)
                {
                    shape.Add(shapePoint);
                    lastAddedPoint = point;
                }
            }
            relation.Ways.Remove(closestWay);
            closestWay = FindClosestWay(relation, lastAddedPoint, out reverse);
        }
        while (closestWay != null);

        return shape;
    }

    private List<Vector2> FindClosestWay(RelationData relation, Vector2 referencePoint, out bool reverse)
    {
        reverse = false;

        if (relation.Ways == null || relation.Ways.Count == 0)
        {
            return null;
        }

        List<Vector2> closestWay = relation.Ways[0];
        Vector2 closestPoint = closestWay[0];
        float sqrDistanceToClosestPoint = (referencePoint - closestPoint).sqrMagnitude;
        foreach (List<Vector2> way in relation.Ways)
        {
            Vector2 point = way[0];
            float sqrDistance = (point - referencePoint).sqrMagnitude;
            if ( sqrDistance < sqrDistanceToClosestPoint )
            {
                reverse = false;
                closestWay = way;
                sqrDistanceToClosestPoint = sqrDistance;
            }

            point = way[way.Count - 1];
            sqrDistance = (point - referencePoint).sqrMagnitude;
            if ( sqrDistance < sqrDistanceToClosestPoint )
            {
                reverse = true;
                closestWay = way;
                sqrDistanceToClosestPoint = sqrDistance;
            }
        }

        if (sqrDistanceToClosestPoint < SqrMagnitudeLargestWayGap)
        {
            return closestWay;
        }
        return null;
    }
}





