using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System;
using System.IO;
using System.Text;
using UnityEngine.Networking;
public class CheckAndDownloadAB : MonoBehaviour
{
    private AssetBundleVerson serverAbVesion = null;
    private AssetBundleVerson pAbVesion = null;
    private AssetBundleVerson sAbVesion = null;
    private string url = "http:/http://127.0.0.1:8080/UnityAssets/AssetBundles/";
    private string serverAbVersionUrl = "";
    private string sAbVesionPath = "";
    private string pAbVesionPath = "";

    private string localABVersionPath = "/ABVersion.text";

    void Awake()
    {
        pAbVesionPath = string.Format("{0}{1}{2}{3}", Application.persistentDataPath, "/AssetBundles/", ApplicationPlatform.GetPlatformFolder(), localABVersionPath);
        sAbVesionPath = string.Format("{0}{1}{2}{3}", Application.streamingAssetsPath, "/AssetBundles/", ApplicationPlatform.GetPlatformFolder(), localABVersionPath);
        serverAbVersionUrl = string.Format("{0}{1}{2}", url, ApplicationPlatform.GetPlatformFolder(), localABVersionPath);
    }

    void Start()
    {
        if (File.Exists(pAbVesionPath))
        {
            string pAbVesionText = File.ReadAllText(pAbVesionPath);
            pAbVesion = JsonMapper.ToObject<AssetBundleVerson>(pAbVesionText);
        }

        if (File.Exists(sAbVesionPath))
        {
            string sAbVesionText = File.ReadAllText(sAbVesionPath);
            sAbVesion = JsonMapper.ToObject<AssetBundleVerson>(sAbVesionText);
        }
        //通过http请求服务器数据
        UnityWebRequestProcessor.Instance.GetText(serverAbVersionUrl, OnLoadABVersionFinish);
    }


    void OnLoadABVersionFinish(string text)
    {
        if (text == null)
        {
             Debug.LogError("OnLoadABVersionFinish text==null");
            return;
        }
        else
        {
            serverAbVesion = JsonMapper.ToObject<AssetBundleVerson>(text);
            if (serverAbVesion == null)
            {
                Debug.LogError("OnLoadABVersionFinish serverAbVesion==null");
                return;
            }
            else
            {
                LoadChangeAb();
            }
        }
    }

    void LoadChangeAb()
    {
        if (sAbVesion == null && pAbVesion == null)
        {
            DownloadAllAB();
        }
        else if (sAbVesion == null && pAbVesion != null)
        {
            if (serverAbVesion.Version == pAbVesion.Version)
            {
                return;
            }
            CheckPVersion();
        }
        else if (sAbVesion != null && pAbVesion == null)
        {
            if (serverAbVesion.Version == sAbVesion.Version)
            {
                return;
            }
            CheckSVersion();
        }
        else if (sAbVesion != null && pAbVesion != null)
        {
            if (serverAbVesion.Version == sAbVesion.Version || serverAbVesion.Version == pAbVesion.Version)
            {
                return;
            }
            CheckVersion();
        }
        CreatePAbVesion();
    }


    void CheckVersion()
    {
        //对比下载
        foreach (KeyValuePair<string, string> serverDic in serverAbVesion.abInfoDic)
        {
            if (pAbVesion.abInfoDic.ContainsKey(serverDic.Key))
            {
                if (pAbVesion.abInfoDic[serverDic.Key] != serverDic.Value)
                {
                    DownloadABFile(serverDic.Key);
                    RefreshPABVersionDic(serverDic.Key, serverDic.Value);
                }
            }
            else
            {
                if (sAbVesion.abInfoDic.ContainsKey(serverDic.Key))
                {
                    if (sAbVesion.abInfoDic[serverDic.Key] != serverDic.Value)
                    {
                        DownloadABFile(serverDic.Key);
                        RefreshPABVersionDic(serverDic.Key, serverDic.Value);
                    }
                }
                else
                {
                    DownloadABFile(serverDic.Key);
                    RefreshPABVersionDic(serverDic.Key, serverDic.Value);
                }
            }
        }
    }
    void CheckPVersion()
    {
        foreach (KeyValuePair<string, string> serverDic in serverAbVesion.abInfoDic)
        {
            if (pAbVesion.abInfoDic.ContainsKey(serverDic.Key))
            {
                if (pAbVesion.abInfoDic[serverDic.Key] != serverDic.Value)
                {
                    DownloadABFile(serverDic.Key);
                    RefreshPABVersionDic(serverDic.Key, serverDic.Value);
                }
            }
            else
            {
                DownloadABFile(serverDic.Key);
                RefreshPABVersionDic(serverDic.Key, serverDic.Value);
            }
        }
    }

