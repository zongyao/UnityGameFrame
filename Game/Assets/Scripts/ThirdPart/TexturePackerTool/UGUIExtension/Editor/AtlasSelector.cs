using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using System;

public class AtlasSelector : ScriptableWizard
{
    private static AtlasSelector instance = null;

    //双击时间阈值
    private const float DoubleClickThreshold = 0.3f;
    //外观参数
    private const float CellSize = 160.0f;
    private const float Padding = 10.0f;
    private const float NameHeight = 40.0f;
    private const float PaddedCellWidth = CellSize + Padding;
    private const float PaddedCellHeight = CellSize + Padding + NameHeight;

    private Color backgroundColor = new Color(1f, 1f, 1f, 0.5f);
    private Color contentColor = new Color(1f, 1f, 1f, 1.0f);

    private Vector2 scrollPosition = Vector2.zero;
    private float timer = 0.0f;
    private string filter = "";

    private Atlas selectedAtlas = null;
    private UnityAction<Atlas> onClickAtlas = null;
    private UnityAction<Atlas> onDoubleClickAtlas = null;

    List<Atlas> atlases = null;
    List<Atlas> atlasesToRemove = null;
    List<Atlas> filteredAtlases = null;
    private float scale = 1.0f;

    void OnEnable()
    {
        instance = this;
        atlasesToRemove = new List<Atlas>();
        atlases = new List<Atlas>(Resources.FindObjectsOfTypeAll<Atlas>());
        SortAtlases();
        filteredAtlases = new List<Atlas>(atlases);
    }

    void OnDisable()
    {
        instance = null;
    }

