using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using UnityEditor.ProjectWindowCallback;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using LitJson;

//本工具供本地打包使用
public class ABTools : EditorWindow
{
   public string assetBundlePath = "";
    public string abVesionPath = "";
    public AssetBundleVerson abVesion = new AssetBundleVerson();

    public string version = "";

    public bool isVersionChange = false;
    public bool isManualVersion = false;

    public Dictionary<string, string> curAbInfoDic;

     public Dictionary<string, string> localAbInfoDic;

    [MenuItem("Tools/ABTools", false, 80)]
    static void Init()
    {
        ABTools aBTools = (ABTools)EditorWindow.GetWindow(typeof(ABTools), false, "ABTools", true);//创建窗口
        aBTools.Show();//展示
    }

    void OnGUI()
    {
       
        if (GUILayout.Button("设置大版本号"))
        {
            isManualVersion = true;
        }
        if (isManualVersion)
        {
            string localVersion = "";
             abVesionPath = string.Format("{0}{1}{2}", Application.dataPath, "/AssetBundles/","/ABVersion.text");
            if (File.Exists(abVesionPath))
            {
                string localAbVesionText = File.ReadAllText(abVesionPath);
                abVesion = JsonMapper.ToObject<AssetBundleVerson>(localAbVesionText);
                localVersion = abVesion.Version;
            }
            else
            {
                localVersion = "1.0.0";
            }
            version = EditorGUILayout.TextField("version: "+localVersion, version);
        }
        if (GUILayout.Button("默认版本号"))
        {
            isManualVersion = false;
        }
        if (GUILayout.Button("打包"))
        {
            abVesionPath = string.Format("{0}{1}{2}", Application.dataPath, "/AssetBundles/","/ABVersion.text");
            assetBundlePath = string.Format("{0}{1}{2}", Application.dataPath, "/AssetBundles/",ApplicationPlatform.GetPlatformFolder());
           
            DirectoryInfo di = new DirectoryInfo(assetBundlePath);
            curAbInfoDic = new Dictionary<string, string>();
            FindFile(di);
            ModifyDictionary(abVesion.abInfoDic, curAbInfoDic);

            if (!isManualVersion)
            {
                if (File.Exists(abVesionPath))
                {    
                    string localAbVesionText = File.ReadAllText(abVesionPath);
                    abVesion = JsonMapper.ToObject<AssetBundleVerson>(localAbVesionText); 
                    if (isVersionChange)
                    {
                        string localVersion = abVesion.Version;
                        int index = localVersion.LastIndexOf(".");
                        string prefix = localVersion.Substring(0, index + 1);
                        string suffix = (int.Parse(localVersion.Substring(index + 1, localVersion.Length - index - 1)) + 1).ToString();
                        version = prefix + suffix;
                        abVesion.Version = version;
                    }
                }
                else
                {
                    version = "1.0.0";
                    abVesion.Version = version;
                }
            }
            else
            {
                 abVesion.Version = version;
            }
            Debug.Log( abVesion.Version);
            abVesion.abInfoDic = curAbInfoDic;
            string abVesionText = JsonMapper.ToJson(abVesion);
            var utf8WithBom = new System.Text.UTF8Encoding(false);
  
            StreamWriter streamWriter = new StreamWriter(abVesionPath, false, utf8WithBom);
            streamWriter.Write(abVesionText);
            streamWriter.Close();
            isVersionChange = false;
        }
    }

    void FindFile(DirectoryInfo di)
    {
        FileInfo[] fis = di.GetFiles();
        for (int i = 0; i < fis.Length; i++)
        {
            string path = fis[i].FullName;
            int pathIndex = path.IndexOf(ApplicationPlatform.GetPlatformFolder()) +ApplicationPlatform.GetPlatformFolder().Length;
            string abPathKey = path.Substring(pathIndex+1 , path.Length - pathIndex-1 );
            if ( !path.EndsWith(".meta") && !path.EndsWith("ABVersion.text") )
            {
                StreamReader streamReader = new StreamReader(path, Encoding.UTF8);
                string str = streamReader.ReadToEnd();
                streamReader.Close();
                byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
                string md5 = MD5Util.GetBytesMD5(data);
                curAbInfoDic.Add(abPathKey, md5);
                RefreshDictionary(abVesion.abInfoDic, abPathKey, md5);
            }
        }
        DirectoryInfo[] dis = di.GetDirectories();
        for (int j = 0; j < dis.Length; j++)
        {
            FindFile(dis[j]);
        }
    }

    void RefreshDictionary(Dictionary<string, string> dic, string key, string value)
    {
        if (dic.ContainsKey(key))
        {
            if (dic[key] != value)
            {
                isVersionChange = true;
            }
        }
        else
        {
            isVersionChange = true;
        }
    }

    void ModifyDictionary(Dictionary<string, string> dic, Dictionary<string, string> newDic)
    {
        List<string> keyList = new List<string>();
        for (var center = dic.Keys.GetEnumerator(); center.MoveNext();)
        {
            if (!newDic.ContainsKey(center.Current))
            {
                keyList.Add(center.Current);
                isVersionChange = true;
            }
        }
        for (int i = 0; i < keyList.Count; i++)
        {
            dic.Remove(keyList[i]);
        }
    }
}
