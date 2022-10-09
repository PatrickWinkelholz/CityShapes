using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Xml;

public class GeoNamesTest : MonoBehaviour
{
    public District DistrictPrefab = null;

    public const float ShapeScaler = 100.0f;
    public const float LatLongSubtrahend = 0;
    public const float SqrMagnitudeDelta = 0.00000000001f;
    public const float SqrMagnitudeLargestWayGap = 0.0001f;

    private void Awake()
    {
        StartCoroutine(GetData());
    }

    IEnumerator GetData()
    {
        string search = "Berlin";
        UnityWebRequest nominatimRequest = UnityWebRequest.Get("https://nominatim.openstreetmap.org/search?q=" + search + "&format=xml&addressdetails=1&extratags=1");
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

                if (nameSplit[0].ToLower() != search.ToLower())
                {
                    continue;
                }

                XmlAttribute boundingBox = searchResult.Attributes["boundingbox"];
                if (boundingBox == null)
                {
                    Debug.LogWarning("bounding box was null!");
                    continue;
                }

                string[] coords = boundingBox.Value.Split(',');
                string overpassString = "relation[boundary=administrative][name=\"" + nameSplit[0] + "\"](" + coords[0] + "," + coords[2] + "," + coords[1] + "," + coords[3] + ");>;out skel;";
                UnityWebRequest overpassRequest = UnityWebRequest.Get("https://overpass-api.de/api/interpreter?data=" + overpassString);
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

                string overpassResult = overpassRequest.downloadHandler.text;

                XmlDocument overpassDoc = new XmlDocument();
                overpassDoc.LoadXml(overpassResult);

                XmlNode osmNode = overpassDoc["osm"];
                if (osmNode == null)
                {
                    Debug.LogWarning("osmNode was null!");
                    continue;
                }

                Dictionary<long, Vector2> nodes = new Dictionary<long, Vector2>();
                List<List<long>> ways = new List<List<long>>();

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
                            nodes.Add(parsedId, new Vector2(parsedLat, parsedLon));
                        }
                    }               
                    if (child.Name == "way")
                    {
                        List<long> way = new List<long>();
                        foreach(XmlNode wayChild in child.ChildNodes)
                        {
                            XmlAttribute id = wayChild.Attributes["ref"];
                            if (id != null && long.TryParse(id.Value, out long parsedId))
                            {
                                way.Add(parsedId);
                            }
                        }
                        ways.Add(way);
                    }
                }

                List<Vector2> shape = new List<Vector2>();
                List<long> closestWay = ways[0];
                bool reverse = false;
                long lastAddedNodeId = 0;

                do
                {
                    for (int i = 0; i < closestWay.Count; i++)
                    {
                        long nodeId = closestWay[reverse ? closestWay.Count - 1 - i : i];
                        Vector2 shapePoint = nodes[nodeId] * ShapeScaler;

                        if (shape.Count == 0 || (shapePoint - shape[shape.Count - 1]).sqrMagnitude > SqrMagnitudeDelta)
                        {
                            shape.Add(shapePoint);
                            lastAddedNodeId = nodeId;
                        }
                    }
                    ways.Remove(closestWay);
                    closestWay = FindClosestWay(ways, nodes, nodes[lastAddedNodeId], out reverse);
                }
                while (closestWay != null);

                District district = Instantiate(DistrictPrefab);
                district.TestInit(shape.ToArray());
                yield break;
            }
        }
    }

    private List<long> FindClosestWay(List<List<long>>ways, Dictionary<long, Vector2> nodes, Vector2 referencePoint, out bool reverse)
    {
        reverse = false;

        if (ways == null || nodes == null 
            || ways.Count == 0 || nodes.Count == 0)
        {
            return null;
        }

        List<long> closestWay = ways[0];
        Vector2 closestPoint = nodes[closestWay[0]];
        float sqrDistanceToClosestPoint = (referencePoint - closestPoint).sqrMagnitude;
        foreach (List<long> way in ways)
        {
            Vector2 point = nodes[way[0]];
            float sqrDistance = (point - referencePoint).sqrMagnitude;
            if ( sqrDistance < sqrDistanceToClosestPoint )
            {
                reverse = false;
                closestWay = way;
                sqrDistanceToClosestPoint = sqrDistance;
            }

            point = nodes[way[way.Count - 1]];
            sqrDistance = (point - referencePoint).sqrMagnitude;
            if ( sqrDistance < sqrDistanceToClosestPoint )
            {
                reverse = true;
                closestWay = way;
                sqrDistanceToClosestPoint = sqrDistance;
            }

            //if (sqrDistanceToClosestPoint < SqrMagnitudeDelta)
            //{
            //    return closestWay;
            //}
        }

        if (sqrDistanceToClosestPoint < SqrMagnitudeLargestWayGap)
        {
            return closestWay;
        }
        return null;
    }
}





