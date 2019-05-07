using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.Networking;
using LitJson;
using UnityEngine.Events;


public class AssetBundleManager : Singleton<AssetBundleManager>
{
    private string pAbPath = "";
    private string sAbPath = "";
    private AssetBundleVerson pAbVesion = null;
    private AssetBundleVerson sAbVesion = null;
    private AssetBundleManifest sManifest = null;
    private AssetBundleManifest pManifest = null;
    private Dictionary<string, AssetBundle> assetBundleDic = new Dictionary<string, AssetBundle>();

    //要求整体实例化在ab下载完之后
    AssetBundleManager()
    {
        pAbPath = string.Format("{0}{1}{2}{3}", Application.persistentDataPath, "/AssetBundles/", ApplicationPlatform.GetPlatformFolder(), "/");
        sAbPath = string.Format("{0}{1}{2}{3}", Application.streamingAssetsPath, "/AssetBundles/", ApplicationPlatform.GetPlatformFolder(), "/");
        string pAbVesionPath = Path.Combine(pAbPath, "ABVersion.text");
        string sAbVesionPath = Path.Combine(sAbPath, "ABVersion.text");
        string sManifestPath = Path.Combine(sAbPath, ApplicationPlatform.GetPlatformFolder());
        string pManifestPath = Path.Combine(pAbPath, ApplicationPlatform.GetPlatformFolder());

        if (File.Exists(pAbVesionPath))
        {
            string pAbVesionText = File.ReadAllText(pAbVesionPath);
            pAbVesion = JsonMapper.ToObject<AssetBundleVerson>(pAbVesionText);
            if (File.Exists(pManifestPath))
            {
                AssetBundle manifestBundle = AssetBundle.LoadFromFile(pManifestPath);
                pManifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                manifestBundle.Unload(false);
            }
        }

        if (File.Exists(sAbVesionPath))
        {
            string sAbVesionText = File.ReadAllText(sAbVesionPath);
            sAbVesion = JsonMapper.ToObject<AssetBundleVerson>(sAbVesionText);
            if (File.Exists(sManifestPath))
            {
                AssetBundle manifestBundle = AssetBundle.LoadFromFile(sManifestPath);
                sManifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                manifestBundle.Unload(false);
            }
        }
    }

    //获取加载路径persistentDataPath or persistentDataPath
    string loadFilePath(string abName)
    {
        if (pAbVesion != null && sAbVesion != null)
        {
            if (pAbVesion.abInfoDic.ContainsKey(abName))
            {
                string depPath = Path.Combine(pAbPath, abName);
                return depPath;
            }
            else if (sAbVesion.abInfoDic.ContainsKey(abName))
            {
                string depPath = Path.Combine(sAbPath, abName);
                return depPath;
            }
            return null;
        }
        else if (pAbVesion != null && sAbVesion == null)
        {
            if (pAbVesion.abInfoDic.ContainsKey(abName))
            {
                string depPath = Path.Combine(pAbPath, abName);
                return depPath;
            }
            return null;
        }
        else if (pAbVesion == null && sAbVesion != null)
        {
            if (sAbVesion.abInfoDic.ContainsKey(abName))
            {
                string depPath = Path.Combine(sAbPath, abName);
                return depPath;
            }
            return null;
        }
        return null;
    }

    ///
    //异步加载
    ///
    public void LoadAbAssetAsync<T>(
        string abName,
        string assetName,
        Action<T> onFinishAction) where T : UnityEngine.Object
    {
        RefreshAddAssetBundleDic();
        if (assetBundleDic.ContainsKey(abName))
        {
            AssetBundleAssetLoader.Instance.LoadAssetAsync(assetBundleDic[abName], assetName, (asset) =>
            {
                if (asset == null)
                {
                    Debug.LogError("asset is null");
                }
                onFinishAction(asset as T);
            });
        }
        else
        {
            string filePath = loadFilePath(abName);
            string[] depends = GetDependenciesByAbName(abName);
            if (depends.Length == 0)
            {
                LoadAbFromFileAsync(abName, assetName, filePath, onFinishAction);
            }
            else
            {
                Dictionary<string, bool> abNameAllDependsDic = new Dictionary<string, bool>();
                LoadAbDependsAsync(abName, abNameAllDependsDic, () =>
                {
                    LoadAbFromFileAsync(abName, assetName, filePath, onFinishAction);
                });
            }
        }
    }

