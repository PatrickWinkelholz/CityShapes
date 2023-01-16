﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public struct RelationData
{
    public string Name;
    public int AdminLevel;
    public Vector2 Center;
    public List<WayData> Ways;
}

public struct WayData
{
    public string Name;
    public List<Vector2> Points;
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


public class OsmDataProcessor : MonoBehaviour
{
    private struct OverpassData
    {
        public OverpassData(
            Dictionary<long, string> names,
            Dictionary<long, Vector2> nodes,
            Dictionary<long, WayReference> ways,
            List<RelationReference> relations)
        {
            Names = names;
            Nodes = nodes;
            Ways = ways;
            Relations = relations;
        }

        public Dictionary<long, string> Names;
        public Dictionary<long, Vector2> Nodes;
        public Dictionary<long, WayReference> Ways;
        public List<RelationReference> Relations;
    }

    private struct RelationReference
    {
        public long CenterId;
        public int AdminLevel;
        public long NameId;
        public List<long> WayIds;
    }

    private struct WayReference
    {
        public string Name;
        public List<long> NodeIds;
    }

    public System.Action<string> StatusChangedEvent = default;

    [SerializeField] private int _TileResolution = 256;
    [SerializeField] private float _SqrMagnitudeDelta = 0.000000001f;
    [SerializeField] private float _SqrMagnitudeLargestWayGap = 0.01f;
    public Vector2Int NrExtraBackgroundTiles => _NrExtraBackgroundTiles;
    [SerializeField] private Vector2Int _NrExtraBackgroundTiles = new Vector2Int(1, 4);
    [SerializeField] private float _BackgroundTileZOffset = 20.0f;
    [SerializeField] private int _Zoom = 13;

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
        });

        string res = "";
        if (GameManager.Instance.MapObjectType == ObjectType.District)
        {
            yield return GenerateDistricts(cityNameOnly, bbox, cityAdminLevel, (result, districts) =>
            {
                res = result;
                cityData.MapObjects = districts;
            });
        }
        else
        {
            yield return GenerateRoads(cityNameOnly, bbox, cityAdminLevel, (result, roads) =>
            {
                res = result;
                cityData.MapObjects = roads;
            });
        }

        if (res != "success")
        {
            callback?.Invoke(res, default);
            yield break;
        }

        if (cityAdminLevel == 0)
        {
            //manually calculate city center from mapObject centers in case city shape generation failed
            foreach (MapObjectData MapObjectData in cityData.MapObjects)
            {
                cityData.Shape.Center += MapObjectData.Shape.Center;
            }
            cityData.Shape.Center /= cityData.MapObjects.Count;
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

            ProcessOverpassData(result, out OverpassData overpassData);
            ObtainRelationsFromOverpassData(overpassData, out List<RelationData> relations);

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
                Shape cityShape = GenerateShape(relations[largestAdminLevelIndex].Ways, relations[largestAdminLevelIndex].Center);
                callback.Invoke(cityShape, relations[largestAdminLevelIndex].AdminLevel);
            }
            else
            {
                Debug.LogWarning("no city relation found!");
                return;
            }
        });
    }

    private IEnumerator GenerateDistricts(string cityNameOnly, string[] bbox, int cityAdminLevel, System.Action<string, List<MapObjectData>> callback)
    {
        StatusChangedEvent?.Invoke("Requesting district boundary data...");

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

            ProcessOverpassData(result, out OverpassData overpassData);
            ObtainRelationsFromOverpassData(overpassData, out List<RelationData> relations);

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

            List<MapObjectData> districts = new List<MapObjectData>();
            foreach (RelationData relation in relationsByAdminLevel[adminLevelWithMostRelations])
            {
                MapObjectData district = new MapObjectData();
                district.Name = relation.Name;
                district.Shape = GenerateShape(relation.Ways, relation.Center);
                districts.Add(district);
            }

            if (districts.Count == 0)
            {
                callback?.Invoke("failed to generate districts!", default);
                return;
            }

            callback.Invoke("success", districts);
        });
    }

    private IEnumerator GenerateRoads(string cityNameOnly, string[] bbox, int cityAdminLevel, System.Action<string, List<MapObjectData>> callback)
    {
        StatusChangedEvent?.Invoke("Requesting road data...");

        string areaFilterString = "";
        if (cityAdminLevel > 0)
        {
            areaFilterString = "[admin_level=" + cityAdminLevel + "]";
        }
        string overpassQuery = "area[~\"^name(:en)?$\"~\"^" + cityNameOnly + "$\",i]" + areaFilterString + "(" + bbox[0] + "," + bbox[2] + "," + bbox[1] + "," + bbox[3] + ")->.a;way[~\"^name|ref$\"~\".\"][\"highway\"~\"^(trunk|motorway|primary|secondary" /*|tertiary*/ + ")$\"](area.a);(._;>;);out qt;";

        yield return Utils.SendWebRequest(_OverpassUrl + overpassQuery, result =>
        {
            if (result[0] != '<')
            {
                callback?.Invoke(result, default);
                return;
            }

            StatusChangedEvent?.Invoke("Processing road data...");

            ProcessOverpassData(result, out OverpassData overpassData);
            ObtainWaysFromOverpassData(overpassData, out List<WayData> ways);

            if (ways == null || ways.Count == 0)
            {
                callback?.Invoke("failed to generate roads!", default);
                return;
            }

            StatusChangedEvent?.Invoke("Generating road shapes...");

            List<MapObjectData> roads = new List<MapObjectData>();
            do
            {
                MapObjectData road = new MapObjectData();
                road.Name = ways[0].Name;

                List<WayData> waysWithSameName = ways.FindAll(w => w.Name == road.Name);
                road.Shape = GenerateShape(waysWithSameName);

                roads.Add(road);
                ways.RemoveAll(w => w.Name == road.Name);

            } while (ways.Count > 0);

            if (roads.Count == 0)
            {
                callback?.Invoke("failed to generate roads!", default);
                return;
            }

            Debug.Log("Generated " + roads.Count + " roads");

            callback.Invoke("success", roads);
        });
    }

    //private void ExtrudeLine(List<Vector2> line)
    //{
    //    LineRenderer

    //    //https://gamedev.stackexchange.com/questions/75182/how-can-i-create-or-extrude-a-mesh-along-a-spline
    //    for (float i = 0; i <= 1.0f;)
    //    {
    //        Vector2 p = CatmullRom.calculatePoint(dataSet, i);
    //        Vector2 deriv = CatmullRom.calculateDerivative(dataSet, i);
    //        float len = deriv.Length();
    //        i += step / len;
    //        deriv.divide(len);
    //        deriv.scale(thickness);
    //        deriv.set(-deriv.y, deriv.x);
    //        Vector2 v1 = new Vector2();
    //        v1.set(p).add(deriv);
    //        vertices.add(v1);
    //        Vector2 v2 = new Vector2();
    //        v2.set(p).sub(deriv);
    //        vertices.add(v2);

    //        if (i > 1.0f) i = 1.0f;
    //    }
    //}

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
        
        //amount of tiles required to cover the entire city
        Vector2Int necessaryTiles = new Vector2Int(
            Mathf.Abs((int)maxXTile - (int)minXTile) + 1, 
            Mathf.Abs((int)maxYTile - (int)minYTile) + 1);

        //total tiles, including extra tiles for more space
        Vector2Int totalTiles = necessaryTiles + _NrExtraBackgroundTiles * 2; // new Vector2Int(
            //necessaryTiles.x + _NrExtraBackgroundTiles.x * 2,
            //necessaryTiles.y + _NrExtraBackgroundTiles.y * 2);

        TileData[,] tiles = new TileData[totalTiles.y, totalTiles.x];

        int processedTiles = 0;
        for (int y = 0; y < totalTiles.y; y++)
        {
            for (int x = 0; x < totalTiles.x; x++)
            {
                Vector2Int tileCords = new Vector2Int(
                    (int)Mathf.Min(minXTile, maxXTile) - _NrExtraBackgroundTiles.x + x,
                    (int)Mathf.Min(minYTile, maxYTile) - _NrExtraBackgroundTiles.y + y);
                StartCoroutine(RequestTile(tileCords.x, tileCords.y, x, y, (destX, destY, tileData) => 
                {
                    tiles[destY, destX] = tileData;
                    processedTiles++;
                }));
            }
        }

        while(processedTiles < totalTiles.x * totalTiles.y)
        {
            yield return null;
        }

        Debug.Log("Generated " + tiles.Length + " Backgound Tiles (" + totalTiles.x + " * " + totalTiles.y + ")");

        callback?.Invoke(tiles);
    }

    //a lot of requests will be running parallel. passing destX and destY is necessary because their local values will have changed by the time the callback is called
    private IEnumerator RequestTile( int x, int y, int destX, int destY, System.Action<int, int, TileData> callback)
    {
        string link = "https://stamen-tiles.a.ssl.fastly.net/watercolor/" + _Zoom + "/" + x + "/" + y + ".jpg";
        yield return Utils.SendWebRequest(link, result =>
        {
            Texture2D texture = new Texture2D(_TileResolution, _TileResolution);
            if (!ImageConversion.LoadImage(texture, result))
            {
                Debug.LogWarning("Background Sprite conversion failed!");
            }
            TileData tileData = new TileData();
            tileData.Sprite = Sprite.Create(texture, new Rect(0, 0, _TileResolution, _TileResolution), new Vector2(0, 1.0f));
            tileData.Pos = new Vector3(x, -y, 0) * (_TileResolution / 100.0f);
            tileData.Pos.z = _BackgroundTileZOffset;
            callback?.Invoke(destX, destY, tileData);
        });
    }

    //this function calculates the position of any lat/lon coordinate on slippy map tiles
    //https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames
    private void CalculateTiles(float lat, float lon, out float xTile, out float yTile)
    {
        float n = Mathf.Pow(2, _Zoom);
        float latRad = Mathf.Deg2Rad * lat;
        xTile = n * ((lon + 180.0f) / 360.0f);
        yTile = n * (1 - (Mathf.Log(Mathf.Tan(latRad) + (1.0f / Mathf.Cos(latRad))) / Mathf.PI)) / 2.0f;
    }

    private void ObtainWaysFromOverpassData(OverpassData overpassData, out List<WayData> outWays)
    {
        outWays = new List<WayData>();
        foreach (var pair in overpassData.Ways)
        {
            WayData wayData = new WayData();
            wayData.Points = new List<Vector2>();
            wayData.Name = pair.Value.Name;
            foreach (long nodeId in pair.Value.NodeIds)
            {
                wayData.Points.Add(overpassData.Nodes[nodeId]);
            }
            outWays.Add(wayData);
        }
    }

    private void ObtainRelationsFromOverpassData(OverpassData overpassData, out List<RelationData> outRelations)
    {
        outRelations = new List<RelationData>();
        foreach (RelationReference relationReference in overpassData.Relations)
        {
            RelationData relationData = new RelationData();
            relationData.AdminLevel = relationReference.AdminLevel;
            if (overpassData.Nodes.TryGetValue(relationReference.CenterId, out Vector2 center))
            {
                relationData.Center = center;
            }
            if (overpassData.Names.TryGetValue(relationReference.NameId, out string name))
            {
                relationData.Name = name;
            }
            relationData.Ways = new List<WayData>();
            foreach (long wayId in relationReference.WayIds)
            {
                WayData wayData = new WayData();
                wayData.Points = new List<Vector2>();
                foreach (long nodeId in overpassData.Ways[wayId].NodeIds)
                {
                    wayData.Points.Add(overpassData.Nodes[nodeId]);
                }
                relationData.Ways.Add(wayData);
            }
            outRelations.Add(relationData);
        }
    }

    private void ProcessOverpassData(string overpassResult, out OverpassData overpassData)
    {
        overpassData = new OverpassData(
            new Dictionary<long, string>(),
            new Dictionary<long, Vector2>(),
            new Dictionary<long, WayReference>(),
            new List<RelationReference>());

        XmlDocument overpassDoc = new XmlDocument();
        overpassDoc.LoadXml(overpassResult);

        XmlNode osmNode = overpassDoc["osm"];
        if (osmNode == null)
        {
            Debug.LogWarning("osmNode was null!");
            return;
        }

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
                    CalculateTiles(parsedLat, parsedLon, out float x, out float y);

                    overpassData.Nodes.Add(parsedId, new Vector2(x, -y));
                    foreach (XmlNode nodeChild in child.ChildNodes)
                    {
                        XmlAttribute key = nodeChild.Attributes["k"];
                        if (key != null && key.Value.ToLower() == "name")
                        {
                            XmlAttribute value = nodeChild.Attributes["v"];
                            if (value != null)
                            {
                                overpassData.Names.Add(parsedId, value.Value);
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
                    WayReference wayReference = new WayReference();
                    wayReference.NodeIds = new List<long>();
                    foreach (XmlNode wayChild in child.ChildNodes)
                    {
                        XmlAttribute nodeId = wayChild.Attributes["ref"];
                        if (nodeId != null && long.TryParse(nodeId.Value, _NumberStyle, _CultureInfo, out long parsedNodeId))
                        {
                            wayReference.NodeIds.Add(parsedNodeId);
                        }
                        else
                        {
                            XmlAttribute key = wayChild.Attributes["k"];
                            XmlAttribute value = wayChild.Attributes["v"];
                            if (key != null && value != null)
                            {
                                if (key.Value == "name" || (key.Value == "ref" && wayReference.Name == default))
                                {
                                    wayReference.Name = value.Value;
                                }
                            }
                        }
                    }
                    overpassData.Ways.Add(parsedWayId, wayReference);
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
                                overpassData.Names.Add(parsedRelationId, value.Value);
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
                    overpassData.Relations.Add(relationReference);
                }
            }
        }
    }

    private Shape GenerateShape(List<WayData> ways, Vector2 center = default)
    {
        if (ways == null || ways.Count == 0)
        {
            return default;
        }

        WayData closestWay = ways[0];
        Vector2 firstPoint = closestWay.Points[0];
        bool reverse = false;
        bool endReached = false;
        Shape shape = new Shape();
        shape.Points = new List<Vector2>();
        Vector2 lastAddedPoint = Vector2.zero;
        do
        {
            for (int i = 0; i < closestWay.Points.Count; i++)
            {
                Vector2 point = closestWay.Points[reverse ? closestWay.Points.Count - 1 - i : i];
                Vector2 shapePoint = point * (_TileResolution / 100.0f);

                if (shape.Points.Count == 0 || (shapePoint - shape.Points[shape.Points.Count - 1]).sqrMagnitude > _SqrMagnitudeDelta)
                {
                    if (endReached)
                    {
                        shape.Points.Insert(0, shapePoint);
                    }
                    else
                    {
                        shape.Points.Add(shapePoint);
                    }
                    lastAddedPoint = point;
                }
            }
            ways.Remove(closestWay);
            closestWay = FindClosestWay(ways, lastAddedPoint, out reverse);

            if (ways.Count > 0 && closestWay.Points == null && !endReached)
            {
                //no next closest way was found while not all ways were used. this can be because:
                //1. a relation consists of multiple areas that aren't connected -> not handled for now
                //2. a relation contains some ways that aren't part of it's hull -> ignore them
                //3. a road wasn't processed from it's starting point
                // -> return to the first point that was processed and see if there are ways left to process
                lastAddedPoint = firstPoint;
                closestWay = FindClosestWay(ways, lastAddedPoint, out reverse);
                endReached = true;
            }
        }
        while (closestWay.Points != null);

        //calculate shape center if no center point was specified (some mapObjects have center nodes, those have priority over geometrical center)
        if (center == default)
        {
            shape.Center = CalculateCenter(shape.Points);
        }
        else
        {
            shape.Center = center * (_TileResolution / 100.0f);
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

    private WayData FindClosestWay(List<WayData> ways, Vector2 referencePoint, out bool reverse)
    {
        reverse = false;

        if (ways == null || ways.Count == 0)
        {
            return default;
        }

        WayData closestWay = ways[0];
        Vector2 closestPoint = closestWay.Points[0];

        float sqrDistanceToClosestPoint = (referencePoint - closestPoint).sqrMagnitude;
        foreach (WayData way in ways)
        {
            Vector2 point = way.Points[0];
            float sqrDistance = (point - referencePoint).sqrMagnitude;
            if ( sqrDistance < sqrDistanceToClosestPoint )
            {
                reverse = false;
                closestWay = way;
                sqrDistanceToClosestPoint = sqrDistance;
            }

            point = way.Points[way.Points.Count - 1];
            sqrDistance = (point - referencePoint).sqrMagnitude;
            if ( sqrDistance < sqrDistanceToClosestPoint )
            {
                reverse = true;
                closestWay = way;
                sqrDistanceToClosestPoint = sqrDistance;
            }
        }

        if (sqrDistanceToClosestPoint < _SqrMagnitudeLargestWayGap)
        {
            return closestWay;
        }
        return default;
    }
}





