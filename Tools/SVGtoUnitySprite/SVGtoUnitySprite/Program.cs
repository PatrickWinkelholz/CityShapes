using System;
using System.IO;
using System.Xml;

namespace SVGtoUnitySprite
{
    class Program
    {
        enum OutputMode
        {
            SPRITE,
            CITYDATA
        }

        static OutputMode outputMode = OutputMode.CITYDATA;

        static string inSVGFile = "D:\\GitRepos\\Leipzig\\Tools\\SVGtoUnitySprite\\Bonn_Stadtbezirk_Bonn.svg";

        static string inSpriteFile = "D:\\GitRepos\\Leipzig\\Tools\\SVGtoUnitySprite\\Sprite.png";
        static string outSpritePath = "D:\\GitRepos\\Leipzig\\Leipzig\\Assets\\SVGs";
        static string outSpritePrefix = "shape_";

        static string inCityDataFile = "D:\\GitRepos\\Leipzig\\Tools\\SVGtoUnitySprite\\Bonn.asset";
        static string outCityDataFile = "D:\\GitRepos\\Leipzig\\Leipzig\\Assets\\Data\\Bonn.asset";

        static void Main(string[] args)
        {
            string inSpriteMetaFile = inSpriteFile + ".meta";

            XmlDocument doc = new XmlDocument();
            doc.Load(inSVGFile);

            XmlNodeList polyPaths = doc.SelectNodes("//*[local-name()='path']");
            int i = 0;
            string districts = "Districts:";
            foreach (XmlNode polyPath in polyPaths)
            {
                foreach (XmlAttribute attribute in polyPath.Attributes)
                {
                    if (attribute.Name == "d")
                    {
                        switch (outputMode)
                        {
                            case OutputMode.CITYDATA:

                                districts += "\n  - Name: \n    Shape:\n    ";
                                string[] points = attribute.Value.Split(' ');

                                //bool skipFirst = false; //this is only necessary because for some reason the first point in the bonn svg makes no sense in each path

                                float[] previousCoords = { 0, 0};

                                foreach (string point in points)
                                {
                                    string[] coords = point.Trim().Split(',');
                                    if (coords.Length < 2)
                                    {
                                        continue;
                                    }

                                    //if (!skipFirst)
                                    //{
                                    //    skipFirst = true;
                                    //    continue;
                                    //}

                                    float[] floatCoords = { float.Parse(coords[0]), float.Parse(coords[1]) };
                                    floatCoords[0] += previousCoords[0];
                                    floatCoords[1] += previousCoords[1];

                                    districts += "- {x: " + floatCoords[0] + ", y: " + floatCoords[1] + "}\n    ";
                                    previousCoords = floatCoords;
                                }
                                districts = districts.TrimEnd();
                                break;
                            case OutputMode.SPRITE:

                                string outSVGFile = outSpritePath + "\\" + outSpritePrefix + i.ToString() + ".png";
                                string outSVGMetaFile = outSpritePath + "\\" + outSpritePrefix + i.ToString() + ".png.meta";

                                File.Copy(inSpriteFile, outSVGFile, true);

                                string outline = processPoints("outline:", attribute.Value);
                                string physicsShape = processPoints("physicsShape:", attribute.Value);

                                string SVGMetaFileText = File.ReadAllText(inSpriteMetaFile);
                                SVGMetaFileText = SVGMetaFileText.Replace("outline:", outline).Replace("physicsShape:", physicsShape);
                                File.WriteAllText(outSVGMetaFile, SVGMetaFileText);

                                break;
                        }
                    }
                }
                i++;
            }
            if (outputMode == OutputMode.CITYDATA)
            {
                string cityDataFileText = File.ReadAllText(inCityDataFile);
                File.WriteAllText(outCityDataFile, cityDataFileText.Replace("Districts:", districts));
            }
        }

        static string processPoints(string name, string pointsString)
        {
            name += "\n    - ";
            string[] points = pointsString.Split(' ');
            foreach (string point in points)
            {
                string[] coords = point.Trim().Split(',');
                if (coords.Length < 2)
                {
                    continue;
                }
                name += "- {x: " + coords[0] + ", y: " + coords[1] + "}\n      ";
            }
            return name.Trim();
        }
    }
}