    void LoadAbFromFileAsync<T>(string abName, string assetName, string filePath, Action<T> onFinishAction) where T : UnityEngine.Object
    {
        AssetBundleLoader.Instance.LoadFromFileAsync(filePath, (assetBundle) =>
        {
            if (assetBundle != null) 
            {
                AddAssetBundleDic(abName, assetBundle);
                AssetBundleAssetLoader.Instance.LoadAssetAsync(assetBundle, assetName, (asset) =>
                {
                    if (asset == null)
                    {
                        Debug.LogError("asset is null");
                    }
                    if (onFinishAction != null)
                    {
                        try
                        {
                            if (asset == null)
                            {
                                Debug.LogError("asset is null");
                            }
                            onFinishAction(asset as T);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                });
            }
        });
    }

    //加载依赖 在所有依赖加载完加载ab
    void LoadAbDependsAsync(string abName, Dictionary<string, bool> abNameAllDependsDic, Action onFinishAction)
    {
        string[] depends = GetDependenciesByAbName(abName);
        foreach (var depName in depends)
        {
            if (!assetBundleDic.ContainsKey(depName))
            {
                string filePath = loadFilePath(depName);
                abNameAllDependsDic.Add(depName, false);
                AssetBundleLoader.Instance.LoadFromFileAsync(filePath, (assetBundle) =>
                {
                    if (assetBundle != null)
                    {
                        AddAssetBundleDic(depName, assetBundle);
                        abNameAllDependsDic[depName] = true;
                      
                        if (CheckAbNameAllDependsLoadFinish(abNameAllDependsDic))
                        {
                            onFinishAction();
                        }
                    }
                });
                LoadAbDependsAsync(depName, abNameAllDependsDic, onFinishAction);
            }
        }
    }

    string[] GetDependenciesByAbName(string abName)
    {
        string[] depends = { };
        if (pAbVesion != null && pAbVesion.abInfoDic.ContainsKey(abName))
        {
            depends = pManifest.GetAllDependencies(abName);
        }
        else if (sAbVesion != null && sAbVesion.abInfoDic.ContainsKey(abName))
        {
            depends = sManifest.GetAllDependencies(abName);
        }
        return depends;
    }

    //检查所有依赖关系是否加载完成
    bool CheckAbNameAllDependsLoadFinish(Dictionary<string, bool> abNameAllDependsDic)
    {
        foreach (KeyValuePair<string, bool> dicITem in abNameAllDependsDic)
        {
            if (!dicITem.Value)
            {
                return false;
            }
        }
        return true;
    }

    ///
    //同步加载
    ///
    public T LoadAbAsset<T>(string abName, string assetName) where T : UnityEngine.Object
    {
        RefreshAddAssetBundleDic();
        if (!assetBundleDic.ContainsKey(abName))
        {
            LoadAssetBundle(abName);
        }
        if (assetBundleDic[abName] != null)
        {
            T asset = assetBundleDic[abName].LoadAsset<T>(assetName);
            if (asset == null)
            {
                Debug.LogError("asset is null");
            }
            return asset;
        }
        else
        {
            Debug.LogError(abName + " assetBundle is null");
            return null;
        }
    }

    void LoadAssetBundle(string abName)
    {
        if (pManifest == null && sManifest != null)
        {
            SVersionAbLoad(abName);
        }
        else if (pManifest == null && sManifest == null)
        {
            Debug.LogError(abName + " assetBundle not exit");
            return;
        }
        else
        {
            PVersionAbLoad(abName);
        }
    }

    void SVersionAbLoad(string abName)
    {
        if (sAbVesion != null && sAbVesion.abInfoDic.ContainsKey(abName))
        {
            string[] depends = sManifest.GetAllDependencies(abName);
            foreach (var depName in depends)
            {
                 if (!assetBundleDic.ContainsKey(depName) || assetBundleDic[depName]==null)
                 {
                    AssetBundle depBundle = AbLoadFromFile(depName);
                    AddAssetBundleDic(depName, depBundle);
                 }
                SVersionAbLoad(depName);
            }
              if (!assetBundleDic.ContainsKey(abName) || assetBundleDic[abName]==null)
                {
                    AssetBundle bundle = AbLoadFromFile(abName);
                    AddAssetBundleDic(abName, bundle);
                }
        }
        else
        {
            Debug.LogError(abName + "assetBundle not exit");
        }
    }

    void PVersionAbLoad(string abName)
    {
        if (pAbVesion.abInfoDic.ContainsKey(abName))
        {
            string[] depends = pManifest.GetAllDependencies(abName);
            foreach (var depName in depends)
            {
                if (!assetBundleDic.ContainsKey(depName) || assetBundleDic[depName]==null)
                {
                    AssetBundle depBundle = AbLoadFromFile(depName);
                    AddAssetBundleDic(depName, depBundle);
                }
                PVersionAbLoad(depName);
            }

             if (!assetBundleDic.ContainsKey(abName) || assetBundleDic[abName]==null)
                {
                    AssetBundle bundle = AbLoadFromFile(abName);
                    AddAssetBundleDic(abName, bundle);
                }
        }
        else
        {
            SVersionAbLoad(abName);
        }
    }

    AssetBundle AbLoadFromFile(string abName)
    {
        string filePath = loadFilePath(abName);
        return filePath != null ? AssetBundle.LoadFromFile(filePath) : null;
    }

    //释放但不销毁加载的Asset对象。 FALSE
    //释放AssetBundle文件内存镜像同时销毁所有已经Load的Assets内存对象 TRUE
    public void UnloadAssetBundle(string abName, bool unloadAllLoadedObjects)
    {
        if (assetBundleDic.ContainsKey(abName))
        {
            if (assetBundleDic[abName] == null)
            {
                Debug.LogError("assetbundle is null");
                return;
            }
            else
            {
                AssetBundle bundle = assetBundleDic[abName];
                bundle.Unload(unloadAllLoadedObjects);
                bundle = null;
                assetBundleDic.Remove(abName);
                _UnLoadDirectDependencies(abName, unloadAllLoadedObjects);
            }
        }
        else
        {
            Debug.LogError("assetbundle is null");
            return;
        }
    }

    void _UnLoadDirectDependencies(string abName, bool unloadAllLoadedObjects)
    {
        if (pAbVesion.abInfoDic.ContainsKey(abName))
        {
            string[] depends = pManifest.GetAllDependencies(abName);
            foreach (var depName in depends)
            {
                if (assetBundleDic[depName] != null)
                {
                    assetBundleDic[depName].Unload(unloadAllLoadedObjects);
                    assetBundleDic[depName] = null;
                    assetBundleDic.Remove(depName);
                }
                _UnLoadDirectDependencies(depName, unloadAllLoadedObjects);
            }
        }
        else if (sAbVesion.abInfoDic.ContainsKey(abName))
        {
            string[] depends = sManifest.GetAllDependencies(abName);
            foreach (var depName in depends)
            {
                if (assetBundleDic[depName] != null)
                {
                    assetBundleDic[depName].Unload(unloadAllLoadedObjects);
                    assetBundleDic[depName] = null;
                    assetBundleDic.Remove(depName);
                }
                _UnLoadDirectDependencies(depName, unloadAllLoadedObjects);
            }
        }
    }

    //用于释放所有没有引用的Asset对象
    public void UnloadUnusedAssets()
    {
        Resources.UnloadUnusedAssets();
        RefreshAddAssetBundleDic();
    }

    void AddAssetBundleDic(string depName, AssetBundle depBundle)
    {
        if (!assetBundleDic.ContainsKey(depName))
        {
            assetBundleDic.Add(depName, depBundle);
        }
        else
        {
            if (assetBundleDic[depName] == null)
            {
                assetBundleDic[depName] = depBundle;
            }
        }
    }

    void RefreshAddAssetBundleDic()
    {
        List<string> keyList = new List<string>();
        foreach (KeyValuePair<string, AssetBundle> dicItem in assetBundleDic)
        {
            if (dicItem.Value == null)
            {
                keyList.Add(dicItem.Key);
            }
        }
        for (int i = 0; i < keyList.Count; i++)
        {
            assetBundleDic.Remove(keyList[i]);
        }
    }

}


