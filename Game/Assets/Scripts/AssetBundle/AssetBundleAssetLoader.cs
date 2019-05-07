using System;
using System.Collections.Generic;
using UnityEngine;


public class AssetBundleAssetLoader : MonoSingleton<AssetBundleAssetLoader>
{
    public class LoadAssetTask : IDisposable 
    {
        public AssetBundle assetBundle;
        public string assetName = "";
        public AssetBundleRequest request;
        public Action<UnityEngine.Object> onFinishAction;
        public Action<AssetBundleRequest> onProcessingAction;
        public LoadAssetTask(
            AssetBundle _assetBundle,
            string _assetName,
            AssetBundleRequest request,
            Action<UnityEngine.Object> onFinishAction,
            Action<AssetBundleRequest> onProcessingAction)
        {
            this.assetBundle = _assetBundle;
            this.assetName = _assetName;
            this.request = request;
            this.onFinishAction = onFinishAction;
            this.onProcessingAction = onProcessingAction;
        }

        public void Dispose()
        {
            assetBundle = null;
            onFinishAction = null;
            onProcessingAction = null;
            request = null;
            assetName ="";
        }
    }

    private Dictionary<string,LoadAssetTask> taskMap = new Dictionary<string, LoadAssetTask>();
    private Dictionary<string, LoadAssetTask> taskMapToAdd = new Dictionary<string, LoadAssetTask>();

    private void CreateTaskToAdd(
        AssetBundle _assetBundle,
        string _assetName,
        Action<UnityEngine.Object> onFinishAction,
        Action<AssetBundleRequest> onProcessingAction
    )
    {
        lock (taskMapToAdd)
        {
            string keyName = string.Format("{0}{1}",_assetBundle.name,_assetName);
            if (taskMapToAdd.ContainsKey(keyName))
            {
                LoadAssetTask task = taskMapToAdd[keyName];
                task.onFinishAction = onFinishAction;
                task.onProcessingAction = onProcessingAction;
            }
            else
            {
                AssetBundleRequest request = _assetBundle.LoadAssetAsync<UnityEngine.Object>(_assetName);;
                LoadAssetTask task = new LoadAssetTask(_assetBundle, _assetName,request, onFinishAction, onProcessingAction);
                taskMapToAdd.Add(keyName, task);
            }
            this.enabled = true;
        }
    }

    public void LoadAssetAsync(
        AssetBundle _assetBundle,
        string _assetName,
        Action<UnityEngine.Object> onFinishAction = null,
        Action<AssetBundleRequest> onProcessingAction = null
    )
    {
        if (_assetBundle == null)
        {
            if (onFinishAction != null)
            {
                try
                {           
                    onFinishAction.Invoke(null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                return;
            }
        }
        else
        {
            CreateTaskToAdd(_assetBundle,_assetName, onFinishAction, onProcessingAction);
        }
    }

    void Update()
    {
        if (taskMapToAdd != null && taskMapToAdd.Count > 0)
        {
            foreach (var kvp in taskMapToAdd)
            {
                string keyToAdd = kvp.Key;
                LoadAssetTask taskToAdd = kvp.Value;
                if (taskMap.ContainsKey(keyToAdd) == true)
                {
                    LoadAssetTask task = taskMap[keyToAdd];
                    task.onFinishAction = taskToAdd.onFinishAction;
                    task.onProcessingAction = taskToAdd.onProcessingAction;
                }
                else
                {
                    taskMap.Add(keyToAdd, taskToAdd);
                }
            }
            taskMapToAdd.Clear();
        }

        if (taskMap != null && taskMap.Count > 0)
        {
            List<string> tasksToRemove = null;
            foreach (var kvp in taskMap)
            {
                LoadAssetTask task = kvp.Value;
                AssetBundleRequest request = task.request;
                if (request.isDone == true)
                {
                    UnityEngine.Object assetLoader = request.asset;
                    if (assetLoader == null)
                    {
#if UNITY_EDITOR
                        Debug.LogWarningFormat(" Load AssetBundle failed ");
#endif
                    }
                    if (task.onFinishAction != null)
                    {
                        try
                        {
                            task.onFinishAction.Invoke(assetLoader);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                    tasksToRemove = tasksToRemove ?? new List<string>();
                    string keyName = string.Format("{0}{1}",task.assetBundle.name,task.assetName);
                    tasksToRemove.Add(keyName);
                }
                else
                {
                    if (task.onProcessingAction != null)
                    {
                        try
                        {
                            task.onProcessingAction.Invoke(request);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
            }

            if (tasksToRemove != null && tasksToRemove.Count > 0)
            {
                for (int i = tasksToRemove.Count - 1; i >= 0; i--)
                {
                    LoadAssetTask task = taskMap[tasksToRemove[i]];
                    taskMap.Remove(tasksToRemove[i]);
                    task.Dispose();
                }
                tasksToRemove.Clear();
                tasksToRemove = null;
            }

            if (taskMap.Count == 0 && taskMapToAdd.Count == 0)
            {
                this.enabled = false;
            }
        }
    }
}