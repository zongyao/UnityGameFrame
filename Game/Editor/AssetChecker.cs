using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

/// <summary>
/// 资源引用检查工具
/// @wangzhenhai
/// </summary>
namespace RL.Editor
{
    public sealed class AssetCheckerWindow : EditorWindow
    {
        static readonly string TexturePath = "Assets/Editor/Textures/AssetChecker/";

        static Texture2D CollapseTexture2D = null;
        static Texture2D ExpandTexture2D = null;

        static Dictionary<string, AssetInfo> AssetInfoDict = new Dictionary<string, AssetInfo>();
        static Dictionary<string, HashSet<string>> AssetRefDict = new Dictionary<string, HashSet<string>>();

        static AssetInfoComparer comparer = new AssetInfoComparer();

        Object _clickObject = null;
        Object _selectObject = null;
        Vector2 _scrollViewRect = Vector2.zero;
        GUIStyle _textStyle = null;
        AssetInfo _assetRef = null;

        [MenuItem("Window/Asset Checker", priority = 2)]
        static void Open()
        {
            AssetCheckerWindow window = GetWindow<AssetCheckerWindow>();
            window.wantsMouseMove = true;
            window.titleContent = new GUIContent("Asset Checker");
            window.minSize = new Vector2(400f, 200f);
            window.Show();
            window.Focus();
        }