    void CheckSVersion()
    {
        foreach (KeyValuePair<string, string> serverDic in serverAbVesion.abInfoDic)
        {
            if (sAbVesion.abInfoDic.ContainsKey(serverDic.Key))
            {
                if (sAbVesion.abInfoDic[serverDic.Key] != serverDic.Value)
                {
                    DownloadABFile(serverDic.Key);
                    RefreshPABVersionDic(serverDic.Key, serverDic.Value);
                }
            }
            else
            {
                DownloadABFile(serverDic.Key);
                RefreshPABVersionDic(serverDic.Key, serverDic.Value);
            }
        }
    }

    void DownloadAllAB()
    {
        foreach (KeyValuePair<string, string> serverDic in serverAbVesion.abInfoDic)
        {
            RefreshPABVersionDic(serverDic.Key, serverDic.Value);
            DownloadABFile(serverDic.Key);
        }
    }

    void DownloadABFile(string path)
    {
        string serverAbUrl = string.Format("{0}{1}{2}{3}", url, ApplicationPlatform.GetPlatformFolder(), "/", path);
        string localPAbPath = string.Format("{0}{1}{2}{3}{4}", Application.persistentDataPath, "/AssetBundles/",
                                ApplicationPlatform.GetPlatformFolder(), "/", path);

        UnityWebRequestProcessor.Instance.GetBytes(serverAbUrl, (bytes) =>
        {
            if (bytes != null)
            {
                WriterBytesFile(localPAbPath, bytes);
            }
        });
    }

    void WriterBytesFile(string path, byte[] bytes)
    {
        int index = path.LastIndexOf("/");
        string direPath = path.Substring(0, index);
        if (!Directory.Exists(direPath))
        {
            Directory.CreateDirectory(direPath);
        }
        StreamWriter streamWriter = new StreamWriter(path);
        streamWriter.Write(bytes);
        streamWriter.Close();
    }

    void WriterStringFile(string path, string str)
    {
        int index = path.LastIndexOf("/");
        string direPath = path.Substring(0, index);
        if (!Directory.Exists(direPath))
        {
            Directory.CreateDirectory(direPath);
        }
        File.WriteAllText(path, str);
    }

    void CreatePAbVesion()
    {
        pAbVesion.Version = serverAbVesion.Version;
        string pAbVesionText = JsonMapper.ToJson(pAbVesion);
        WriterStringFile(pAbVesionPath, pAbVesionText);
    }


    void RefreshPABVersionDic(string key, string value)
    {
        if (pAbVesion == null)
        {
            pAbVesion = new AssetBundleVerson();
        }
        if (pAbVesion.abInfoDic.ContainsKey(key))
        {
            pAbVesion.abInfoDic[key] = value;
        }
        else
        {
            pAbVesion.abInfoDic.Add(key, value);
        }
    }
}

public class ApplicationPlatform
{
    public static string GetPlatformFolder()
    {
        switch (Application.platform)
        {
            case UnityEngine.RuntimePlatform.Android:
                return "Android";
            case UnityEngine.RuntimePlatform.IPhonePlayer:
                return "IOS";
            case UnityEngine.RuntimePlatform.WindowsPlayer:
                return "Windows";
            case UnityEngine.RuntimePlatform.OSXPlayer:
            case UnityEngine.RuntimePlatform.OSXEditor:
                return "OSX";
            default:
                return null;
        }
    }
}