    void OnGUI()
    {
        if (atlases == null || atlases.Count == 0)
        {
            GUILayout.Label("No Atlas found, please create an Atlas first.", "LODLevelNotifyText");
        }
        else
        {
            GUILayout.Label("Atlases in Project", "LODLevelNotifyText");
            EditorGUIDrawer.DrawSeparator();

            EditorGUI.BeginChangeCheck();
            filter = EditorGUIDrawer.DrawSearchBox(filter);

            if (EditorGUI.EndChangeCheck() == true)
            {
                FilterAtlases(filter);
            }

            int columns = Mathf.FloorToInt(Screen.width * scale / PaddedCellWidth);
            columns = columns < 1 ? 1 : columns;
            int rows = (int)Mathf.CeilToInt((float)atlases.Count / columns);
            rows = rows < 1 ? 1 : rows;

            GUILayout.Space(10.0f);

            using (GUILayout.ScrollViewScope scrollViewScope = new GUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollViewScope.scrollPosition;

                GUILayout.Space(rows * PaddedCellHeight);

                for (int i = 0; i < filteredAtlases.Count; i++)
                {
                    Atlas atlas = filteredAtlases[i];

                    Rect rect = new Rect(PaddedCellWidth * (i % columns) + Padding, PaddedCellHeight * (i / columns) + Padding, CellSize, CellSize);

                    if (atlas == null || atlas.Source == null)
                    {
                        if (atlasesToRemove != null)
                        {
                            atlasesToRemove.Add(atlas);
                        }
                        continue;
                    }

                    if (GUI.Button(rect, ""))
                    {
                        if (Event.current.button == 0)
                        {
                            float delta = Time.realtimeSinceStartup - timer;
                            timer = Time.realtimeSinceStartup;

                            if (onClickAtlas != null)
                            {
                                onClickAtlas(atlas);
                            }

                            if (selectedAtlas == atlas)
                            {
                                if (delta < DoubleClickThreshold)
                                {
                                    if (onDoubleClickAtlas != null)
                                    {
                                        onDoubleClickAtlas(atlas);
                                    }
                                }
                            }
                            else
                            {
                                selectedAtlas = atlas;
                            }

                        }
                    }

                    if (Event.current.type == EventType.Repaint)
                    {
                        Rect clipRect = rect;
                        float aspect = (float)atlas.Source.width / atlas.Source.height;

                        if (aspect != 1.0f)
                        {
                            if (aspect < 1.0f)
                            {
                                float padding = CellSize * (1.0f - aspect) * 0.5f;
                                clipRect.xMin += padding;
                                clipRect.xMax -= padding;
                            }
                            else
                            {
                                float padding = CellSize * (1.0f - 1.0f / aspect) * 0.5f;
                                clipRect.yMin += padding;
                                clipRect.yMax -= padding;
                            }
                        }

                        EditorGUIDrawer.DrawContrastBackground(clipRect);
                        GUI.DrawTexture(rect, atlas.Source, ScaleMode.ScaleToFit);

                        if (atlas == selectedAtlas)
                        {
                            EditorGUIDrawer.DrawOutline(rect, Color.green);
                        }
                        GUI.backgroundColor = backgroundColor;
                        GUI.contentColor = contentColor;
                        GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, NameHeight), atlas.name, "ProgressBarBack");
                        GUI.contentColor = Color.white;
                        GUI.backgroundColor = Color.white;
                    }
                }

                if (atlasesToRemove != null && atlasesToRemove.Count > 0)
                {
                    foreach (var item in atlasesToRemove)
                    {
                        filteredAtlases.Remove(item);
                    }
                }
                atlasesToRemove.Clear();
            }
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Search Atlas", GUILayout.Height(100)) == true)
        {
            LoadAllAtlases();
            FilterAtlases(filter);
        }
    }

    private void SortAtlases()
    {
        if (atlases != null && atlases.Count > 0)
        {
            atlases.Sort(
                new Comparison<Atlas>((atlas1, atlas2) =>
                {
                    return String.CompareOrdinal(atlas1.name, atlas2.name);
                })
            );
        }
    }

    private void FilterAtlases(string searchString)
    {
        if (string.IsNullOrEmpty(searchString) == false && atlases != null && atlases.Count > 0)
        {
            string lowerSearchString = searchString.ToLower();
            filteredAtlases.Clear();
            for (int i = 0; i < atlases.Count; i++)
            {
                if (atlases[i].name.ToLower().Contains(lowerSearchString) == true)
                {
                    filteredAtlases.Add(atlases[i]);
                }
            }
        }
        else
        {
            filteredAtlases = new List<Atlas>(atlases);
        }
    }

    private void LoadAllAtlases()
    {
        if (atlases == null)
        {
            atlases = new List<Atlas>();
        }
        else
        {
            atlases.Clear();
        }

        string[] paths = AssetDatabase.GetAllAssetPaths();
        List<string> assetList = new List<string>();

        for (int i = 0; i < paths.Length; i++)
        {
            if (paths[i].EndsWith(".asset", StringComparison.OrdinalIgnoreCase) == true)
            {
                assetList.Add(paths[i]);
            }
        }

        for (int i = 0; i < assetList.Count; i++)
        {
            EditorUtility.DisplayProgressBar("Searching", "Searching atlases, please wait...", (float)i / assetList.Count);
            UnityEngine.Object target = AssetDatabase.LoadMainAssetAtPath(assetList[i]);
            if (target != null && target is Atlas)
            {
                atlases.Add(target as Atlas);
            }
        }
        EditorUtility.ClearProgressBar();

        SortAtlases();
    }

    public static void Show(UnityAction<Atlas> onClickAtlas = null, UnityAction<Atlas> onDoubleClickAtlas = null, Atlas selectedAtlas = null)
    {
        if (instance != null)
        {
            instance.Close();
            instance = null;
        }

        instance = ScriptableWizard.DisplayWizard<AtlasSelector>("Select an atlas");
        instance.onClickAtlas = onClickAtlas;
        instance.onDoubleClickAtlas = onDoubleClickAtlas;
        instance.selectedAtlas = selectedAtlas;
        instance.scale = 96 / Screen.dpi;
    }

    public static void Hide()
    {
        if (instance != null)
        {
            instance.Close();
            instance = null;
        }
    }
}