        void OnGUI()
        {
            float width = position.width;
            float height = position.height;
            int assetCount = AssetInfoDict.Count;

            Object newSelectObject = EditorGUILayout.ObjectField(_selectObject, typeof(Object), false);

            EditorGUILayout.BeginHorizontal();

            if (GUI.Button(new Rect(width / 2f - 60f, 25f, 120f, 30f), string.Format("Update <{0}>", assetCount)))
            {
                UpdateAssets();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.BeginArea(new Rect(5f, 55f, width - 5f, height - 30f));
            EditorGUILayout.Separator();
            _scrollViewRect = EditorGUILayout.BeginScrollView(_scrollViewRect, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (newSelectObject != null && newSelectObject != _selectObject && assetCount > 0)
            {
                _assetRef = GetAssetInfo(_selectObject = newSelectObject);

                if(_assetRef != null)
                {
					var list = GetAssetRefs (_assetRef.sGUID);
					foreach(var asset in list)
					{
						_ExtraOp (asset.sPath);
					}
                }
            }

            if (_assetRef != null)
            {
                int index = -1;
                UpdateScrollView(_assetRef, 0, ref index);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            OnClickAsset();
        }

        void OnClickAsset()
        {
            if (_clickObject != null)
            {
                Object curObject = _clickObject;
                _clickObject = null;

                if (AssetDatabase.Contains(curObject))
                {
                    EditorApplication.ExecuteMenuItem("Window/Project");
                    EditorUtility.FocusProjectWindow();
                }

                if (curObject is GameObject && AssetDatabase.Contains(curObject))
                {
                    GameObject go = curObject as GameObject;
                    while (go.transform.parent != null)
                    {
                        go = go.transform.parent.gameObject;
                        ProjectWindowUtil.ShowCreatedAsset(go);
                        EditorGUIUtility.PingObject(go);
                    }
                }

                Selection.activeObject = curObject;
                EditorGUIUtility.PingObject(curObject);
            }
        }

        void UpdateScrollView(AssetInfo asset, int next, ref int index)
        {
            if (string.IsNullOrEmpty(asset.sGUID))
            {
                return;
            }

            index++;

            List<AssetInfo> references = GetAssetRefs(asset.sGUID);

            EditorGUILayout.BeginHorizontal(GUILayout.Height(24f));
            GUILayout.Space(next * 24f);

            int refCount = references.Count;
            bool opened = asset.bOpend;

            if (refCount > 0)
            {
                GUIStyle style = GUIStyle.none;
                style.margin.top = 5;

                if (CollapseTexture2D == null)
                {
                    CollapseTexture2D = AssetDatabase.LoadAssetAtPath(TexturePath + "collapse.png", typeof(Texture2D)) as Texture2D;
                }

                if (ExpandTexture2D == null)
                {
                    ExpandTexture2D = AssetDatabase.LoadAssetAtPath(TexturePath + "expand.png", typeof(Texture2D)) as Texture2D;
                }

                if (GUILayout.Button(opened ? CollapseTexture2D : ExpandTexture2D, style, GUILayout.Width(24f)))
                {
                    opened = !opened;
                    asset.bOpend = opened;
                }
            }
            else
            {
                GUILayout.Space(28f);
            }

            if (_textStyle == null)
            {
                _textStyle = new GUIStyle { richText = true, margin = new RectOffset(0, 0, 5, 0), normal = { textColor = Color.white } };
            }

            Texture2D icon = GetIcon(asset.sPath);
            GUILayout.Label(icon, GUILayout.Width(20f), GUILayout.Height(20f));
            GUILayout.Label(string.Format("{0} <b><color=#00ff00ff>{1} 个引用</color></b>", Path.GetFileNameWithoutExtension(asset.sPath), refCount), _textStyle, GUILayout.Width(250f));
            GUILayout.Label(asset.sPath, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();

            if (Event.current.clickCount == 2 && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                _clickObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(asset.sGUID), typeof(Object));
            }

            if (opened)
            {
                foreach (AssetInfo reference in references)
                {
                    UpdateScrollView(reference, next + 1, ref index);
                }
            }
        }

        //执行额外操作
        void _ExtraOp(string path)
        {
			return;
        	try
        	{
				GameObject go = AssetDatabase.LoadAssetAtPath<GameObject> (path);
				GameObject newGo = GameObject.Instantiate(go);
				UnityEngine.UI.Text[] texts = newGo.GetComponentsInChildren<UnityEngine.UI.Text> (true);
				foreach(var t in texts)
				{
					t.font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
				}
				PrefabUtility.ReplacePrefab(newGo, go);

        	}catch(Exception e)
        	{
        		
        	}
			
        }

        void UpdateAssets()
        {
            AssetInfoDict.Clear();

            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            if (assetPaths == null) return;

            int total = assetPaths.Length;
            int current = 0;

            foreach (string assetPath in assetPaths)
            {
                if (++current > total)
                {
                    current = total;
                }

                if (EditorUtility.DisplayCancelableProgressBar("Update", "Please wait ... " + current.ToString() + "/" + total.ToString(), (float)current / total))
                {
                    EditorUtility.ClearProgressBar();
                    AssetInfoDict.Clear();
                    break;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                InsertToAssetInfoDict(guid, assetPath);
                string[] depPaths = AssetDatabase.GetDependencies(new[] { assetPath });
                if (depPaths != null && depPaths.Length > 0)
                {
                    foreach (string depPath in depPaths)
                    {
                        if (!string.IsNullOrEmpty(depPath))
                        {
                            string depGuid = AssetDatabase.AssetPathToGUID(depPath);

                            //

                            InsertToAssetInfoDict(depGuid, depPath);

                            if (guid != depGuid)
                            {
                                if (AssetRefDict.ContainsKey(depGuid))
                                {
                                    AssetRefDict[depGuid].Add(guid);
                                }
                                else
                                {
                                    HashSet<string> depHash = new HashSet<string>();
                                    depHash.Add(guid);
                                    AssetRefDict.Add(depGuid, depHash);
                                }
                            }
                        }
                    }
                }

                if (current == total)
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        AssetInfo GetAssetInfo(Object selectObject)
        {
            string path = AssetDatabase.GetAssetPath(selectObject);
            string guid = AssetDatabase.AssetPathToGUID(path);

            if (AssetInfoDict.ContainsKey(guid))
            {
                return AssetInfoDict[guid];
            }

            return null;
        }

        static List<AssetInfo> GetAssetRefs(string guid)
        {
            List<AssetInfo> refList = new List<AssetInfo>();

            if (AssetRefDict.ContainsKey(guid))
            {
                foreach (string refGuid in AssetRefDict[guid])
                {
                    if (AssetInfoDict.ContainsKey(refGuid))
                    {
                        refList.Add(AssetInfoDict[refGuid]);
                    }
                }
            }

            refList.Sort(comparer);

            return refList;
        }

        static void InsertToAssetInfoDict(string guid, string path)
        {
            if (!AssetInfoDict.ContainsKey(guid))
            {
                AssetInfoDict.Add(guid, new AssetInfo(guid, path));
            }
        }

        static Texture2D GetIcon(string fileName)
        {
            var lastDot = fileName.LastIndexOf('.');
            var extension = (lastDot != -1) ? fileName.Substring(lastDot + 1).ToLower() : string.Empty;
            switch (extension)
            {
                case "boo":
                    return EditorGUIUtility.FindTexture("boo Script Icon");
                case "cginc":
                    return EditorGUIUtility.FindTexture("CGProgram Icon");
                case "cs":
                    return EditorGUIUtility.FindTexture("cs Script Icon");
                case "guiskin":
                    return EditorGUIUtility.FindTexture("GUISkin Icon");
                case "js":
                    return EditorGUIUtility.FindTexture("Js Script Icon");
                case "mat":
                    return EditorGUIUtility.FindTexture("Material Icon");
                case "prefab":
                    return EditorGUIUtility.FindTexture("PrefabNormal Icon");
                case "shader":
                    return EditorGUIUtility.FindTexture("Shader Icon");
                case "txt":
                    return EditorGUIUtility.FindTexture("TextAsset Icon");
                case "unity":
                    return EditorGUIUtility.FindTexture("SceneAsset Icon");
                case "asset":
                case "prefs":
                    return EditorGUIUtility.FindTexture("GameManager Icon");
                case "anim":
                    return EditorGUIUtility.FindTexture("Animation Icon");
                case "meta":
                    return EditorGUIUtility.FindTexture("MetaFile Icon");
                case "ttf":
                case "otf":
                case "fon":
                case "fnt":
                    return EditorGUIUtility.FindTexture("Font Icon");
                case "aac":
                case "aif":
                case "aiff":
                case "au":
                case "mid":
                case "midi":
                case "mp3":
                case "mpa":
                case "ra":
                case "ram":
                case "wma":
                case "wav":
                case "wave":
                case "ogg":
                    return EditorGUIUtility.FindTexture("AudioClip Icon");
                case "ai":
                case "apng":
                case "png":
                case "bmp":
                case "cdr":
                case "dib":
                case "eps":
                case "exif":
                case "gif":
                case "ico":
                case "icon":
                case "j":
                case "j2c":
                case "j2k":
                case "jas":
                case "jiff":
                case "jng":
                case "jp2":
                case "jpc":
                case "jpe":
                case "jpeg":
                case "jpf":
                case "jpg":
                case "jpw":
                case "jpx":
                case "jtf":
                case "mac":
                case "omf":
                case "qif":
                case "qti":
                case "qtif":
                case "tex":
                case "tfw":
                case "tga":
                case "tif":
                case "tiff":
                case "wmf":
                case "psd":
                case "exr":
                    return EditorGUIUtility.FindTexture("Texture Icon");
                case "3df":
                case "3dm":
                case "3dmf":
                case "3ds":
                case "3dv":
                case "3dx":
                case "blend":
                case "c4d":
                case "lwo":
                case "lws":
                case "ma":
                case "max":
                case "mb":
                case "mesh":
                case "obj":
                case "vrl":
                case "wrl":
                case "wrz":
                case "fbx":
                    return EditorGUIUtility.FindTexture("Mesh Icon");
                case "asf":
                case "asx":
                case "avi":
                case "dat":
                case "divx":
                case "dvx":
                case "mlv":
                case "m2l":
                case "m2t":
                case "m2ts":
                case "m2v":
                case "m4e":
                case "m4v":
                case "mjp":
                case "mov":
                case "movie":
                case "mp21":
                case "mp4":
                case "mpe":
                case "mpeg":
                case "mpg":
                case "mpv2":
                case "ogm":
                case "qt":
                case "rm":
                case "rmvb":
                case "wmw":
                case "xvid":
                    return EditorGUIUtility.FindTexture("MovieTexture Icon");
                case "colors":
                case "gradients":
                case "curves":
                case "curvesnormalized":
                case "particlecurves":
                case "particlecurvessigned":
                case "particledoublecurves":
                case "particledoublecurvessigned":
                    return EditorGUIUtility.FindTexture("ScriptableObject Icon");
            }
            return EditorGUIUtility.FindTexture("DefaultAsset Icon");
        }
    }

    internal sealed class AssetInfo
    {
        public string sGUID { get; private set; }
        public string sPath { get; private set; }
        public bool bOpend { get; set; }

        public AssetInfo(string guid, string path)
        {
            sGUID = guid;
            sPath = path;
            bOpend = false;
        }
    }

    internal sealed class AssetInfoComparer : IComparer<AssetInfo>
    {
        static readonly string SceneExtension = ".unity";

        public int Compare(AssetInfo left, AssetInfo right)
        {
            bool bSceneFileLeft = left.sPath.EndsWith(SceneExtension, StringComparison.OrdinalIgnoreCase);
            bool bSceneFileRight = right.sPath.EndsWith(SceneExtension, StringComparison.OrdinalIgnoreCase);

            if (bSceneFileLeft == bSceneFileRight)
            {
                return string.Compare(left.sPath, right.sPath, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return bSceneFileLeft == true ? 1 : -1;
            }
        }
    }
}