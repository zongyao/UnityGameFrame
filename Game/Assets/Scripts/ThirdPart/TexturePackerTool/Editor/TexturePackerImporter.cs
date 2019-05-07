using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEditor;

public class TexturePackerImporter
{
    public class TexturePackerExportData
    {
        public string ImagePath { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public SpriteMetaData[] SpriteMetaDatas { get; private set; }

        public TexturePackerExportData(string imagePath, float width, float height, SpriteMetaData[] spriteMetaDatas)
        {
            ImagePath = imagePath;
            Width = width;
            Height = height;
            SpriteMetaDatas = spriteMetaDatas;
        }
    }

    public TexturePackerExportData Parse(string dataXml)
    {
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(dataXml);
        if (xmlDocument != null)
        {
            XmlElement textureAtlasNode = xmlDocument.SelectSingleNode("TextureAtlas") as XmlElement;
            if (textureAtlasNode != null)
            {
                return ParseTextureAtlasNode(textureAtlasNode);
            }
        }
        return null;
    }

    private TexturePackerExportData ParseTextureAtlasNode(XmlElement textureAtlasNode)
    {
        if (textureAtlasNode != null)
        {
            string imagePath = textureAtlasNode.GetAttribute("imagePath");
            float width = 0;
            float height = 0;
            List<SpriteMetaData> spriteMetaDatas = new List<SpriteMetaData>();

            float.TryParse(textureAtlasNode.GetAttribute("width"), out width);
            float.TryParse(textureAtlasNode.GetAttribute("height"), out height);

            var spriteNodes = textureAtlasNode.SelectNodes("sprite");
            if (spriteNodes != null && spriteNodes.Count > 0)
            {
                foreach (XmlElement node in spriteNodes)
                {
                    string name = node.GetAttribute("n");
                    float x = 0, y = 0, w = 0, h = 0, pX = 0.5f, pY = 0.5f;

                    float.TryParse(node.GetAttribute("x"), out x);
                    float.TryParse(node.GetAttribute("y"), out y);
                    float.TryParse(node.GetAttribute("w"), out w);
                    float.TryParse(node.GetAttribute("h"), out h);
                    float.TryParse(node.GetAttribute("pX"), out pX);
                    float.TryParse(node.GetAttribute("pY"), out pY);
                    //需要翻转y坐标
                    y = height - y - h;

                    SpriteMetaData data = new SpriteMetaData();
                    data.name = name;
                    data.rect = new Rect(x, y, w, h);
                    data.pivot = new Vector2(pX, pY);
                    spriteMetaDatas.Add(data);
                }
            }
            return new TexturePackerExportData(imagePath, width, height, spriteMetaDatas.ToArray());
        }
        return null;
    }
}