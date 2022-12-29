using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public struct RelationData
{
    public string Name;
    public int AdminLevel;
    public Vector2 Center;
    public List<List<Vector2>> Ways;
}

public struct RelationReference
{
    public long CenterId;
    public int AdminLevel;
    public long NameId;
    public List<long> WayIds;
}

public struct NominatimResult
{
    public string Name;
    public string DisplayName;
    public string BoundingBox; //minLat, maxLat, minLon, maxLon
}

public struct TileData
{
    public Sprite Sprite;
    public Vector3 Pos;
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



public class OsmDataProcessor
{
    public System.Action<string> StatusChangedEvent = default;

    public const float ShapeScaler = 2.56f;
    public const float SqrMagnitudeDelta = 0.000000001f;
    public const float SqrMagnitudeLargestWayGap = 0.01f;
    public static Vector2Int ExtraBackgroundTiles = new Vector2Int(1, 4);
    static int _Zoom = 13;

    static string _OverpassUrl = "https://overpass-api.de/api/interpreter?data=";
    static System.Globalization.NumberStyles _NumberStyle = System.Globalization.NumberStyles.Number;
    static System.Globalization.CultureInfo _CultureInfo = System.Globalization.CultureInfo.InvariantCulture;

    public IEnumerator SearchCities(string query, System.Action<string, List<NominatimResult>> callback)
    {
        StatusChangedEvent?.Invoke("Requesting search results...");

        yield return Utils.SendWebRequest("https://nominatim.openstreetmap.org/search?q=" + query + "&format=xml&addressdetails=1&extratags=1", result =>
        {
            if (result.Length == 0 || result[0] != '<')
            {
                callback?.Invoke(result, default);
                return;
            }

            StatusChangedEvent?.Invoke("Processing search results...");

            XmlDocument nominatimDoc = new XmlDocument();
            nominatimDoc.LoadXml(result);

            XmlNode searchResults = nominatimDoc["searchresults"];
            if (searchResults == null)
            {
                callback?.Invoke("searchresults not found!", default);
                return;
            }

            List<NominatimResult> cities = new List<NominatimResult>();
            foreach (XmlNode searchResult in searchResults.ChildNodes)
            {
                XmlAttribute addressRank = searchResult.Attributes["address_rank"];
                if (addressRank != null && int.TryParse(addressRank.Value, _NumberStyle, _CultureInfo, out int rank)
                    && rank >= 13 && rank <= 16)
                {
                    XmlAttribute displayName = searchResult.Attributes["display_name"];
                    if (displayName == null)
                    {
                        Debug.LogWarning("displayName was null!");
                        continue;
                    }
                    string[] displayNameSplit = displayName.Value.Split(',');
                    if (displayNameSplit.Length < 1)
                    {
                        Debug.LogWarning("nameSplit was empty!");
                        continue;
                    }

                    string countryCode = "";
                    if (displayNameSplit.Length > 1)
                    {
                        countryCode = ", " + displayNameSplit[displayNameSplit.Length - 1].ToUpper();
                    }
                    foreach (XmlNode childNode in searchResult.ChildNodes)
                    {
                        if (childNode.Name == "country_code")
                        {
                            countryCode = ", " + childNode.InnerText.ToUpper();
                        }
                    }

                    XmlAttribute boundingBox = searchResult.Attributes["boundingbox"];
                    if (boundingBox == null)
                    {
                        Debug.LogWarning("bounding box was null!");
                        continue;
                    }

                    NominatimResult city = new NominatimResult();
                    city.Name = displayNameSplit[0];
                    city.DisplayName = displayNameSplit[0] + countryCode;
                    city.BoundingBox = boundingBox.Value;
                    cities.Add(city);
                }
            }
            callback.Invoke("success", cities);
        });
    }

