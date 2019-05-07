using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TexturePackerToolWindow : EditorWindow
{
    private readonly string[] sourceType = { "Project", "Folder" };
    private int selectedSourceType = 0;

    private string inputFolderPath = "";

    private string outputPath = "TexturePackerExport/";
    private string atlasName = "out";

    [MenuItem("Assets/TexturePackerTool", false, -100)]
    private static void Open()
    {
        EditorWindow.GetWindow<TexturePackerToolWindow>("TexturePacker Tool");
    }

    void OnEnable()
    {
        if (PlayerPrefs.HasKey("TexturePackerToolOutputPath") == true)
        {
            outputPath = PlayerPrefs.GetString("TexturePackerToolOutputPath");
        }
        if (PlayerPrefs.HasKey("TexturePackerToolAtlasName") == true)
        {
            atlasName = PlayerPrefs.GetString("TexturePackerToolAtlasName");
        }
    }

    void OnDisable()
    {
        PlayerPrefs.SetString("TexturePackerToolOutputPath", outputPath);
        PlayerPrefs.SetString("TexturePackerToolAtlasName", atlasName);
    }

    void OnGUI()
    {
        selectedSourceType = GUILayout.SelectionGrid(selectedSourceType, sourceType, sourceType.Length, GUILayout.Height(50.0f));

        switch (selectedSourceType)
        {
            case 1:
                EditorGUILayout.BeginHorizontal();
                inputFolderPath = EditorGUILayout.TextField("Input Folder Path", inputFolderPath);
                if (GUILayout.Button("Browse", GUILayout.Width(100.0f)) == true)
                {
                    inputFolderPath = EditorUtility.OpenFolderPanel("Select a folder, all images in this folder will be packed into one atlas.", "", "");
                    if (string.IsNullOrEmpty(inputFolderPath) == false)
                    {
                        atlasName = inputFolderPath.Substring(inputFolderPath.LastIndexOf('/') + 1);
                    }
                }
                EditorGUILayout.EndHorizontal();
                break;
            case 0:
            default:
                break;
        }

        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        atlasName = EditorGUILayout.TextField("Atlas Name", atlasName);

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Pack", GUILayout.Height(100.0f)) == true)
        {
            string[] paths = null;
            switch (selectedSourceType)
            {
                case 0:
                    paths = GetAssetsPathsInSelection<Texture>(SelectionMode.DeepAssets);
                    break;
                case 1:
                    paths = GetImagePathsInFolder(inputFolderPath);
                    break;
                default:
                    break;
            }
            if (paths != null && paths.Length > 0)
            {
                TexturePackerTool texturePackerTool = new TexturePackerTool();
                texturePackerTool.PackTexture(paths, outputPath, atlasName, selectedSourceType);
            }
        }
    }

    /// <summary>
    /// 从选择的所有资源上过滤出指定类型的资源，并返回其路径数组
    /// </summary>
    private string[] GetAssetsPathsInSelection<T>(SelectionMode mode = SelectionMode.Unfiltered) where T : Object
    {
        Object[] selectedObjects;
        selectedObjects = Selection.GetFiltered(typeof(T), mode);
        List<string> result = new List<string>();
        foreach (var item in selectedObjects)
        {
            result.Add(AssetDatabase.GetAssetPath(item));
        }
        return result.ToArray();
    }

    /// <summary>
    /// 从指定目录过滤出图片资源，并返回所有图片的路径
    /// </summary>
    private string[] GetImagePathsInFolder(string path)
    {
        if (string.IsNullOrEmpty(path) == false)
        {
            var paths = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(
                s =>
                s.EndsWith(".png", true, System.Globalization.CultureInfo.CurrentCulture) ||
                s.EndsWith(".jpg", true, System.Globalization.CultureInfo.CurrentCulture) ||
                s.EndsWith(".jpeg", true, System.Globalization.CultureInfo.CurrentCulture)
            );
            return paths.ToArray();
        }
        return null;
    }
}