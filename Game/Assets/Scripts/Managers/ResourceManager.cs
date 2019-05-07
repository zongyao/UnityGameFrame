using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ResourceManager : Singleton<ResourceManager>
{
    public T LoadFromResourceSync<T>(string path) where T : UnityEngine.Object
    {
        T result = null;
        if (string.IsNullOrEmpty(path) == false)
        {
            result = Resources.Load<T>(path);
        }
        return result;
    }

    public T LoadSync<T>(string path) where T : UnityEngine.Object
    {
        return LoadFromResourceSync<T>(path);
    }

    public void Cleanup(){
        AssetBundleManager.Instance.UnloadUnusedAssets();
    }


    public T LoadFromAssetBundleSync<T>(string abName, string assetName) where T : UnityEngine.Object
    {
        return AssetBundleManager.Instance.LoadAbAsset<T>(abName, assetName);
    }

    public void LoadFromAssetBundleAsync<T>(string abName, string assetName, Action<T> onFinishAction) where T : UnityEngine.Object
    {
        AssetBundleManager.Instance.LoadAbAssetAsync(abName, assetName, onFinishAction);
    }

    public void UnloadAssetBundle(string abName, bool unloadAllLoadedObjects)
    {
        AssetBundleManager.Instance.UnloadAssetBundle(abName, unloadAllLoadedObjects);
    }

}