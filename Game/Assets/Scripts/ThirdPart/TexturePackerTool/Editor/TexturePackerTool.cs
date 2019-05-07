using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class TexturePackerTool
{
    //TexturePacker.exe路径
    public readonly static string TexturePackerExePath = "TexturePackerTool/TexturePacker/TexturePacker.exe";

    public enum InputType
    {
        RelativePath = 0,
        AbsolutePath
    }

    //获取指定路径的图片的九宫格信息
    private Dictionary<string, SpriteMetaData> GetSpriteMetaDataFromTexture(string texturePath)
    {
        //图集不存在，返回空
        if (File.Exists(texturePath) == false)
        {
            return null;
        }
        //获取图集的TextureImporter，不存在则返回空
        TextureImporter textureImporter = TextureImporter.GetAtPath(texturePath) as TextureImporter;
        if (textureImporter == null)
        {
            EditorUtility.DisplayDialog("Error", "Saving borders faild. Packing abort.", "OK");
            return null;
        }
        //获取图集九宫格信息，并保存至九宫格地图
        SpriteMetaData[] metaDatas = textureImporter.spritesheet;
        Dictionary<string, SpriteMetaData> spriteMetaDataMap = new Dictionary<string, SpriteMetaData>();
        for (int i = 0; i < metaDatas.Length; i++)
        {
            SpriteMetaData metaData = metaDatas[i];
            spriteMetaDataMap.Add(metaData.name, metaData);
        }
        return spriteMetaDataMap;
    }

    private string GenerateCommand(string[] texturePaths, string outputPath, string atlasName, int type = 0)
    {
        if (texturePaths != null && texturePaths.Length > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("--format xml ");//设置使用unity格式
            sb.Append("--data \"" + Path.Combine(Application.dataPath, Path.Combine(outputPath, atlasName)) + ".xml\" ");//数据文件名
            sb.Append("--sheet \"" + Path.Combine(Application.dataPath, Path.Combine(outputPath, atlasName)) + ".png\" ");//输出图片名
            sb.Append("--size-constraints POT ");
            sb.Append("--trim-mode None ");
            sb.Append("--max-size 2048 ");
            sb.Append("--disable-rotation ");//禁用旋转

            switch ((InputType)type)
            {
                case InputType.RelativePath:
                    for (int i = 0; i < texturePaths.Length; i++)
                    {
                        sb.Append("\"");
                        sb.Append(System.Environment.CurrentDirectory + "/" + texturePaths[i] + "\" ");
                    }
                    break;
                case InputType.AbsolutePath:
                    for (int i = 0; i < texturePaths.Length; i++)
                    {
                        sb.Append("\"");
                        sb.Append(texturePaths[i] + "\" ");
                    }
                    break;
            }

            sb.Replace('/', '\\');
            return sb.ToString();
        }
        else
        {
            return "";
        }
    }

    private string GetExePath()
    {
        string exePath = Path.Combine(Application.dataPath, TexturePackerExePath).Replace('/', '\\');

        if (File.Exists(exePath) == true)
        {
            return exePath;
        }

        string assetPath = "";
        string[] guids = AssetDatabase.FindAssets("TexturePacker");
        foreach (var item in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(item);
            if (path.EndsWith("TexturePacker.exe", true, System.Globalization.CultureInfo.CurrentCulture))
            {
                assetPath = path;
                break;
            }
        }
        exePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath).Replace('/', '\\');
        return exePath;
    }

    private bool RunTexturePackerCommand(string command)
    {
        string exePath = GetExePath();

        System.Diagnostics.Process process = new System.Diagnostics.Process();
        process.StartInfo.FileName = exePath.Replace('/', '\\');
        process.StartInfo.Arguments = command;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
        process.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
        process.StartInfo.RedirectStandardError = true;//重定向标准错误输出
        process.StartInfo.CreateNoWindow = true;//不显示程序窗口
        process.Start();//启动程序

        string error = process.StandardError.ReadToEnd();
        if (error.Contains("TexturePacker:: error:"))
        {
            UnityEngine.Debug.LogError(error);
            EditorUtility.DisplayDialog("TexturePacker Error", error, "OK");
            return false;
        }
        EditorUtility.DisplayDialog("Pack success", process.StandardOutput.ReadToEnd(), "OK");
        process.WaitForExit();
        process.Close();
        return true;
    }

    /// <summary>
    /// 打包，所有路径均相对于Assets目录
    /// </summary>
    public void PackTexture(string[] texturePaths, string outputPath, string atlasName, int type = 0)
    {
        string unityOutputPath = Path.Combine("Assets", outputPath);

        string unityAtlasPath = Path.Combine(unityOutputPath, atlasName) + ".png";//输出图集路径
        unityAtlasPath = unityAtlasPath.Replace('\\', '/');

        string unityAtlasDataPath = Path.Combine(unityOutputPath, atlasName) + ".xml";//输出图集信息路径
        unityAtlasDataPath = unityAtlasDataPath.Replace('\\', '/');
        string unityAtlasDataMetaPath = unityAtlasDataPath + ".meta";

        string unityAtlasObjectPath = Path.Combine(unityOutputPath, atlasName) + ".asset";//输出图集数据路径
        unityAtlasObjectPath = unityAtlasObjectPath.Replace('\\', '/');

        //存储原先的九宫格信息
        Dictionary<string, SpriteMetaData> restoredSpriteMetaDataMap = GetSpriteMetaDataFromTexture(unityAtlasPath);
        DeleteFileIfExists(unityAtlasObjectPath);
        
        string command = GenerateCommand(texturePaths, outputPath, atlasName, type);
        if (RunTexturePackerCommand(command) == true)
        {
            string xml = File.ReadAllText(unityAtlasDataPath.Replace("Assets", Application.dataPath));
            if (string.IsNullOrEmpty(xml) == false)
            {
                TexturePackerImporter texturePackerImporter = new TexturePackerImporter();
                var importData = texturePackerImporter.Parse(xml);
                if (importData != null)
                {
                    //还原九宫格信息
                    if (restoredSpriteMetaDataMap != null)
                    {
                        for (int i = 0; i < importData.SpriteMetaDatas.Length; i++)
                        {
                            string spriteName = importData.SpriteMetaDatas[i].name;
                            if (restoredSpriteMetaDataMap.ContainsKey(spriteName) == true)
                            {
                                SpriteMetaData restoredData = restoredSpriteMetaDataMap[spriteName];
                                importData.SpriteMetaDatas[i].border = restoredData.border;
                                importData.SpriteMetaDatas[i].alignment = restoredData.alignment;
                                importData.SpriteMetaDatas[i].pivot = restoredData.pivot;
                            }
                        }
                    }
                    TextureImporter textureImporter = AssetImporter.GetAtPath(unityAtlasPath) as TextureImporter;
                    if (textureImporter == null)
                    {
                        AssetDatabase.Refresh();
                        textureImporter = AssetImporter.GetAtPath(unityAtlasPath) as TextureImporter;
                    }
                    if (textureImporter != null)
                    {
                        textureImporter.textureType = TextureImporterType.Sprite;
                        textureImporter.alphaIsTransparency = true;
                        textureImporter.spriteImportMode = SpriteImportMode.Multiple;
                        textureImporter.mipmapEnabled = false;
                        textureImporter.spritesheet = importData.SpriteMetaDatas;
                        textureImporter.SaveAndReimport();
                    }
                    //获取所有的Sprite，根据结果确定是否需要创建图集对象
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(unityAtlasPath);
                    if (sprites != null && sprites.Length > 0)
                    {
                        Texture atlas = AssetDatabase.LoadAssetAtPath<Texture>(unityAtlasPath);
                        Atlas atlasObject;
                        if (File.Exists(unityAtlasObjectPath) == true)
                        {
                            atlasObject = AssetDatabase.LoadAssetAtPath<Atlas>(unityAtlasObjectPath);
                            atlasObject.Initialize(atlas);
                        }
                        else
                        {
                            atlasObject = ScriptableObject.CreateInstance<Atlas>();
                            atlasObject.Initialize(atlas);
                            AssetDatabase.CreateAsset(atlasObject, unityAtlasObjectPath);
                            AssetDatabase.SaveAssets();
                        }

                        DeleteFileIfExists(unityAtlasDataPath);
                        DeleteFileIfExists(unityAtlasDataMetaPath);
                        AssetDatabase.Refresh();
                    }
                }
            }
        }
    }

    public void DeleteFileIfExists(string path)
    {
        if (File.Exists(path) == true)
        {
            File.Delete(path);
        }
    }
}