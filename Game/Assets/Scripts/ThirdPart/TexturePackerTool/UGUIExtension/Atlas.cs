using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Atlas : ScriptableObject
{
    /// <summary>
    /// 图集
    /// </summary>
    /// <value>The source.</value>
    public Texture Source { get { return source; } }

    //图集资源
    [SerializeField] protected Texture source = null;

    /// <summary>
    /// Sprites数组
    /// </summary>
    /// <value>The sprites.</value>
    public Sprite[] Sprites
    {
        get { return sprites; }
    }

    [HideInInspector] [SerializeField] protected Sprite[] sprites = null;


    /// <summary>
    /// 图集内Sprite名字
    /// </summary>
    public string[] SpriteNames { get { return spriteNames; } }

    [HideInInspector] [SerializeField] private string[] spriteNames = null;

#if UNITY_EDITOR
    /// <summary>
    /// 从一个Texture资源初始化图集
    /// </summary>
    public void Initialize(Texture atlasSource)
    {
        if (atlasSource == null)
        {
#if UNITY_EDITOR
            Debug.LogFormat("Parameter 'atlasSource' cannot be null when initializing an atlas.");
#endif
            return;
        }
        //获取图集路径
        var path = AssetDatabase.GetAssetPath(atlasSource);
        //加载图集下所有资源
        var targets = AssetDatabase.LoadAllAssetsAtPath(path);
        Array.Sort(targets, (left, right) => { return left.name.CompareTo(right.name); });

        List<Sprite> listSprite = new List<Sprite>();
        List<string> listSpriteNames = new List<string>();
        foreach (var target in targets)
        {
            if (target is Sprite)
            {
                listSprite.Add(target as Sprite);
                listSpriteNames.Add(target.name);
            }
        }
        sprites = listSprite.ToArray();
        spriteNames = listSpriteNames.ToArray();

        this.source = atlasSource;
    }
#endif

    /// <summary>
    /// 根据名字获取Sprite；如果参数不包含扩展名，将会尝试添加扩展名后查找；如果还是没有找到，返回null
    /// </summary>
    public Sprite GetSprite(string name)
    {
        if (source == null)
        {
#if UNITY_EDITOR
            Debug.LogFormat("Atlas '{0}' has an invalid source.", name);
#endif
            return null;
        }

        if (sprites == null || sprites.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogFormat("There is no sprite in atlas '{0}'", name, source.name);
#endif
            return null;
        }
        else
        {
            Sprite result = null;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i].name == name || sprites[i].name == name + ".png" || sprites[i].name == name + ".jpg")
                {
                    result = sprites[i];
                    break;
                }
            }
#if UNITY_EDITOR
            if (result == null)
            {
                Debug.LogFormat("Sprite with name '{0}' not found in atlas '{1}'", name, source.name);
            }
#endif
            return result;
        }
    }
}