    public IEnumerator GenerateCityData(string cityName, string boundingBox, System.Action<string, CityData> callback)
    {
        StatusChangedEvent?.Invoke("Requesting city data...");

        CityData cityData = new CityData();
        cityData.Name = cityName;

        string cityNameOnly = cityName.Split(',')[0];
        string[] bbox = boundingBox.Split(','); //minLat, maxLat, minLon, maxLon

        int cityAdminLevel = 0;
        yield return GenerateCityShape(cityNameOnly, bbox, (shape, adminLevel) =>
        {
            cityData.Shape = shape;
            cityAdminLevel = adminLevel;

            StatusChangedEvent?.Invoke("Requesting district boundary data...");
        });

        string res = "";
        yield return GenerateDistricts(cityNameOnly, bbox, cityAdminLevel, (result, districts)=> 
        {
            res = result;
            cityData.Districts = districts;
        });

        if (res != "success")
        {
            callback?.Invoke(res, default);
            yield break;
        }

        if (cityAdminLevel == 0)
        {
            //manually calculate city center from district centers in case city shape generation failed
            foreach (DistrictData districtData in cityData.Districts)
            {
                cityData.Shape.Center += districtData.Shape.Center;
            }
            cityData.Shape.Center /= cityData.Districts.Count;
        }

        StatusChangedEvent?.Invoke("Requesting background tiles...");

        yield return GenerateBackgroundTiles(bbox, (tiles) =>
        {
            cityData.BackgroundTiles = tiles;
        });

        callback?.Invoke("success", cityData);
    }

    private IEnumerator GenerateCityShape(string cityNameOnly, string[] bbox, System.Action<Shape, int> callback)
    {
        string overpassQuery = "relation[boundary=administrative][~\"^name(:en)?$\"~\"^" + cityNameOnly + "$\",i][\"admin_level\"~\"4|6|8\"](" + bbox[0] + "," + bbox[2] + "," + bbox[1] + "," + bbox[3] + ");(._; >;);out qt;";
        yield return Utils.SendWebRequest(_OverpassUrl + overpassQuery, result =>
        {
            if (result[0] != '<')
            {
                Debug.LogWarning(result);
                return;
            }

            StatusChangedEvent?.Invoke("Processing city data...");
            List<RelationData> relations = ProcessOverpassData(result);
            if (relations != null && relations.Count > 0)
            {
                int largestAdminLevelIndex = 0;
                for (int i = 0; i < relations.Count; i++)
                {
                    if (relations[i].AdminLevel > relations[largestAdminLevelIndex].AdminLevel)
                    {
                        largestAdminLevelIndex = i;
                    }
                }
                StatusChangedEvent?.Invoke("Generating city shape...");
                Shape cityShape = GenerateShape(relations[largestAdminLevelIndex]);
                callback.Invoke(cityShape, relations[largestAdminLevelIndex].AdminLevel);
            }
            else
            {
                Debug.LogWarning("no city relation found!");
                return;
            }
        });
    }

    private IEnumerator GenerateDistricts(string cityNameOnly, string[] bbox, int cityAdminLevel, System.Action<string, List<DistrictData>> callback)
    {
        string areaFilterString = "";
        if (cityAdminLevel > 0)
        {
            areaFilterString = "[admin_level=" + cityAdminLevel + "]";
        }
        string overpassQuery = "area[~\"^name(:en)?$\"~\"^" + cityNameOnly + "$\",i]" + areaFilterString + "(" + bbox[0] + "," + bbox[2] + "," + bbox[1] + "," + bbox[3] + ")->.b;rel[boundary=administrative][\"admin_level\"~\"9|10\"](area.b);(._; >;);out qt;";

        yield return Utils.SendWebRequest(_OverpassUrl + overpassQuery, result =>
        {
            if (result[0] != '<')
            {
                callback?.Invoke(result, default);
                return;
            }

            StatusChangedEvent?.Invoke("Processing district boundary data...");

            List<RelationData> relations = ProcessOverpassData(result);
            if (relations == null || relations.Count == 0)
            {
                callback?.Invoke("failed to generate districts!", default);
                return;
            }

            Debug.Log("Generated " + relations.Count + " relations");

            Dictionary<int, List<RelationData>> relationsByAdminLevel = new Dictionary<int, List<RelationData>>();
            int adminLevelWithMostRelations = relations[0].AdminLevel;
            foreach (RelationData relation in relations)
            {
                int adminLevel = relation.AdminLevel;
                if (relationsByAdminLevel.ContainsKey(adminLevel))
                {
                    relationsByAdminLevel[adminLevel].Add(relation);
                }
                else
                {
                    relationsByAdminLevel.Add(adminLevel, new List<RelationData>() { relation });
                }
                if (relationsByAdminLevel[adminLevel].Count > relationsByAdminLevel[adminLevelWithMostRelations].Count)
                {
                    adminLevelWithMostRelations = adminLevel;
                }
            }

            StatusChangedEvent?.Invoke("Generating district shapes...");

            List<DistrictData> districts = new List<DistrictData>();
            foreach (RelationData relation in relationsByAdminLevel[adminLevelWithMostRelations])
            {
                DistrictData districtData = new DistrictData();
                districtData.Name = relation.Name;
                districtData.Shape = GenerateShape(relation);
                districts.Add(districtData);
            }

            callback.Invoke("success", districts);
        });
    }

