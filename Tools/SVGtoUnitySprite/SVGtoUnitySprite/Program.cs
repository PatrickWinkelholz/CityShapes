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

        static string inSVGFile = "D:\\GitRepos\\Leipzig\\Resources\\svgs\\Leipzig_Ortsteil_24_Paunsdorf.svg";

        static string inSpriteFile = "D:\\GitRepos\\Leipzig\\Tools\\SVGtoUnitySprite\\Sprite.png";
        static string outSpritePath = "D:\\GitRepos\\Leipzig\\Leipzig\\Assets\\SVGs";
        static string outSpritePrefix = "shape_";

        static string inCityDataFile = "D:\\GitRepos\\Leipzig\\Tools\\SVGtoUnitySprite\\CityData.asset";
        static string outCityDataFile = "D:\\GitRepos\\Leipzig\\Leipzig\\Assets\\Data\\CityData.asset";

        static void Main(string[] args)
        {
            string inSpriteMetaFile = inSpriteFile + ".meta";

            XmlDocument doc = new XmlDocument();
            doc.Load(inSVGFile);

            XmlNodeList polyPaths = doc.SelectNodes("//*[local-name()='polyline']");
            int i = 0;
            string districts = "Districts:";
            foreach (XmlNode polyPath in polyPaths)
            {
                foreach (XmlAttribute attribute in polyPath.Attributes)
                {
                    if (attribute.Name == "points")
                    {
                        switch (outputMode)
                        {
                            case OutputMode.CITYDATA:

                                districts += "\n  - Name: \n    Shape:\n    ";
                                string[] points = attribute.Value.Split(' ');
                                foreach (string point in points)
                                {
                                    string[] coords = point.Trim().Split(',');
                                    if (coords.Length < 2)
                                    {
                                        continue;
                                    }
                                    districts += "- {x: " + coords[0] + ", y: " + coords[1] + "}\n    ";
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
