using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

public class AtlasSpriteSelector : ScriptableWizard
{
    private static AtlasSpriteSelector instance = null;

    //双击时间阈值
    private const float DoubleClickThreshold = 0.3f;
    //单元格尺寸
    private const float CellSize = 80.0f;
    private const float Padding = 10.0f;
    private const float NameHeight = 40.0f;
    private const float PaddedCellWidth = CellSize + Padding;
    private const float PaddedCellHeight = CellSize + Padding + NameHeight;

    private Color backgroundColor = new Color(1f, 1f, 1f, 0.5f);
    private Color contentColor = new Color(1f, 1f, 1f, 1.0f);

    private Vector2 scrollPosition = Vector2.zero;
    //记录点击时间，用来模拟双击
    private float timer = 0;
    private string filter = "";
    //目标图集
    private Atlas targetAtlas;

    //已选择的Sprite
    private Sprite currentSprite = null;
    private UnityAction<Sprite> onClickSprite = null;
    private UnityAction<Sprite> onDoubleClickSprite = null;

    private List<Sprite> filteredSprites = null;

	private float scale = 1.0f;

    void OnEnable()
    {
        instance = this;
        filter = "";
    }

    void OnDisable()
    {
        instance = null;
    }

    void OnGUI()
    {
        if (targetAtlas == null)
        {
            GUILayout.Label("No Atlas selected.", "LODLevelNotifyText");
        }
        else
        {
            GUILayout.Label(targetAtlas.name + " Sprites", "LODLevelNotifyText");
            EditorGUIDrawer.DrawSeparator();

            EditorGUI.BeginChangeCheck();
            filter = EditorGUIDrawer.DrawSearchBox(filter);
            if (EditorGUI.EndChangeCheck() == true)
            {
                FilterSprites(filter);
            }

            int columns = Mathf.FloorToInt(Screen.width * scale / PaddedCellWidth);
            columns = columns < 1 ? 1 : columns;
            int rows = (int)Mathf.CeilToInt((float)filteredSprites.Count / columns);
            rows = rows < 1 ? 1 : rows;

            GUILayout.Space(10.0f);

            using (GUILayout.ScrollViewScope scrollViewScope = new GUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollViewScope.scrollPosition;

                GUILayout.Space(rows * PaddedCellHeight);

                for (int i = 0; i < filteredSprites.Count; i++)
                {

                    Rect rect = new Rect(PaddedCellWidth * (i % columns) + Padding, PaddedCellHeight * (i / columns) + Padding, CellSize, CellSize);

                    Sprite sprite = filteredSprites[i];
                    if (sprite == null)
                    {
                        continue;
                    }

                    if (GUI.Button(rect, ""))
                    {
                        if (Event.current.button == 0)
                        {
                            float delta = Time.realtimeSinceStartup - timer;
                            timer = Time.realtimeSinceStartup;

                            if (onClickSprite != null)
                            {
                                onClickSprite(sprite);
                            }

                            if (currentSprite == sprite)
                            {
                                if (delta < DoubleClickThreshold)
                                {
                                    if (onDoubleClickSprite != null)
                                    {
                                        onDoubleClickSprite(sprite);
                                    }
                                }
                            }
                            else
                            {
                                currentSprite = sprite;
                            }

                        }
                    }

                    if (Event.current.type == EventType.Repaint)
                    {

                        EditorGUIDrawer.DrawContrastBackground(rect);

                        Rect clipRect = rect;
                        float aspect = sprite.rect.width / sprite.rect.height;
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

                        Texture targetTexture = targetAtlas.Source;

                        EditorGUIDrawer.DrawTextureWithPixelCoords(clipRect, targetAtlas.Source, sprite.rect);
                        if (currentSprite == sprite)
                        {
                            EditorGUIDrawer.DrawOutline(rect, Color.green);
                        }

                        GUI.backgroundColor = backgroundColor;
                        GUI.contentColor = contentColor;
                        GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, NameHeight), sprite.name, "ProgressBarBack");
                        GUI.contentColor = Color.white;
                        GUI.backgroundColor = Color.white;
                    }
                }
            }
        }
    }

    private void FilterSprites(string filter)
    {
        if (targetAtlas != null && targetAtlas.Sprites != null && targetAtlas.Sprites.Length > 0)
        {
            if (string.IsNullOrEmpty(filter) == false)
            {
                string lowerFilter = filter.ToLower();
                filteredSprites.Clear();
                for (int i = 0; i < targetAtlas.Sprites.Length; i++)
                {
                    if (targetAtlas.Sprites[i].name.ToLower().Contains(lowerFilter) == true)
                    {
                        filteredSprites.Add(targetAtlas.Sprites[i]);
                    }
                }
            }
            else
            {
                filteredSprites = new List<Sprite>(targetAtlas.Sprites);
            }
        }
        else
        {
            filteredSprites.Clear();
        }
    }

    public static void Show(Atlas atlas, UnityAction<Sprite> onClickSprite = null, UnityAction<Sprite> onDoubleClickSprite = null, Sprite currentSprite = null)
    {
        if (instance != null)
        {
            instance.Close();
            instance = null;
        }
        instance = ScriptableWizard.DisplayWizard<AtlasSpriteSelector>("Select a Sprite");
        instance.targetAtlas = atlas;
        instance.onClickSprite = onClickSprite;
        instance.onDoubleClickSprite = onDoubleClickSprite;
        instance.currentSprite = currentSprite;
        if (instance.targetAtlas != null && instance.targetAtlas.Sprites != null)
        {
            instance.filteredSprites = new List<Sprite>(instance.targetAtlas.Sprites);
        }
        else
        {
            instance.filteredSprites = new List<Sprite>();
        }
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