    private IEnumerator GenerateBackgroundTiles(string[] bbox, System.Action<TileData[,]> callback)
    {
        //calculate tiles
        float minLat = float.Parse(bbox[0]);
        float maxLat = float.Parse(bbox[1]);
        float minLon = float.Parse(bbox[2]);
        float maxLon = float.Parse(bbox[3]);

        float centerLat = minLat + (maxLat - minLat) / 2.0f;
        float centerLon = minLon + (maxLon - minLon) / 2.0f;

        CalculateTiles(minLat, minLon, out float minXTile, out float minYTile);
        CalculateTiles(maxLat, maxLon, out float maxXTile, out float maxYTile);
        int nrXTiles = Mathf.Abs((int)maxXTile - (int)minXTile) + 1;
        int nrYTiles = Mathf.Abs((int)maxYTile - (int)minYTile) + 1;

        TileData[,] tiles = new TileData[nrYTiles + ExtraBackgroundTiles.y * 2, nrXTiles + ExtraBackgroundTiles.x * 2];

        for (int y = 0; y < nrYTiles + ExtraBackgroundTiles.y * 2; y++)
        {
            for (int x = 0; x < nrXTiles + ExtraBackgroundTiles.x * 2; x++)
            {
                int currentX = (int)Mathf.Min(minXTile, maxXTile) - ExtraBackgroundTiles.x + x;
                int currentY = (int)Mathf.Min(minYTile, maxYTile) - ExtraBackgroundTiles.y + y;
                string link = "https://stamen-tiles.a.ssl.fastly.net/watercolor/" + _Zoom + "/"+ currentX + "/" + currentY + ".jpg";
                yield return Utils.SendWebRequest(link, result =>
                {
                    Texture2D texture = new Texture2D(256, 256);
                    if (!ImageConversion.LoadImage(texture, result))
                    {
                        Debug.LogWarning("Background Sprite conversion failed!");
                    }
                    TileData tileData = new TileData();
                    tileData.Sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), new Vector2(0, 1.0f));
                    tileData.Pos = new Vector3(currentX, -currentY, 1.0f) * ShapeScaler;
                    tiles[y, x] = tileData;
                });
            }
        }
        Debug.Log("Generated " + tiles.Length + " Backgound Tiles (" + (nrXTiles + ExtraBackgroundTiles.x * 2) + " * " + (nrYTiles + ExtraBackgroundTiles.y * 2) + ")");

        callback?.Invoke(tiles);
    }

    private void CalculateTiles(float lat, float lon, out float xTile, out float yTile)
    {
        float n = Mathf.Pow(2, _Zoom);
        float latRad = Mathf.Deg2Rad * lat;
        xTile = n * ((lon + 180.0f) / 360.0f);
        yTile = n * (1 - (Mathf.Log(Mathf.Tan(latRad) + (1.0f / Mathf.Cos(latRad))) / Mathf.PI)) / 2.0f;
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
                    && float.TryParse(lat.Value, _NumberStyle, _CultureInfo, out float parsedLat)
                    && float.TryParse(lon.Value, _NumberStyle, _CultureInfo, out float parsedLon)
                    && long.TryParse(id.Value, _NumberStyle, _CultureInfo, out long parsedId))
                {
                    //Apply mercator projection
                    ////https://stackoverflow.com/questions/14329691/convert-latitude-longitude-point-to-a-pixels-x-y-on-mercator-projection
                    //float mapWidth = 100.0f;
                    //float mapHeight = 50.0f;

                    //float x = (parsedLon + 180.0f) * (mapWidth/360.0f);
                    //float latRad = parsedLat * Mathf.PI / 180.0f;
                    //float mercN = Mathf.Log(Mathf.Tan((Mathf.PI / 4.0f) + (latRad / 2.0f)));
                    //float y = (mapHeight / 2) - (mapWidth * mercN / (2.0f * Mathf.PI));

                    CalculateTiles(parsedLat, parsedLon, out float x, out float y);

                    nodes.Add(parsedId, new Vector2(x, -y));
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
                if (long.TryParse(wayId.Value, _NumberStyle, _CultureInfo, out long parsedWayId))
                {
                    List<long> way = new List<long>();
                    foreach (XmlNode wayChild in child.ChildNodes)
                    {
                        XmlAttribute nodeId = wayChild.Attributes["ref"];
                        if (nodeId != null && long.TryParse(nodeId.Value, _NumberStyle, _CultureInfo, out long parsedNodeId))
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
                        && long.TryParse(id.Value, _NumberStyle, _CultureInfo, out long parsedId))
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
                    else
                    {
                        XmlAttribute key = relationChild.Attributes["k"];
                        XmlAttribute value = relationChild.Attributes["v"];
                        if (key != null && key.Value == "name"
                            && relationReference.NameId == 0)
                        {
                            //save label name as fallback name in case there is no admin_center/label node available
                            XmlAttribute relationId = child.Attributes["id"];
                            if (value != null && relationId != null
                                && long.TryParse(relationId.Value, _NumberStyle, _CultureInfo, out long parsedRelationId))
                            {
                                names.Add(parsedRelationId, value.Value);
                                relationReference.NameId = parsedRelationId;
                            }
                        }
                        if (key != null && key.Value == "admin_level")
                        {
                            if (value != null && int.TryParse(value.Value, _NumberStyle, _CultureInfo, out int parsedAdminLevel))
                            {
                                relationReference.AdminLevel = parsedAdminLevel;
                            }
                        }
                    }
                }
                if (relationReference.WayIds.Count > 0)
                {
                    relations.Add(relationReference);
                }
            }
        }

        List<RelationData> relationDataList = new List<RelationData>();
        foreach (RelationReference relationReference in relations)
        {
            RelationData relationData = new RelationData();
            relationData.AdminLevel = relationReference.AdminLevel;
            if (nodes.TryGetValue(relationReference.CenterId, out Vector2 center))
            {
                relationData.Center = center;
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

    private Shape GenerateShape(RelationData relation)
    {
        if (relation.Ways == null || relation.Ways.Count == 0)
        {
            return default;
        }

        List<Vector2> points = new List<Vector2>();
        List<Vector2> closestWay = relation.Ways[0];
        bool reverse = false;
        Vector2 lastAddedPoint = Vector2.zero;

        do
        {
            for (int i = 0; i < closestWay.Count; i++)
            {
                Vector2 point = closestWay[reverse ? closestWay.Count - 1 - i : i];
                Vector2 shapePoint = point * ShapeScaler;

                if (points.Count == 0 || (shapePoint - points[points.Count - 1]).sqrMagnitude > SqrMagnitudeDelta)
                {
                    points.Add(shapePoint);
                    lastAddedPoint = point;
                }
            }
            relation.Ways.Remove(closestWay);
            closestWay = FindClosestWay(relation, lastAddedPoint, out reverse);
        }
        while (closestWay != null);

        Shape shape = new Shape();
        shape.Points = points;
        if (relation.Center == default)
        {
            shape.Center = CalculateCenter(points);
        }
        else
        {
            shape.Center = relation.Center * ShapeScaler;
        }
        return shape;
    }

    private Vector2 CalculateCenter(List<Vector2> points)
    {
        Vector2 center = new Vector2();
        foreach( Vector2 point in points)
        {
            center += point;
        }
        return center / points.Count;
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





